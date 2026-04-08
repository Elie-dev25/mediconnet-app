using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Admin;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Services;

public interface IAffectationServiceService
{
    Task<HistoriqueAffectationsDto?> GetHistoriqueAffectationsAsync(int userId, string typeUser);
    Task<ChangerServiceResponse> ChangerServiceAsync(int userId, string typeUser, ChangerServiceRequest request, int adminId);
    Task<List<AffectationServiceDto>> GetAffectationsParServiceAsync(int serviceId);
    Task InitialiserAffectationAsync(int userId, string typeUser, int serviceId);
}

public class AffectationServiceService : IAffectationServiceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AffectationServiceService> _logger;

    public AffectationServiceService(ApplicationDbContext context, ILogger<AffectationServiceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Récupère l'historique complet des affectations d'un utilisateur
    /// </summary>
    public async Task<HistoriqueAffectationsDto?> GetHistoriqueAffectationsAsync(int userId, string typeUser)
    {
        var utilisateur = await _context.Utilisateurs
            .FirstOrDefaultAsync(u => u.IdUser == userId);

        if (utilisateur == null)
            return null;

        var affectations = await _context.AffectationsService
            .Include(a => a.Service)
            .Include(a => a.AdminChangement)
            .Where(a => a.IdUser == userId && a.TypeUser == typeUser)
            .OrderByDescending(a => a.DateDebut)
            .ToListAsync();

        var affectationActuelle = affectations.FirstOrDefault(a => a.DateFin == null);
        var historique = affectations.Where(a => a.DateFin != null).ToList();

        return new HistoriqueAffectationsDto
        {
            IdUser = userId,
            NomComplet = $"{utilisateur.Prenom} {utilisateur.Nom}",
            TypeUser = typeUser,
            AffectationActuelle = affectationActuelle != null ? MapToDto(affectationActuelle) : null,
            Historique = historique.Select(MapToDto).ToList()
        };
    }

    /// <summary>
    /// Change le service d'un utilisateur avec historisation
    /// </summary>
    public async Task<ChangerServiceResponse> ChangerServiceAsync(int userId, string typeUser, ChangerServiceRequest request, int adminId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Vérifier que le nouveau service existe
            var nouveauService = await _context.Services.FindAsync(request.IdNouveauService);
            if (nouveauService == null)
            {
                return new ChangerServiceResponse
                {
                    Success = false,
                    Message = "Le service spécifié n'existe pas"
                };
            }

            // Vérifier que l'utilisateur existe et est du bon type
            var utilisateurValide = typeUser switch
            {
                TypeUserAffectation.Medecin => await _context.Medecins.AnyAsync(m => m.IdUser == userId),
                TypeUserAffectation.Infirmier => await _context.Infirmiers.AnyAsync(i => i.IdUser == userId),
                _ => false
            };

            if (!utilisateurValide)
            {
                return new ChangerServiceResponse
                {
                    Success = false,
                    Message = $"Aucun {typeUser} trouvé avec cet identifiant"
                };
            }

            // Récupérer le service actuel
            int? serviceActuelId = typeUser switch
            {
                TypeUserAffectation.Medecin => (await _context.Medecins.FindAsync(userId))?.IdService,
                TypeUserAffectation.Infirmier => (await _context.Infirmiers.FindAsync(userId))?.IdService,
                _ => null
            };

            // Vérifier si c'est le même service
            if (serviceActuelId == request.IdNouveauService)
            {
                return new ChangerServiceResponse
                {
                    Success = false,
                    Message = "L'utilisateur est déjà affecté à ce service"
                };
            }

            var maintenant = DateTime.UtcNow;

            // 1. Clôturer l'affectation actuelle (si elle existe)
            var affectationActuelle = await _context.AffectationsService
                .FirstOrDefaultAsync(a => a.IdUser == userId && a.TypeUser == typeUser && a.DateFin == null);

            if (affectationActuelle != null)
            {
                affectationActuelle.DateFin = maintenant;
                _context.AffectationsService.Update(affectationActuelle);
            }

            // 2. Créer la nouvelle affectation
            var nouvelleAffectation = new AffectationService
            {
                IdUser = userId,
                TypeUser = typeUser,
                IdService = request.IdNouveauService,
                DateDebut = maintenant,
                DateFin = null,
                MotifChangement = request.Motif,
                IdAdminChangement = adminId,
                CreatedAt = maintenant
            };

            _context.AffectationsService.Add(nouvelleAffectation);

            // 3. Mettre à jour le service dans la table principale (medecin ou infirmier)
            if (typeUser == TypeUserAffectation.Medecin)
            {
                var medecin = await _context.Medecins.FindAsync(userId);
                if (medecin != null)
                {
                    medecin.IdService = request.IdNouveauService;
                    _context.Medecins.Update(medecin);
                }
            }
            else if (typeUser == TypeUserAffectation.Infirmier)
            {
                var infirmier = await _context.Infirmiers.FindAsync(userId);
                if (infirmier != null)
                {
                    infirmier.IdService = request.IdNouveauService;
                    _context.Infirmiers.Update(infirmier);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Recharger l'affectation avec les relations
            await _context.Entry(nouvelleAffectation).Reference(a => a.Service).LoadAsync();

            _logger.LogInformation(
                "Service changé pour {TypeUser} {UserId}: {AncienService} -> {NouveauService} par admin {AdminId}",
                typeUser, userId, serviceActuelId, request.IdNouveauService, adminId);

            return new ChangerServiceResponse
            {
                Success = true,
                Message = "Service modifié avec succès",
                NouvelleAffectation = MapToDto(nouvelleAffectation)
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erreur lors du changement de service pour {TypeUser} {UserId}", typeUser, userId);
            return new ChangerServiceResponse
            {
                Success = false,
                Message = "Erreur lors du changement de service"
            };
        }
    }

    /// <summary>
    /// Récupère toutes les affectations actives pour un service donné
    /// </summary>
    public async Task<List<AffectationServiceDto>> GetAffectationsParServiceAsync(int serviceId)
    {
        var affectations = await _context.AffectationsService
            .Include(a => a.Service)
            .Include(a => a.Utilisateur)
            .Where(a => a.IdService == serviceId && a.DateFin == null)
            .OrderBy(a => a.TypeUser)
            .ThenBy(a => a.Utilisateur!.Nom)
            .ToListAsync();

        return affectations.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Initialise une première affectation pour un nouvel utilisateur
    /// </summary>
    public async Task InitialiserAffectationAsync(int userId, string typeUser, int serviceId)
    {
        // Vérifier si une affectation existe déjà
        var existante = await _context.AffectationsService
            .AnyAsync(a => a.IdUser == userId && a.TypeUser == typeUser);

        if (existante)
            return;

        var affectation = new AffectationService
        {
            IdUser = userId,
            TypeUser = typeUser,
            IdService = serviceId,
            DateDebut = DateTime.UtcNow,
            DateFin = null,
            MotifChangement = "Affectation initiale",
            CreatedAt = DateTime.UtcNow
        };

        _context.AffectationsService.Add(affectation);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Affectation initiale créée pour {TypeUser} {UserId} au service {ServiceId}",
            typeUser, userId, serviceId);
    }

    private AffectationServiceDto MapToDto(AffectationService affectation)
    {
        return new AffectationServiceDto
        {
            IdAffectation = affectation.IdAffectation,
            IdUser = affectation.IdUser,
            TypeUser = affectation.TypeUser,
            IdService = affectation.IdService,
            NomService = affectation.Service?.NomService ?? "",
            DateDebut = affectation.DateDebut,
            DateFin = affectation.DateFin,
            MotifChangement = affectation.MotifChangement,
            IdAdminChangement = affectation.IdAdminChangement,
            NomAdminChangement = affectation.AdminChangement != null 
                ? $"{affectation.AdminChangement.Prenom} {affectation.AdminChangement.Nom}" 
                : null
        };
    }
}
