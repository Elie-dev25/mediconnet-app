using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Core.Configuration;
using Mediconnet_Backend.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// Controleur pour l'authentification
/// Gere login, register, confirmation email, etc.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IPasswordValidationService _passwordValidationService;
    private readonly IEmailConfirmationService _emailConfirmationService;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<AuthController> _logger;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(
        IAuthService authService,
        IPasswordValidationService passwordValidationService,
        IEmailConfirmationService emailConfirmationService,
        IOptions<EmailSettings> emailSettings,
        ILogger<AuthController> logger,
        IJwtTokenService jwtTokenService)
    {
        _authService = authService;
        _passwordValidationService = passwordValidationService;
        _emailConfirmationService = emailConfirmationService;
        _emailSettings = emailSettings.Value;
        _logger = logger;
        _jwtTokenService = jwtTokenService;
    }

    /// <summary>
    /// Login - Authentifie un utilisateur et retourne un token
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _authService.LoginAsync(request);

            if (response == null)
                return Unauthorized(new { message = "Email ou mot de passe invalide" });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Login error: {ex.Message}");
            return StatusCode(500, new { message = "Une erreur s'est produite lors de la connexion" });
        }
    }

    /// <summary>
    /// Register - Enregistre un nouvel utilisateur
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request.Password != request.ConfirmPassword)
                return BadRequest(new { message = "Les mots de passe ne correspondent pas" });

            // Valider la robustesse du mot de passe
            var passwordValidation = _passwordValidationService.ValidatePassword(request.Password);
            if (!passwordValidation.IsValid)
            {
                return BadRequest(new 
                { 
                    message = "Le mot de passe ne respecte pas la politique de sécurité",
                    errors = passwordValidation.Errors,
                    criteria = new PasswordCriteriaDto
                    {
                        HasMinLength = passwordValidation.Criteria.HasMinLength,
                        HasUppercase = passwordValidation.Criteria.HasUppercase,
                        HasLowercase = passwordValidation.Criteria.HasLowercase,
                        HasDigit = passwordValidation.Criteria.HasDigit,
                        HasSpecialChar = passwordValidation.Criteria.HasSpecialChar
                    }
                });
            }

            var user = await _authService.RegisterAsync(request);

            if (user == null)
                return BadRequest(new { message = "L'utilisateur existe deja ou l'enregistrement a echoue" });

            // Si la confirmation email est requise, ne pas générer de token
            if (_emailSettings.EnableEmailConfirmation && !user.EmailConfirmed)
            {
                return Ok(new LoginResponse
                {
                    Token = null,
                    IdUser = user.IdUser,
                    Nom = user.Nom,
                    Prenom = user.Prenom,
                    Email = user.Email,
                    Role = user.Role,
                    Message = "Inscription réussie ! Un email de confirmation vous a été envoyé.",
                    RequiresEmailConfirmation = true,
                    EmailConfirmed = false
                });
            }

            // Generer le token JWT apres l'enregistrement (si pas de confirmation requise)
            var token = await _jwtTokenService.GenerateTokenAsync(user.IdUser, user.Role);

            var response = new LoginResponse
            {
                Token = token,
                IdUser = user.IdUser,
                Nom = user.Nom,
                Prenom = user.Prenom,
                Email = user.Email,
                Role = user.Role,
                Message = "Utilisateur enregistre avec succes",
                ExpiresIn = 3600,
                EmailConfirmed = true
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Registration error: {ex.Message}");
            return StatusCode(500, new { message = "Une erreur s'est produite lors de l'enregistrement" });
        }
    }

    /// <summary>
    /// GetProfile - Recupere le profil de l'utilisateur connecte
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
                return Unauthorized();

            if (!int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized();

            var user = await _authService.GetCurrentUserAsync(userId);
            if (user == null)
                return NotFound();

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting profile: {ex.Message}");
            return StatusCode(500, new { message = "Une erreur s'est produite" });
        }
    }

    /// <summary>
    /// Logout - Deconnecte l'utilisateur (client-side)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        try
        {
            return Ok(new { message = "Deconnecte avec succes" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during logout: {ex.Message}");
            return StatusCode(500, new { message = "Une erreur s'est produite lors de la deconnexion" });
        }
    }

    /// <summary>
    /// Validate - Valide un token JWT
    /// </summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateToken([FromBody] dynamic request)
    {
        try
        {
            string? token = request?.token;
            if (string.IsNullOrEmpty(token))
                return BadRequest(new { message = "Le token est requis" });

            var isValid = await _authService.ValidateTokenAsync(token);
            return Ok(new { isValid });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error validating token: {ex.Message}");
            return StatusCode(500, new { message = "Une erreur s'est produite" });
        }
    }

    #region Email Confirmation

    /// <summary>
    /// Confirme l'adresse email avec le token reçu
    /// </summary>
    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Token))
                return BadRequest(new ConfirmEmailResponse
                {
                    Success = false,
                    Message = "Le token de confirmation est requis.",
                    ErrorCode = "MISSING_TOKEN"
                });

            // Récupérer l'adresse IP du client
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _emailConfirmationService.ConfirmEmailAsync(request.Token, ipAddress);

            if (!result.Success)
            {
                return BadRequest(new ConfirmEmailResponse
                {
                    Success = false,
                    Message = result.Message,
                    ErrorCode = result.ErrorCode
                });
            }

            return Ok(new ConfirmEmailResponse
            {
                Success = true,
                Message = result.Message,
                RedirectUrl = "/auth/login?confirmed=true"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error confirming email: {ex.Message}");
            return StatusCode(500, new ConfirmEmailResponse
            {
                Success = false,
                Message = "Une erreur est survenue lors de la confirmation.",
                ErrorCode = "SERVER_ERROR"
            });
        }
    }

    /// <summary>
    /// Confirme l'email via GET (pour les liens cliquables)
    /// </summary>
    [HttpGet("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmailGet([FromQuery] string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
                return Redirect($"{GetFrontendUrl()}/auth/confirm-email?error=missing_token");

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _emailConfirmationService.ConfirmEmailAsync(token, ipAddress);

            if (!result.Success)
            {
                return Redirect($"{GetFrontendUrl()}/auth/email-verified?success=false&error={result.ErrorCode}");
            }

            // Rediriger vers la page de confirmation réussie
            return Redirect($"{GetFrontendUrl()}/auth/email-verified?success=true");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error confirming email via GET: {ex.Message}");
            return Redirect($"{GetFrontendUrl()}/auth/confirm-email?error=server_error");
        }
    }

    /// <summary>
    /// Renvoie un email de confirmation
    /// </summary>
    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Email))
                return BadRequest(new ResendConfirmationResponse
                {
                    Success = false,
                    Message = "L'adresse email est requise."
                });

            // Toujours retourner un succès pour ne pas révéler si l'email existe
            var success = await _emailConfirmationService.ResendConfirmationEmailAsync(request.Email);

            // Message générique pour des raisons de sécurité
            return Ok(new ResendConfirmationResponse
            {
                Success = true,
                Message = "Si un compte existe avec cette adresse email et n'est pas encore confirmé, un nouvel email de confirmation a été envoyé."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error resending confirmation: {ex.Message}");
            return StatusCode(500, new ResendConfirmationResponse
            {
                Success = false,
                Message = "Une erreur est survenue. Veuillez réessayer."
            });
        }
    }

    /// <summary>
    /// Vérifie le statut de confirmation d'email pour un utilisateur
    /// </summary>
    [HttpGet("email-status")]
    [Authorize]
    public async Task<IActionResult> GetEmailStatus()
    {
        try
        {
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized();

            var isConfirmed = await _emailConfirmationService.IsEmailConfirmedAsync(userId);

            return Ok(new { emailConfirmed = isConfirmed });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting email status: {ex.Message}");
            return StatusCode(500, new { message = "Une erreur s'est produite" });
        }
    }

    /// <summary>
    /// Vérifie la robustesse d'un mot de passe (endpoint public pour validation côté client)
    /// </summary>
    [HttpPost("check-password-strength")]
    [AllowAnonymous]
    public IActionResult CheckPasswordStrength([FromBody] PasswordStrengthRequest request)
    {
        var validation = _passwordValidationService.ValidatePassword(request.Password);
        
        return Ok(new PasswordStrengthResponse
        {
            IsValid = validation.IsValid,
            Score = validation.StrengthScore,
            StrengthLevel = validation.StrengthLevel.ToString().ToLower(),
            Errors = validation.Errors,
            Criteria = new PasswordCriteriaDto
            {
                HasMinLength = validation.Criteria.HasMinLength,
                HasUppercase = validation.Criteria.HasUppercase,
                HasLowercase = validation.Criteria.HasLowercase,
                HasDigit = validation.Criteria.HasDigit,
                HasSpecialChar = validation.Criteria.HasSpecialChar
            }
        });
    }

    /// <summary>
    /// Change le mot de passe de l'utilisateur connecté
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized(new ChangePasswordResponse
                {
                    Success = false,
                    Message = "Utilisateur non authentifié"
                });

            // Vérifier que les nouveaux mots de passe correspondent
            if (request.NewPassword != request.ConfirmNewPassword)
            {
                return BadRequest(new ChangePasswordResponse
                {
                    Success = false,
                    Message = "Les nouveaux mots de passe ne correspondent pas"
                });
            }

            // Valider la robustesse du nouveau mot de passe
            var validation = _passwordValidationService.ValidatePassword(request.NewPassword);
            if (!validation.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Le nouveau mot de passe ne respecte pas la politique de sécurité",
                    errors = validation.Errors,
                    criteria = new PasswordCriteriaDto
                    {
                        HasMinLength = validation.Criteria.HasMinLength,
                        HasUppercase = validation.Criteria.HasUppercase,
                        HasLowercase = validation.Criteria.HasLowercase,
                        HasDigit = validation.Criteria.HasDigit,
                        HasSpecialChar = validation.Criteria.HasSpecialChar
                    }
                });
            }

            // Récupérer l'utilisateur
            var user = await _authService.GetCurrentUserAsync(userId);
            if (user == null)
            {
                return NotFound(new ChangePasswordResponse
                {
                    Success = false,
                    Message = "Utilisateur non trouvé"
                });
            }

            // Vérifier le mot de passe actuel et mettre à jour (via un nouveau service)
            var result = await ChangeUserPasswordAsync(userId, request.CurrentPassword, request.NewPassword);
            
            if (!result.Success)
            {
                return BadRequest(new ChangePasswordResponse
                {
                    Success = false,
                    Message = result.Message
                });
            }

            _logger.LogInformation($"Password changed successfully for user {userId}");

            return Ok(new ChangePasswordResponse
            {
                Success = true,
                Message = "Mot de passe modifié avec succès"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error changing password: {ex.Message}");
            return StatusCode(500, new ChangePasswordResponse
            {
                Success = false,
                Message = "Une erreur est survenue lors du changement de mot de passe"
            });
        }
    }

    private async Task<(bool Success, string Message)> ChangeUserPasswordAsync(int userId, string currentPassword, string newPassword)
    {
        using var scope = HttpContext.RequestServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
        
        var user = await context.Utilisateurs.FindAsync(userId);
        if (user == null)
            return (false, "Utilisateur non trouvé");

        // Vérifier le mot de passe actuel
        if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        {
            return (false, "Le mot de passe actuel est incorrect");
        }

        // Hasher et sauvegarder le nouveau mot de passe
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        user.MustChangePassword = false; // Réinitialiser le flag après changement
        
        await context.SaveChangesAsync();
        
        return (true, "Mot de passe modifié avec succès");
    }

    private string GetFrontendUrl()
    {
        // Utilise la configuration ou une valeur par défaut
        return "http://localhost:4200";
    }

    #endregion
}
