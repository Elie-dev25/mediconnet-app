using Mediconnet_Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mediconnet_Backend.Infrastructure.BackgroundJobs.Jobs;

/// <summary>
/// Job de nettoyage des données expirées
/// </summary>
public class CleanupJob
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CleanupJob> _logger;

    public CleanupJob(ApplicationDbContext context, ILogger<CleanupJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Nettoie les verrous de créneaux expirés
    /// </summary>
    public async Task CleanExpiredSlotLocksAsync()
    {
        var now = DateTime.UtcNow;
        
        var expiredLocks = await _context.SlotLocks
            .Where(s => s.ExpiresAt < now)
            .ToListAsync();

        if (expiredLocks.Any())
        {
            _context.SlotLocks.RemoveRange(expiredLocks);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Nettoyage de {expiredLocks.Count} verrous de créneaux expirés");
        }
    }

    /// <summary>
    /// Archive les anciens logs d'audit (> 90 jours)
    /// </summary>
    public async Task ArchiveOldAuditLogsAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-90);
        
        // Compter les logs à archiver
        var count = await _context.AuditLogs
            .CountAsync(a => a.CreatedAt < cutoffDate);

        if (count > 0)
        {
            _logger.LogInformation($"Archivage de {count} logs d'audit (> 90 jours)");
            
            // Supprimer par lots pour éviter les timeouts
            var batchSize = 1000;
            var deleted = 0;
            
            while (deleted < count)
            {
                var logsToDelete = await _context.AuditLogs
                    .Where(a => a.CreatedAt < cutoffDate)
                    .Take(batchSize)
                    .ToListAsync();

                if (!logsToDelete.Any()) break;

                _context.AuditLogs.RemoveRange(logsToDelete);
                await _context.SaveChangesAsync();
                deleted += logsToDelete.Count;
                
                _logger.LogDebug($"Supprimé {deleted}/{count} logs d'audit");
            }
        }
    }

    /// <summary>
    /// Nettoie les tokens de confirmation email expirés
    /// </summary>
    public async Task CleanExpiredEmailTokensAsync()
    {
        var now = DateTime.UtcNow;
        
        var expiredTokens = await _context.EmailConfirmationTokens
            .Where(t => t.ExpiresAt < now)
            .ToListAsync();

        if (expiredTokens.Any())
        {
            _context.EmailConfirmationTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Nettoyage de {expiredTokens.Count} tokens email expirés");
        }
    }
}
