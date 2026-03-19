using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service pour initialiser les donnees par defaut dans la base de donnees
/// </summary>
public class DataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(ApplicationDbContext context, ILogger<DataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Initialise les donnees par defaut (admin, services, etc.)
    /// </summary>
    public async Task SeedAsync()
    {
        await SeedServicesAsync();
    }

    /// <summary>
    /// Cree les services par defaut si ils n'existent pas
    /// </summary>
    private async Task SeedServicesAsync()
    {
        if (!await _context.Services.AnyAsync())
        {
            var services = new List<Service>
            {
                new Service { NomService = "Administration", Description = "Service administratif" },
                new Service { NomService = "Urgences", Description = "Service des urgences" },
                new Service { NomService = "Consultation", Description = "Consultations generales" },
                new Service { NomService = "Chirurgie", Description = "Service de chirurgie" },
                new Service { NomService = "Pediatrie", Description = "Service de pediatrie" },
                new Service { NomService = "Maternite", Description = "Service de maternite" },
                new Service { NomService = "Cardiologie", Description = "Service de cardiologie" },
                new Service { NomService = "Radiologie", Description = "Service de radiologie" }
            };

            await _context.Services.AddRangeAsync(services);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Services par defaut crees");
        }
    }

}
