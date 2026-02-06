using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Admin;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Services;

public interface IInfirmierManagementService
{
    Task<InfirmierDetailsDto?> GetInfirmierDetailsAsync(int userId);
    Task<(bool Success, string Message)> UpdateStatutAsync(int userId, string statut);
    Task<(bool Success, string Message)> NommerMajorAsync(int userId, int idService);
    Task<(bool Success, string Message)> RevoquerMajorAsync(int userId, string? motif);
    Task<(bool Success, string Message)> UpdateAccreditationsAsync(int userId, string? accreditations);
}

public class InfirmierManagementService : IInfirmierManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InfirmierManagementService> _logger;

    public InfirmierManagementService(ApplicationDbContext context, ILogger<InfirmierManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<InfirmierDetailsDto?> GetInfirmierDetailsAsync(int userId)
    {
        var infirmier = await _context.Infirmiers
            .Include(i => i.Service)
            .FirstOrDefaultAsync(i => i.IdUser == userId);

        if (infirmier == null)
            return null;

        // Vérifier si l'infirmier est Major d'un service (via Service.IdMajor)
        var serviceMajor = await _context.Services
            .FirstOrDefaultAsync(s => s.IdMajor == userId);

        bool isMajor = serviceMajor != null;

        return new InfirmierDetailsDto
        {
            Matricule = infirmier.Matricule,
            Statut = infirmier.Statut,
            IdService = infirmier.IdService,
            NomService = infirmier.Service?.NomService,
            IsMajor = isMajor,
            IdServiceMajor = serviceMajor?.IdService,
            NomServiceMajor = serviceMajor?.NomService,
            DateNominationMajor = infirmier.DateNominationMajor,
            Accreditations = infirmier.Accreditations,
            TitreAffiche = isMajor && serviceMajor != null 
                ? $"Major {serviceMajor.NomService}" 
                : "Infirmier"
        };
    }

    public async Task<(bool Success, string Message)> UpdateStatutAsync(int userId, string statut)
    {
        var validStatuts = new[] { "actif", "bloque", "suspendu" };
        if (!validStatuts.Contains(statut.ToLower()))
        {
            return (false, "Statut invalide. Valeurs acceptées: actif, bloque, suspendu");
        }

        var infirmier = await _context.Infirmiers.FindAsync(userId);
        if (infirmier == null)
        {
            return (false, "Infirmier non trouvé");
        }

        infirmier.Statut = statut.ToLower();
        await _context.SaveChangesAsync();

        _logger.LogInformation("Statut de l'infirmier {UserId} mis à jour: {Statut}", userId, statut);
        return (true, $"Statut mis à jour: {statut}");
    }

    public async Task<(bool Success, string Message)> NommerMajorAsync(int userId, int idService)
    {
        var infirmier = await _context.Infirmiers
            .Include(i => i.Utilisateur)
            .FirstOrDefaultAsync(i => i.IdUser == userId);

        if (infirmier == null)
        {
            return (false, "Infirmier non trouvé");
        }

        if (infirmier.Statut == "bloque")
        {
            return (false, "Impossible de nommer un infirmier bloqué comme Major");
        }

        var service = await _context.Services.FindAsync(idService);
        if (service == null)
        {
            return (false, "Service non trouvé");
        }

        // Vérifier si le service a déjà un Major (via Service.IdMajor)
        if (service.IdMajor.HasValue && service.IdMajor.Value != userId)
        {
            var existingMajor = await _context.Infirmiers
                .Include(i => i.Utilisateur)
                .FirstOrDefaultAsync(i => i.IdUser == service.IdMajor.Value);
            
            if (existingMajor != null)
            {
                return (false, $"Le service {service.NomService} a déjà un Major: {existingMajor.Utilisateur.Prenom} {existingMajor.Utilisateur.Nom}");
            }
        }

        // Si l'infirmier était déjà Major d'un autre service, révoquer d'abord
        var ancienServiceMajor = await _context.Services
            .FirstOrDefaultAsync(s => s.IdMajor == userId && s.IdService != idService);
        
        if (ancienServiceMajor != null)
        {
            _logger.LogInformation("Révocation automatique du Major {UserId} du service {OldService}", 
                userId, ancienServiceMajor.NomService);
            ancienServiceMajor.IdMajor = null;
        }

        // Nommer le Major via Service.IdMajor
        service.IdMajor = userId;
        infirmier.DateNominationMajor = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Infirmier {UserId} nommé Major du service {Service}", userId, service.NomService);
        return (true, $"Infirmier nommé Major du service {service.NomService}");
    }

    public async Task<(bool Success, string Message)> RevoquerMajorAsync(int userId, string? motif)
    {
        var infirmier = await _context.Infirmiers
            .FirstOrDefaultAsync(i => i.IdUser == userId);

        if (infirmier == null)
        {
            return (false, "Infirmier non trouvé");
        }

        // Vérifier si l'infirmier est Major d'un service (via Service.IdMajor)
        var serviceMajor = await _context.Services
            .FirstOrDefaultAsync(s => s.IdMajor == userId);

        if (serviceMajor == null)
        {
            return (false, "Cet infirmier n'est pas Major d'un service");
        }

        var ancienService = serviceMajor.NomService;

        // Révoquer le Major via Service.IdMajor
        serviceMajor.IdMajor = null;
        infirmier.DateNominationMajor = null;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Révocation du Major {UserId} du service {Service}. Motif: {Motif}", 
            userId, ancienService, motif ?? "Non spécifié");
        return (true, $"Nomination Major révoquée (ancien service: {ancienService})");
    }

    public async Task<(bool Success, string Message)> UpdateAccreditationsAsync(int userId, string? accreditations)
    {
        var infirmier = await _context.Infirmiers.FindAsync(userId);
        if (infirmier == null)
        {
            return (false, "Infirmier non trouvé");
        }

        infirmier.Accreditations = accreditations;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Accréditations de l'infirmier {UserId} mises à jour", userId);
        return (true, "Accréditations mises à jour");
    }
}
