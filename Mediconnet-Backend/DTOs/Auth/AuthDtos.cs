using Mediconnet_Backend.Core.Enums;

namespace Mediconnet_Backend.DTOs.Auth;

/// <summary>
/// DTO pour la demande de connexion
/// Accepte email OU telephone comme identifiant
/// </summary>
public class LoginRequest
{
    /// <summary>Identifiant : email ou numero de telephone</summary>
    public string Identifier { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour la réponse de connexion
/// Inclut les informations d'authentification et les rôles
/// </summary>
public class LoginResponse
{
    public int IdUser { get; set; }
    
    public string Nom { get; set; } = string.Empty;
    
    public string Prenom { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string? Telephone { get; set; }
    
    public string Role { get; set; } = string.Empty;
    
    /// <summary>JWT Token</summary>
    public string? Token { get; set; } = string.Empty;
    
    /// <summary>Message de réponse</summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>Expiration du token (en secondes)</summary>
    public int ExpiresIn { get; set; } = 3600;

    /// <summary>Indique si l'email doit être confirmé</summary>
    public bool RequiresEmailConfirmation { get; set; } = false;

    /// <summary>Indique si l'email est confirmé</summary>
    public bool EmailConfirmed { get; set; } = false;

    /// <summary>Indique si le profil est complété</summary>
    public bool ProfileCompleted { get; set; } = false;

    /// <summary>Indique si l'utilisateur doit changer son mot de passe</summary>
    public bool MustChangePassword { get; set; } = false;

    /// <summary>Indique si la déclaration sur l'honneur a été acceptée (pour les patients)</summary>
    public bool DeclarationHonneurAcceptee { get; set; } = false;

    /// <summary>Indique si l'utilisateur doit compléter sa première connexion</summary>
    public bool RequiresFirstLogin { get; set; } = false;
}

/// <summary>
/// DTO pour l'enregistrement d'un nouvel utilisateur (inscription complète)
/// Fusionne l'inscription et la complétion du profil en une seule étape
/// </summary>
public class RegisterRequest
{
    // === Étape 1: Informations de base ===
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    
    // === Étape 2: Informations personnelles ===
    public DateTime? DateNaissance { get; set; }
    public string? Sexe { get; set; }
    public string? Nationalite { get; set; }
    public string? RegionOrigine { get; set; }
    public string? Adresse { get; set; }
    public string? SituationMatrimoniale { get; set; }
    public string? Profession { get; set; }
    
    // === Étape 3: Informations médicales ===
    public string? GroupeSanguin { get; set; }
    public List<string>? MaladiesChroniques { get; set; }
    public bool OperationsChirurgicales { get; set; }
    public string? OperationsDetails { get; set; }
    public bool AllergiesConnues { get; set; }
    public string? AllergiesDetails { get; set; }
    public bool AntecedentsFamiliaux { get; set; }
    public string? AntecedentsFamiliauxDetails { get; set; }
    
    // === Étape 4: Contact d'urgence ===
    public string? PersonneContact { get; set; }
    public string? NumeroContact { get; set; }
    
    // === Déclaration sur l'honneur ===
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
/// DTO pour la réponse d'erreur d'authentification
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
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>Nouveau mot de passe</summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>Confirmation du nouveau mot de passe</summary>
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour la réponse de changement de mot de passe
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
/// DTO pour la réponse de validation de robustesse
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
/// DTO pour les critères de mot de passe
/// </summary>
public class PasswordCriteriaDto
{
    public bool HasMinLength { get; set; }
    public bool HasUppercase { get; set; }
    public bool HasLowercase { get; set; }
    public bool HasDigit { get; set; }
    public bool HasSpecialChar { get; set; }
}
