using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mediconnet_Backend.Core.Configuration;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service pour la gestion des tokens de confirmation d'email
/// </summary>
public class EmailConfirmationService : IEmailConfirmationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly EmailSettings _emailSettings;
    private readonly AppSettings _appSettings;
    private readonly ILogger<EmailConfirmationService> _logger;

    public EmailConfirmationService(
        ApplicationDbContext context,
        IEmailService emailService,
        IOptions<EmailSettings> emailSettings,
        IOptions<AppSettings> appSettings,
        ILogger<EmailConfirmationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _emailSettings = emailSettings.Value;
        _appSettings = appSettings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GenerateConfirmationTokenAsync(int userId)
    {
        // Invalider les anciens tokens non utilisés
        var oldTokens = await _context.EmailConfirmationTokens
            .Where(t => t.IdUser == userId && !t.IsUsed)
            .ToListAsync();

        foreach (var oldToken in oldTokens)
        {
            oldToken.IsUsed = true;
            oldToken.UsedAt = DateTime.UtcNow;
        }

        // Générer un nouveau token sécurisé
        var token = GenerateSecureToken();
        var expiresAt = DateTime.UtcNow.AddHours(_emailSettings.TokenExpirationHours);

        var confirmationToken = new EmailConfirmationToken
        {
            IdUser = userId,
            Token = token,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            IsUsed = false
        };

        _context.EmailConfirmationTokens.Add(confirmationToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Generated confirmation token for user {userId}, expires at {expiresAt}");

        return token;
    }

    /// <inheritdoc />
    public async Task<bool> SendConfirmationEmailAsync(int userId, string email, string userName)
    {
        try
        {
            var token = await GenerateConfirmationTokenAsync(userId);
            var confirmationLink = BuildConfirmationLink(token);

            var sent = await _emailService.SendEmailConfirmationAsync(email, userName, confirmationLink);

            if (sent)
            {
                _logger.LogInformation($"Confirmation email sent to {email}");
            }
            else
            {
                _logger.LogWarning($"Failed to send confirmation email to {email}");
            }

            return sent;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending confirmation email: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<EmailConfirmationResult> ConfirmEmailAsync(string token, string? ipAddress = null)
    {
        try
        {
            var confirmationToken = await _context.EmailConfirmationTokens
                .Include(t => t.Utilisateur)
                .FirstOrDefaultAsync(t => t.Token == token);

            if (confirmationToken == null)
            {
                _logger.LogWarning($"Invalid confirmation token attempted");
                return new EmailConfirmationResult
                {
                    Success = false,
                    ErrorCode = "INVALID_TOKEN",
                    Message = "Le lien de confirmation est invalide."
                };
            }

            // Vérifier si déjà utilisé
            if (confirmationToken.IsUsed)
            {
                _logger.LogWarning($"Token already used for user {confirmationToken.IdUser}");
                return new EmailConfirmationResult
                {
                    Success = false,
                    ErrorCode = "TOKEN_ALREADY_USED",
                    Message = "Ce lien de confirmation a déjà été utilisé."
                };
            }

            // Vérifier l'expiration
            if (DateTime.UtcNow > confirmationToken.ExpiresAt)
            {
                _logger.LogWarning($"Expired token for user {confirmationToken.IdUser}");
                return new EmailConfirmationResult
                {
                    Success = false,
                    ErrorCode = "TOKEN_EXPIRED",
                    Message = "Ce lien de confirmation a expiré. Veuillez demander un nouveau lien."
                };
            }

            // Marquer le token comme utilisé
            confirmationToken.IsUsed = true;
            confirmationToken.UsedAt = DateTime.UtcNow;
            confirmationToken.ConfirmedFromIp = ipAddress;

            // Mettre à jour l'utilisateur
            var user = confirmationToken.Utilisateur;
            if (user != null)
            {
                user.EmailConfirmed = true;
                user.EmailConfirmedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Email confirmed for user {confirmationToken.IdUser}");

            // Envoyer l'email de bienvenue
            if (user != null)
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, $"{user.Prenom} {user.Nom}");
            }

            return new EmailConfirmationResult
            {
                Success = true,
                UserId = confirmationToken.IdUser,
                Message = "Votre adresse email a été confirmée avec succès !"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error confirming email: {ex.Message}");
            return new EmailConfirmationResult
            {
                Success = false,
                ErrorCode = "SERVER_ERROR",
                Message = "Une erreur est survenue. Veuillez réessayer."
            };
        }
    }

    /// <inheritdoc />
    public async Task<bool> ResendConfirmationEmailAsync(string email)
    {
        try
        {
            var user = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                _logger.LogWarning($"Resend attempt for non-existent email: {email}");
                return false; // Ne pas révéler si l'email existe ou non
            }

            if (user.EmailConfirmed)
            {
                _logger.LogInformation($"Resend attempt for already confirmed email: {email}");
                return true; // L'email est déjà confirmé
            }

            // Vérifier le rate limiting (max 1 email toutes les 2 minutes)
            var recentToken = await _context.EmailConfirmationTokens
                .Where(t => t.IdUser == user.IdUser && !t.IsUsed)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (recentToken != null && 
                (DateTime.UtcNow - recentToken.CreatedAt).TotalMinutes < 2)
            {
                _logger.LogWarning($"Rate limit hit for email resend: {email}");
                return false;
            }

            return await SendConfirmationEmailAsync(user.IdUser, user.Email, $"{user.Prenom} {user.Nom}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error resending confirmation email: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsEmailConfirmedAsync(int userId)
    {
        var user = await _context.Utilisateurs.FindAsync(userId);
        return user?.EmailConfirmed ?? false;
    }

    /// <summary>
    /// Génère un token sécurisé cryptographiquement
    /// </summary>
    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        
        // Encoder en Base64 URL-safe
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    /// <summary>
    /// Construit le lien de confirmation
    /// Le lien pointe vers l'API backend qui valide le token puis redirige vers le frontend
    /// </summary>
    private string BuildConfirmationLink(string token)
    {
        // Utiliser l'URL de l'API backend pour la confirmation
        return $"{_appSettings.ApiUrl}/api/auth/confirm-email?token={Uri.EscapeDataString(token)}";
    }
}

/// <summary>
/// Résultat de la confirmation d'email
/// </summary>
public class EmailConfirmationResult
{
    public bool Success { get; set; }
    public int? UserId { get; set; }
    public string? ErrorCode { get; set; }
    public string Message { get; set; } = string.Empty;
}
