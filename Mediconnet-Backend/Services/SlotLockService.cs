using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service de verrouillage des créneaux horaires
/// Utilise un verrou pessimiste temporaire pour éviter les doubles réservations
/// </summary>
public class SlotLockService : ISlotLockService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SlotLockService> _logger;
    private const int DEFAULT_LOCK_DURATION_MINUTES = 5;

    public SlotLockService(ApplicationDbContext context, ILogger<SlotLockService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SlotLockResult> AcquireLockAsync(int medecinId, DateTime dateHeure, int duree, int userId)
    {
        // Nettoyer d'abord les verrous expirés
        await CleanupExpiredLocksAsync();

        var now = DateTime.UtcNow;

        // Vérifier si le créneau est déjà verrouillé par quelqu'un d'autre
        var existingLock = await _context.SlotLocks
            .FirstOrDefaultAsync(l => 
                l.IdMedecin == medecinId &&
                l.ExpiresAt > now &&
                // Chevauchement de créneaux
                l.DateHeure < dateHeure.AddMinutes(duree) &&
                l.DateHeure.AddMinutes(l.Duree) > dateHeure);

        if (existingLock != null)
        {
            if (existingLock.IdUser == userId)
            {
                // L'utilisateur a déjà un verrou, on le prolonge
                existingLock.ExpiresAt = now.AddMinutes(DEFAULT_LOCK_DURATION_MINUTES);
                await _context.SaveChangesAsync();

                return new SlotLockResult
                {
                    Success = true,
                    LockToken = existingLock.LockToken,
                    ExpiresAt = existingLock.ExpiresAt,
                    Message = "Verrou existant prolongé"
                };
            }

            return new SlotLockResult
            {
                Success = false,
                Message = "Ce créneau est temporairement réservé par un autre utilisateur",
                LockedByUserId = existingLock.IdUser
            };
        }

        // Vérifier aussi s'il y a déjà un rendez-vous à ce créneau
        var existingRdv = await _context.RendezVous
            .AnyAsync(r => 
                r.IdMedecin == medecinId &&
                r.Statut != "annule" &&
                r.DateHeure < dateHeure.AddMinutes(duree) &&
                r.DateHeure.AddMinutes(r.Duree) > dateHeure);

        if (existingRdv)
        {
            return new SlotLockResult
            {
                Success = false,
                Message = "Ce créneau est déjà réservé"
            };
        }

        // Créer le nouveau verrou. Si une transaction est déjà active (ex: paiement caisse), ne pas en ouvrir une autre.
        var hasAmbientTransaction = _context.Database.CurrentTransaction != null;
        var transaction = hasAmbientTransaction ? null : await _context.Database.BeginTransactionAsync();

        try
        {
            // Double-vérification après le début de la transaction (ou sous transaction ambiante)
            var lockExists = await _context.SlotLocks
                .AnyAsync(l =>
                    l.IdMedecin == medecinId &&
                    l.ExpiresAt > now &&
                    l.DateHeure < dateHeure.AddMinutes(duree) &&
                    l.DateHeure.AddMinutes(l.Duree) > dateHeure);

            if (lockExists)
            {
                if (transaction != null) await transaction.RollbackAsync();
                return new SlotLockResult
                {
                    Success = false,
                    Message = "Ce créneau vient d'être réservé par un autre utilisateur"
                };
            }

            var lockToken = GenerateLockToken();
            var expiresAt = now.AddMinutes(DEFAULT_LOCK_DURATION_MINUTES);

            var slotLock = new SlotLock
            {
                IdMedecin = medecinId,
                DateHeure = dateHeure,
                Duree = duree,
                IdUser = userId,
                LockToken = lockToken,
                ExpiresAt = expiresAt,
                CreatedAt = now
            };

            _context.SlotLocks.Add(slotLock);
            await _context.SaveChangesAsync();
            if (transaction != null) await transaction.CommitAsync();

            _logger.LogInformation($"Verrou acquis: Médecin {medecinId}, {dateHeure}, Token: {lockToken[..8]}...");

            return new SlotLockResult
            {
                Success = true,
                LockToken = lockToken,
                ExpiresAt = expiresAt,
                Message = "Créneau verrouillé avec succès"
            };
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("Duplicate") == true ||
                                           ex.InnerException?.Message.Contains("UNIQUE") == true)
        {
            if (transaction != null) await transaction.RollbackAsync();
            _logger.LogWarning($"Conflit de verrou détecté: {ex.Message}");

            return new SlotLockResult
            {
                Success = false,
                Message = "Ce créneau vient d'être réservé par un autre utilisateur"
            };
        }
        catch (Exception ex)
        {
            if (transaction != null) await transaction.RollbackAsync();
            _logger.LogError($"Erreur lors de l'acquisition du verrou: {ex.Message}");
            throw;
        }
        finally
        {
            if (transaction != null) await transaction.DisposeAsync();
        }
    }

    public async Task<bool> ValidateLockAsync(string lockToken, int userId)
    {
        if (string.IsNullOrEmpty(lockToken)) return false;

        var slotLock = await _context.SlotLocks
            .FirstOrDefaultAsync(l => l.LockToken == lockToken && l.IdUser == userId);

        if (slotLock == null) return false;

        // Vérifier si le verrou n'a pas expiré
        return slotLock.ExpiresAt > DateTime.UtcNow;
    }

    public async Task<bool> ReleaseLockAsync(string lockToken, int userId)
    {
        if (string.IsNullOrEmpty(lockToken)) return false;

        var slotLock = await _context.SlotLocks
            .FirstOrDefaultAsync(l => l.LockToken == lockToken && l.IdUser == userId);

        if (slotLock == null) return false;

        _context.SlotLocks.Remove(slotLock);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Verrou libéré: Token {lockToken[..8]}...");

        return true;
    }

    public async Task<bool> ExtendLockAsync(string lockToken, int userId, int additionalMinutes = 5)
    {
        if (string.IsNullOrEmpty(lockToken)) return false;

        var slotLock = await _context.SlotLocks
            .FirstOrDefaultAsync(l => l.LockToken == lockToken && l.IdUser == userId);

        if (slotLock == null || slotLock.ExpiresAt <= DateTime.UtcNow)
            return false;

        slotLock.ExpiresAt = DateTime.UtcNow.AddMinutes(additionalMinutes);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<int> CleanupExpiredLocksAsync()
    {
        var now = DateTime.UtcNow;
        var expiredLocks = await _context.SlotLocks
            .Where(l => l.ExpiresAt <= now)
            .ToListAsync();

        if (expiredLocks.Any())
        {
            _context.SlotLocks.RemoveRange(expiredLocks);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Nettoyage: {expiredLocks.Count} verrous expirés supprimés");
        }

        return expiredLocks.Count;
    }

    public async Task<bool> IsSlotLockedAsync(int medecinId, DateTime dateHeure, int duree, int? excludeUserId = null)
    {
        var now = DateTime.UtcNow;

        var query = _context.SlotLocks
            .Where(l => 
                l.IdMedecin == medecinId &&
                l.ExpiresAt > now &&
                l.DateHeure < dateHeure.AddMinutes(duree) &&
                l.DateHeure.AddMinutes(l.Duree) > dateHeure);

        if (excludeUserId.HasValue)
        {
            query = query.Where(l => l.IdUser != excludeUserId.Value);
        }

        return await query.AnyAsync();
    }

    private static string GenerateLockToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}
