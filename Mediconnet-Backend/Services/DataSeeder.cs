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
        await SeedAdminAsync();
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

    /// <summary>
    /// Cree l'administrateur par defaut si il n'existe pas
    /// </summary>
    private async Task SeedAdminAsync()
    {
        const string adminEmail = "admin@gmail.com";
        const string adminPassword = "admin00";

        // Verifier si l'admin existe deja
        var existingAdmin = await _context.Utilisateurs
            .FirstOrDefaultAsync(u => u.Email == adminEmail);

        if (existingAdmin != null)
        {
            // Mettre a jour le mot de passe si necessaire
            existingAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Admin mot de passe mis a jour");
            return;
        }

        // Creer l'utilisateur admin
        var admin = new Utilisateur
        {
            Nom = "Admin",
            Prenom = "System",
            Email = adminEmail,
            Telephone = "000000000",
            Role = "administrateur",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            CreatedAt = DateTime.UtcNow
        };

        _context.Utilisateurs.Add(admin);
        await _context.SaveChangesAsync();

        // Creer l'entree dans la table administrateur
        var administrateur = new Administrateur
        {
            IdUser = admin.IdUser
        };

        _context.Administrateurs.Add(administrateur);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Admin cree avec succes: {adminEmail}");
    }
}
