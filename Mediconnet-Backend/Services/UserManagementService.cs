using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service pour la gestion des utilisateurs
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        ApplicationDbContext context,
        ILogger<UserManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        return await _context.Utilisateurs
            .Include(u => u.Medecin)
                .ThenInclude(m => m != null ? m.Specialite : null)
            .Include(u => u.Medecin)
                .ThenInclude(m => m != null ? m.Service : null)
            .Select(u => new UserDto
            {
                IdUser = u.IdUser,
                Nom = u.Nom,
                Prenom = u.Prenom,
                Email = u.Email,
                Telephone = u.Telephone,
                Role = u.Role,
                CreatedAt = u.CreatedAt,
                Specialite = u.Medecin != null && u.Medecin.Specialite != null 
                    ? u.Medecin.Specialite.NomSpecialite : null,
                Service = u.Medecin != null && u.Medecin.Service != null 
                    ? u.Medecin.Service.NomService : null
            })
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message, int? UserId)> CreateUserAsync(CreateUserRequest request)
    {
        // Verifier si l'email existe deja
        if (await _context.Utilisateurs.AnyAsync(u => u.Email == request.Email))
        {
            return (false, "Cet email est deja utilise", null);
        }

        // Verifier si le telephone existe deja
        if (!string.IsNullOrEmpty(request.Telephone) && 
            await _context.Utilisateurs.AnyAsync(u => u.Telephone == request.Telephone))
        {
            return (false, "Ce numero de telephone est deja utilise", null);
        }

        // Creer l'utilisateur
        var utilisateur = new Utilisateur
        {
            Nom = request.Nom,
            Prenom = request.Prenom,
            Email = request.Email,
            Telephone = request.Telephone,
            Role = request.Role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true, // Les utilisateurs créés par l'admin ont leur email confirmé
            MustChangePassword = true // Doit changer son mot de passe à la première connexion
        };

        _context.Utilisateurs.Add(utilisateur);
        await _context.SaveChangesAsync();

        // Creer l'entite specifique selon le role
        await CreateRoleSpecificEntityAsync(utilisateur, request);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"User created: {request.Email} with role {request.Role}");
        return (true, "Utilisateur cree avec succes", utilisateur.IdUser);
    }

    public async Task<(bool Success, string Message)> DeleteUserAsync(int userId, int? currentUserId)
    {
        var utilisateur = await _context.Utilisateurs.FindAsync(userId);
        if (utilisateur == null)
        {
            return (false, "Utilisateur non trouve");
        }

        // Ne pas permettre la suppression de soi-meme
        if (currentUserId == userId)
        {
            return (false, "Vous ne pouvez pas supprimer votre propre compte");
        }

        _context.Utilisateurs.Remove(utilisateur);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"User deleted: {userId}");
        return (true, "Utilisateur supprime avec succes");
    }

    public async Task<List<SpecialiteDto>> GetSpecialitesAsync()
    {
        return await _context.Specialites
            .Select(s => new SpecialiteDto 
            { 
                IdSpecialite = s.IdSpecialite, 
                NomSpecialite = s.NomSpecialite 
            })
            .OrderBy(s => s.NomSpecialite)
            .ToListAsync();
    }

    private async Task CreateRoleSpecificEntityAsync(Utilisateur utilisateur, CreateUserRequest request)
    {
        switch (request.Role)
        {
            case "patient":
                _context.Patients.Add(new Patient
                {
                    IdUser = utilisateur.IdUser,
                    NumeroDossier = $"PAT-{DateTime.UtcNow:yyyyMMdd}-{utilisateur.IdUser}",
                    DateCreation = DateTime.UtcNow
                });
                break;

            case "medecin":
                _context.Medecins.Add(new Medecin
                {
                    IdUser = utilisateur.IdUser,
                    IdSpecialite = request.IdSpecialite,
                    IdService = request.IdService ?? 1,
                    NumeroOrdre = request.NumeroOrdre
                });
                break;

            case "infirmier":
                _context.Infirmiers.Add(new Infirmier
                {
                    IdUser = utilisateur.IdUser,
                    Matricule = request.Matricule
                });
                break;

            case "administrateur":
                _context.Administrateurs.Add(new Administrateur
                {
                    IdUser = utilisateur.IdUser
                });
                break;

            case "caissier":
                _context.Caissiers.Add(new Caissier
                {
                    IdUser = utilisateur.IdUser
                });
                break;

            case "accueil":
                _context.Accueils.Add(new Accueil
                {
                    IdUser = utilisateur.IdUser,
                    Poste = "Accueil Principal",
                    DateEmbauche = DateTime.UtcNow
                });
                break;

            case "pharmacien":
                _context.Pharmaciens.Add(new Pharmacien
                {
                    IdUser = utilisateur.IdUser,
                    Matricule = request.Matricule,
                    DateEmbauche = DateTime.UtcNow,
                    Actif = true,
                    CreatedAt = DateTime.UtcNow
                });
                break;

            case "biologiste":
                _context.Biologistes.Add(new Biologiste
                {
                    IdUser = utilisateur.IdUser,
                    Matricule = request.Matricule,
                    DateEmbauche = DateTime.UtcNow,
                    Actif = true,
                    CreatedAt = DateTime.UtcNow
                });
                break;
        }
    }
}
