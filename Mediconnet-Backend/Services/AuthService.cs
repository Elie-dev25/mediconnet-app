using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Core.Configuration;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using BCrypt.Net;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service d'authentification pour les tables existantes
/// </summary>
public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuditService _auditService;
    private readonly IEmailConfirmationService _emailConfirmationService;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IAuditService auditService,
        IEmailConfirmationService emailConfirmationService,
        IOptions<EmailSettings> emailSettings,
        ILogger<AuthService> logger)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _auditService = auditService;
        _emailConfirmationService = emailConfirmationService;
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            // Rechercher l'utilisateur par email OU telephone
            var identifier = request.Identifier?.Trim();
            var utilisateur = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.Email == identifier || u.Telephone == identifier);

            if (utilisateur == null)
            {
                _logger.LogWarning($"Login failed: User {identifier} not found");
                await _auditService.LogActionAsync("SYSTEM", "LOGIN_FAILED", "Utilisateur", $"Identifier: {identifier}");
                return null;
            }

            // Verifier le password
            if (string.IsNullOrEmpty(utilisateur.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, utilisateur.PasswordHash))
            {
                _logger.LogWarning($"Login failed: Invalid password for user {identifier}");
                await _auditService.LogActionAsync(utilisateur.IdUser.ToString(), "LOGIN_FAILED", "Utilisateur", "Invalid password");
                return null;
            }

            // Vérifier si l'email est confirmé (si la confirmation est activée)
            if (_emailSettings.EnableEmailConfirmation && !utilisateur.EmailConfirmed)
            {
                _logger.LogWarning($"Login failed: Email not confirmed for user {identifier}");
                return new LoginResponse
                {
                    Token = null,
                    IdUser = utilisateur.IdUser,
                    Email = utilisateur.Email,
                    Message = "EMAIL_NOT_CONFIRMED",
                    RequiresEmailConfirmation = true
                };
            }

            // Generer token
            var token = await _jwtTokenService.GenerateTokenAsync(utilisateur.IdUser, utilisateur.Role);

            // Logger le login reussi
            await _auditService.LogActionAsync(utilisateur.IdUser.ToString(), "LOGIN_SUCCESS", "Utilisateur");

            // Récupérer les infos du patient si c'est un patient
            bool declarationHonneurAcceptee = false;
            if (utilisateur.Role == "patient")
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.IdUser == utilisateur.IdUser);
                declarationHonneurAcceptee = patient?.DeclarationHonneurAcceptee ?? false;
            }

            // Déterminer si première connexion requise
            bool requiresFirstLogin = utilisateur.Role == "patient" && 
                (utilisateur.MustChangePassword || !declarationHonneurAcceptee);

            var response = new LoginResponse
            {
                Token = token,
                IdUser = utilisateur.IdUser,
                Nom = utilisateur.Nom,
                Prenom = utilisateur.Prenom,
                Email = utilisateur.Email,
                Telephone = utilisateur.Telephone,
                Role = utilisateur.Role,
                Message = "Connexion reussie",
                ExpiresIn = 3600,
                EmailConfirmed = utilisateur.EmailConfirmed,
                ProfileCompleted = utilisateur.ProfileCompleted,
                MustChangePassword = utilisateur.MustChangePassword,
                DeclarationHonneurAcceptee = declarationHonneurAcceptee,
                RequiresFirstLogin = requiresFirstLogin
            };

            _logger.LogInformation($"User {utilisateur.Email} logged in successfully");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during login: {ex.Message}");
            throw;
        }
    }

    public async Task<UtilisateurDto?> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Verifier si l'utilisateur existe deja (email ou telephone)
            if (await _context.Utilisateurs.AnyAsync(u => u.Email == request.Email))
            {
                _logger.LogWarning($"Registration failed: Email already exists");
                return null;
            }

            if (await _context.Utilisateurs.AnyAsync(u => u.Telephone == request.Telephone))
            {
                _logger.LogWarning($"Registration failed: Phone number already exists");
                return null;
            }

            // Creer l'utilisateur avec toutes les informations (inscription complète)
            var utilisateur = new Utilisateur
            {
                Nom = request.LastName,
                Prenom = request.FirstName,
                Email = request.Email,
                Telephone = request.Telephone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = "patient",
                CreatedAt = DateTime.UtcNow,
                // Informations personnelles étendues
                Naissance = request.DateNaissance,
                Sexe = request.Sexe,
                Nationalite = request.Nationalite ?? "Cameroun",
                RegionOrigine = request.RegionOrigine,
                Adresse = request.Adresse,
                SituationMatrimoniale = request.SituationMatrimoniale,
                // Marquer le profil comme complété si toutes les infos sont fournies
                ProfileCompleted = request.DeclarationHonneurAcceptee,
                ProfileCompletedAt = request.DeclarationHonneurAcceptee ? DateTime.UtcNow : null
            };

            _context.Utilisateurs.Add(utilisateur);
            await _context.SaveChangesAsync();

            // Generer un numero de dossier patient
            string numeroDossier = "DOS_" + DateTime.UtcNow.ToString("yyyyMMdd") + "_" + utilisateur.IdUser.ToString().PadLeft(4, '0');

            // Creer l'entree dans la table patient avec toutes les informations médicales
            var patient = new Patient
            {
                IdUser = utilisateur.IdUser,
                NumeroDossier = numeroDossier,
                DateCreation = DateTime.UtcNow,
                // Informations personnelles
                Profession = request.Profession,
                // Informations médicales
                GroupeSanguin = request.GroupeSanguin,
                MaladiesChroniques = request.MaladiesChroniques != null ? string.Join(", ", request.MaladiesChroniques) : null,
                OperationsChirurgicales = request.OperationsChirurgicales,
                OperationsDetails = request.OperationsDetails,
                AllergiesConnues = request.AllergiesConnues,
                AllergiesDetails = request.AllergiesDetails,
                AntecedentsFamiliaux = request.AntecedentsFamiliaux,
                AntecedentsFamiliauxDetails = request.AntecedentsFamiliauxDetails,
                // Contact d'urgence
                PersonneContact = request.PersonneContact,
                NumeroContact = request.NumeroContact,
                // Déclaration sur l'honneur
                DeclarationHonneurAcceptee = request.DeclarationHonneurAcceptee,
                DeclarationHonneurAt = request.DeclarationHonneurAcceptee ? DateTime.UtcNow : null
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {utilisateur.Email} registered successfully with complete profile");

            // Envoyer l'email de confirmation si activé
            if (_emailSettings.EnableEmailConfirmation)
            {
                var userName = $"{utilisateur.Prenom} {utilisateur.Nom}";
                var emailSent = await _emailConfirmationService.SendConfirmationEmailAsync(
                    utilisateur.IdUser, 
                    utilisateur.Email, 
                    userName);

                if (!emailSent)
                {
                    _logger.LogWarning($"Failed to send confirmation email to {utilisateur.Email}");
                }
            }
            else
            {
                // Si la confirmation n'est pas requise, marquer comme confirmé
                utilisateur.EmailConfirmed = true;
                utilisateur.EmailConfirmedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return new UtilisateurDto
            {
                IdUser = utilisateur.IdUser,
                Nom = utilisateur.Nom,
                Prenom = utilisateur.Prenom,
                Email = utilisateur.Email,
                Role = utilisateur.Role,
                CreatedAt = utilisateur.CreatedAt,
                EmailConfirmed = utilisateur.EmailConfirmed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during registration: {ex.Message} - {ex.InnerException?.Message}");
            throw;
        }
    }

    public async Task<UtilisateurDto?> GetCurrentUserAsync(int userId)
    {
        try
        {
            var utilisateur = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.IdUser == userId);

            if (utilisateur == null)
                return null;

            return new UtilisateurDto
            {
                IdUser = utilisateur.IdUser,
                Nom = utilisateur.Nom,
                Prenom = utilisateur.Prenom,
                Email = utilisateur.Email,
                Role = utilisateur.Role,
                Telephone = utilisateur.Telephone,
                CreatedAt = utilisateur.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting current user: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var userIdStr = _jwtTokenService.GetUserIdFromToken(token);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return false;

            var utilisateur = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.IdUser == userId);
            return utilisateur != null;
        }
        catch
        {
            return false;
        }
    }
}
