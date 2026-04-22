using System.ComponentModel.DataAnnotations;
using Mediconnet_Backend.Core.Enums;
using System.Text.Json.Serialization;

namespace Mediconnet_Backend.DTOs.Auth;

/// <summary>
/// DTO pour la demande de connexion
/// Accepte email OU telephone comme identifiant
/// </summary>
public class LoginRequest
{
    /// <summary>Identifiant : email ou numero de telephone</summary>
    [Required(ErrorMessage = "L'identifiant est requis")]
    public string Identifier { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le mot de passe est requis")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour la rÃ©ponse de connexion
/// Inclut les informations d'authentification et les rÃ´les
/// </summary>
public class LoginResponse
{
    public int IdUser { get; set; }
    
    public string Nom { get; set; } = string.Empty;
    
    public string Prenom { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string? Telephone { get; set; }
    
    public string Role { get; set; } = string.Empty;
    
    /// <summary>Titre affichÃ© (ex: "Major PÃ©diatrie" pour un infirmier major)</summary>
    public string? TitreAffiche { get; set; }
    
    /// <summary>ID de la spÃ©cialitÃ© (pour les mÃ©decins)</summary>
    public int? IdSpecialite { get; set; }
    
    /// <summary>JWT Token</summary>
    public string? Token { get; set; } = string.Empty;
    
    /// <summary>Message de rÃ©ponse</summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>Expiration du token (en secondes)</summary>
    public int ExpiresIn { get; set; } = 3600;

    /// <summary>Indique si l'email doit Ãªtre confirmÃ©</summary>
    public bool RequiresEmailConfirmation { get; set; } = false;

    /// <summary>Indique si l'email est confirmÃ©</summary>
    public bool EmailConfirmed { get; set; } = false;

    /// <summary>Indique si le profil est complÃ©tÃ©</summary>
    public bool ProfileCompleted { get; set; } = false;

    /// <summary>Indique si l'utilisateur doit changer son mot de passe</summary>
    public bool MustChangePassword { get; set; } = false;

    /// <summary>Indique si la dÃ©claration sur l'honneur a Ã©tÃ© acceptÃ©e (pour les patients)</summary>
    public bool DeclarationHonneurAcceptee { get; set; } = false;

    /// <summary>Indique si l'utilisateur doit complÃ©ter sa premiÃ¨re connexion</summary>
    public bool RequiresFirstLogin { get; set; } = false;
}

/// <summary>
/// DTO pour l'enregistrement d'un nouvel utilisateur (inscription complÃ¨te)
/// Fusionne l'inscription et la complÃ©tion du profil en une seule Ã©tape
/// </summary>
public class RegisterRequest
{
    // === Ã‰tape 1: Informations de base ===
    [Required(ErrorMessage = "Le prÃ©nom est requis")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le prÃ©nom doit contenir entre 2 et 100 caractÃ¨res")]
    public string FirstName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le nom est requis")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caractÃ¨res")]
    public string LastName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le tÃ©lÃ©phone est requis")]
    [Phone(ErrorMessage = "Format de tÃ©lÃ©phone invalide")]
    public string Telephone { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le mot de passe est requis")]
    [MinLength(8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractÃ¨res")]
    public string Password { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
    [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas")]
    public string ConfirmPassword { get; set; } = string.Empty;
    
    // === Ã‰tape 2: Informations personnelles ===
    public DateTime? DateNaissance { get; set; }
    public string? Sexe { get; set; }
    public string? Nationalite { get; set; }
    public string? RegionOrigine { get; set; }
    public string? Adresse { get; set; }
    public string? SituationMatrimoniale { get; set; }
    public string? Profession { get; set; }
    public int? NbEnfants { get; set; }
    public string? Ethnie { get; set; }
    
    // === Ã‰tape 3: Informations mÃ©dicales ===
    public string? GroupeSanguin { get; set; }
    public List<string>? MaladiesChroniques { get; set; }
    [JsonRequired]
    public bool OperationsChirurgicales { get; set; }
    public string? OperationsDetails { get; set; }
    [JsonRequired]
    public bool AllergiesConnues { get; set; }
    public string? AllergiesDetails { get; set; }
    [JsonRequired]
    public bool AntecedentsFamiliaux { get; set; }
    public string? AntecedentsFamiliauxDetails { get; set; }
    
    // === Ã‰tape 4: Habitudes de vie ===
    public bool? ConsommationAlcool { get; set; }
    public string? FrequenceAlcool { get; set; }
    public bool? Tabagisme { get; set; }
    public bool? ActivitePhysique { get; set; }
    
    // === Ã‰tape 5: Contact d'urgence ===
    public string? PersonneContact { get; set; }
    public string? NumeroContact { get; set; }
    
    // === DÃ©claration sur l'honneur ===
    [JsonRequired]
    public bool DeclarationHonneurAcceptee { get; set; }
    
    public string? Role { get; set; }
}

/// <summary>
/// DTO pour les informations publiques d'un utilisateur
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    
    public string Username { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    public UserRole PrimaryRole { get; set; }
    
    public List<UserRole>? SecondaryRoles { get; set; }
    
    public bool IsActive { get; set; }
    
    public string? PhoneNumber { get; set; }
    
    public string? Department { get; set; }
    
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO pour la rÃ©ponse d'erreur d'authentification
/// </summary>
public class AuthErrorResponse
{
    public string Message { get; set; } = string.Empty;
    
    public string Code { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO pour le changement de mot de passe
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>Mot de passe actuel</summary>
    [Required(ErrorMessage = "Le mot de passe actuel est requis")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>Nouveau mot de passe</summary>
    [Required(ErrorMessage = "Le nouveau mot de passe est requis")]
    [MinLength(8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractÃ¨res")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>Confirmation du nouveau mot de passe</summary>
    [Required(ErrorMessage = "La confirmation est requise")]
    [Compare("NewPassword", ErrorMessage = "Les mots de passe ne correspondent pas")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour la rÃ©ponse de changement de mot de passe
/// </summary>
public class ChangePasswordResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour la validation de robustesse du mot de passe
/// </summary>
public class PasswordStrengthRequest
{
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour la rÃ©ponse de validation de robustesse
/// </summary>
public class PasswordStrengthResponse
{
    public bool IsValid { get; set; }
    public int Score { get; set; }
    public string StrengthLevel { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public PasswordCriteriaDto Criteria { get; set; } = new();
}

/// <summary>
/// DTO pour les critÃ¨res de mot de passe
/// </summary>
public class PasswordCriteriaDto
{
    public bool HasMinLength { get; set; }
    public bool HasUppercase { get; set; }
    public bool HasLowercase { get; set; }
    public bool HasDigit { get; set; }
    public bool HasSpecialChar { get; set; }
}
