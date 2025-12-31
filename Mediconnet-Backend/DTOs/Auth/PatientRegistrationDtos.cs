using Mediconnet_Backend.Core.Enums;

namespace Mediconnet_Backend.DTOs.Auth;

/// <summary>
/// DTO pour l'enregistrement d'un patient - ÉTAPE 1
/// Contient uniquement les champs essentiels
/// </summary>
public class PatientRegistrationStep1Request
{
    public string Username { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string Password { get; set; } = string.Empty;
    
    public string ConfirmPassword { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour la réponse après l'enregistrement ÉTAPE 1
/// </summary>
public class PatientRegistrationStep1Response
{
    public int UserId { get; set; }
    
    public int PatientProfileId { get; set; }
    
    public string Username { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    /// <summary>JWT Token pour continuer dans la session</summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>Message d'invite à compléter le profil</summary>
    public string Message { get; set; } = "Compte créé avec succès! Veuillez compléter votre profil.";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO pour la complétion du profil patient - ÉTAPE 2
/// </summary>
public class PatientProfileCompletionRequest
{
    public DateTime? DateOfBirth { get; set; }
    
    public string? Gender { get; set; } // M, F, Other
    
    public string? Address { get; set; }
    
    public string? City { get; set; }
    
    public string? PostalCode { get; set; }
    
    public string? Country { get; set; }
    
    public string? PhoneNumber { get; set; }
    
    public string? AlternatePhoneNumber { get; set; }
    
    public string? NationalId { get; set; }
    
    public string? BloodType { get; set; }
    
    public string? Allergies { get; set; }
    
    public string? ChronicDiseases { get; set; }
    
    public string? GeneralPractitioner { get; set; }
    
    public string? EmergencyContactName { get; set; }
    
    public string? EmergencyContactPhone { get; set; }
    
    public string? EmergencyContactRelation { get; set; }
}

/// <summary>
/// DTO pour la réponse après complétion du profil - ÉTAPE 2
/// </summary>
public class PatientProfileCompletionResponse
{
    public int UserId { get; set; }
    
    public int PatientProfileId { get; set; }
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    public DateTime? DateOfBirth { get; set; }
    
    public string? Gender { get; set; }
    
    public string? PhoneNumber { get; set; }
    
    public string? Address { get; set; }
    
    public string? City { get; set; }
    
    public bool IsProfileComplete { get; set; } = true;
    
    /// <summary>Dashboard route pour le patient</summary>
    public string DashboardRoute { get; set; } = "/dashboard/patient";
    
    /// <summary>Message de bienvenue</summary>
    public string Message { get; set; } = "Profil complété avec succès! Bienvenue sur MediConnect.";
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO pour récupérer le statut du profil patient
/// </summary>
public class PatientProfileStatusResponse
{
    public int UserId { get; set; }
    
    public int PatientProfileId { get; set; }
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    public bool IsProfileComplete { get; set; }
    
    /// <summary>Pourcentage de complétude du profil (0-100)</summary>
    public int ProfileCompletionPercentage { get; set; }
    
    /// <summary>Liste des champs manquants</summary>
    public List<string> MissingFields { get; set; } = new();
}
