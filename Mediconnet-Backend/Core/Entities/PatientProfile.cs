namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité Patient - Informations spécifiques aux patients
/// </summary>
public class PatientProfile
{
    public int Id { get; set; }
    
    /// <summary>Clé étrangère vers l'utilisateur</summary>
    public int UserId { get; set; }
    
    /// <summary>Date de naissance</summary>
    public DateTime? DateOfBirth { get; set; }
    
    /// <summary>Genre (M/F/Other)</summary>
    public string? Gender { get; set; }
    
    /// <summary>Adresse</summary>
    public string? Address { get; set; }
    
    /// <summary>Ville</summary>
    public string? City { get; set; }
    
    /// <summary>Code postal</summary>
    public string? PostalCode { get; set; }
    
    /// <summary>Pays</summary>
    public string? Country { get; set; }
    
    /// <summary>Numéro de téléphone principal</summary>
    public string? PhoneNumber { get; set; }
    
    /// <summary>Numéro de téléphone secondaire</summary>
    public string? AlternatePhoneNumber { get; set; }
    
    /// <summary>Identifiant national/Numéro d'assuré</summary>
    public string? NationalId { get; set; }
    
    /// <summary>Groupe sanguin</summary>
    public string? BloodType { get; set; }
    
    /// <summary>Allergies connues</summary>
    public string? Allergies { get; set; }
    
    /// <summary>Maladies chroniques</summary>
    public string? ChronicDiseases { get; set; }
    
    /// <summary>Nom du médecin généraliste</summary>
    public string? GeneralPractitioner { get; set; }
    
    /// <summary>Personne à contacter en urgence</summary>
    public string? EmergencyContactName { get; set; }
    
    /// <summary>Téléphone personne urgence</summary>
    public string? EmergencyContactPhone { get; set; }
    
    /// <summary>Relation avec personne urgence</summary>
    public string? EmergencyContactRelation { get; set; }
    
    /// <summary>Profil complété?</summary>
    public bool IsProfileComplete { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Relations
    public virtual User? User { get; set; }
}
