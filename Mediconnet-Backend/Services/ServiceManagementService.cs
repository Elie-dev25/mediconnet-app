using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service pour la gestion des services hospitaliers
/// </summary>
public class ServiceManagementService : IServiceManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ServiceManagementService> _logger;

    public ServiceManagementService(
        ApplicationDbContext context,
        ILogger<ServiceManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ServiceDto>> GetAllServicesAsync()
    {
        return await _context.Services
            .Include(s => s.Responsable)
            .Include(s => s.Medecins)
            .Select(s => new ServiceDto
            {
                IdService = s.IdService,
                NomService = s.NomService,
                Description = s.Description,
                ResponsableId = s.ResponsableService,
                ResponsableNom = s.Responsable != null 
                    ? $"{s.Responsable.Prenom} {s.Responsable.Nom}" 
                    : null,
                NombreMedecins = s.Medecins.Count
            })
            .OrderBy(s => s.NomService)
            .ToListAsync();
    }

    public async Task<ServiceDto?> GetServiceByIdAsync(int id)
    {
        return await _context.Services
            .Include(s => s.Responsable)
            .Include(s => s.Medecins)
            .Where(s => s.IdService == id)
            .Select(s => new ServiceDto
            {
                IdService = s.IdService,
                NomService = s.NomService,
                Description = s.Description,
                ResponsableId = s.ResponsableService,
                ResponsableNom = s.Responsable != null 
                    ? $"{s.Responsable.Prenom} {s.Responsable.Nom}" 
                    : null,
                NombreMedecins = s.Medecins.Count
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Message, int? ServiceId)> CreateServiceAsync(CreateServiceRequest request)
    {
        // Verifier si le nom existe deja
        if (await _context.Services.AnyAsync(s => s.NomService == request.NomService))
        {
            return (false, "Un service avec ce nom existe deja", null);
        }

        var service = new Service
        {
            NomService = request.NomService,
            Description = request.Description,
            ResponsableService = request.ResponsableId
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Service created: {request.NomService}");
        return (true, "Service cree avec succes", service.IdService);
    }

    public async Task<(bool Success, string Message)> UpdateServiceAsync(int id, UpdateServiceRequest request)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null)
        {
            return (false, "Service non trouve");
        }

        // Verifier si le nouveau nom existe deja (sauf pour ce service)
        if (request.NomService != service.NomService && 
            await _context.Services.AnyAsync(s => s.NomService == request.NomService))
        {
            return (false, "Un service avec ce nom existe deja");
        }

        service.NomService = request.NomService;
        service.Description = request.Description;
        service.ResponsableService = request.ResponsableId;

        await _context.SaveChangesAsync();

        _logger.LogInformation($"Service updated: {id}");
        return (true, "Service modifie avec succes");
    }

    public async Task<(bool Success, string Message)> DeleteServiceAsync(int id)
    {
        var service = await _context.Services
            .Include(s => s.Medecins)
            .FirstOrDefaultAsync(s => s.IdService == id);

        if (service == null)
        {
            return (false, "Service non trouve");
        }

        // Verifier si des medecins sont affectes a ce service
        if (service.Medecins.Any())
        {
            return (false, $"Impossible de supprimer ce service. {service.Medecins.Count} medecin(s) y sont affecte(s).");
        }

        _context.Services.Remove(service);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Service deleted: {id}");
        return (true, "Service supprime avec succes");
    }

    public async Task<List<ResponsableDto>> GetResponsablesAsync()
    {
        var medecins = await _context.Medecins
            .Include(m => m.Utilisateur)
            .ToListAsync();

        return medecins
            .Select(m => new ResponsableDto
            {
                Id = m.IdUser,
                Nom = m.Utilisateur != null 
                    ? $"Dr. {m.Utilisateur.Prenom} {m.Utilisateur.Nom}" 
                    : "Inconnu"
            })
            .OrderBy(r => r.Nom)
            .ToList();
    }
}
