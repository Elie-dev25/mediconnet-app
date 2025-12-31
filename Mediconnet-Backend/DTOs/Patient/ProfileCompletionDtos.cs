using System.ComponentModel.DataAnnotations;

namespace Mediconnet_Backend.DTOs.Patient;

/// <summary>
/// DTO pour la complétion du profil patient - Étape 1: Informations personnelles
/// </summary>
public class PersonalInfoDto
{
    [Required(ErrorMessage = "La date de naissance est requise")]
    public DateTime DateNaissance { get; set; }

    [Required(ErrorMessage = "La nationalité est requise")]
    [MaxLength(100)]
    public string Nationalite { get; set; } = "Cameroun";

    [Required(ErrorMessage = "La région d'origine est requise")]
    [MaxLength(100)]
    public string RegionOrigine { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Ethnie { get; set; }

    [Required(ErrorMessage = "Le sexe est requis")]
    [MaxLength(10)]
    public string Sexe { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? SituationMatrimoniale { get; set; }

    public int? NbEnfants { get; set; }

    /// <summary>Adresse complète</summary>
    [MaxLength(500)]
    public string? Adresse { get; set; }
}

/// <summary>
/// DTO pour la complétion du profil patient - Étape 2: Informations médicales
/// </summary>
public class MedicalInfoDto
{
    [MaxLength(10)]
    public string? GroupeSanguin { get; set; }

    [MaxLength(255)]
    public string? Profession { get; set; }

    /// <summary>Liste des maladies chroniques sélectionnées</summary>
    public List<string> MaladiesChroniques { get; set; } = new();

    /// <summary>Autre maladie chronique (si "Autres" sélectionné)</summary>
    public string? AutreMaladieChronique { get; set; }

    /// <summary>A eu des opérations chirurgicales</summary>
    public bool OperationsChirurgicales { get; set; }

    /// <summary>Détails des opérations si oui</summary>
    public string? OperationsDetails { get; set; }

    /// <summary>A des allergies connues</summary>
    public bool AllergiesConnues { get; set; }

    /// <summary>Détails des allergies si oui</summary>
    public string? AllergiesDetails { get; set; }

    /// <summary>A des antécédents familiaux</summary>
    public bool AntecedentsFamiliaux { get; set; }

    /// <summary>Détails des antécédents si oui</summary>
    public string? AntecedentsFamiliauxDetails { get; set; }
}

/// <summary>
/// DTO pour la complétion du profil patient - Étape 3: Habitudes de vie
/// </summary>
public class LifestyleInfoDto
{
    /// <summary>Consomme de l'alcool</summary>
    public bool ConsommationAlcool { get; set; }

    /// <summary>Fréquence de consommation: occasionnel, regulier, quotidien</summary>
    [MaxLength(50)]
    public string? FrequenceAlcool { get; set; }

    /// <summary>Fumeur</summary>
    public bool Tabagisme { get; set; }

    /// <summary>Pratique une activité physique régulière</summary>
    public bool ActivitePhysique { get; set; }
}

/// <summary>
/// DTO pour la complétion du profil patient - Étape 4: Contacts d'urgence
/// </summary>
public class EmergencyContactDto
{
    [Required(ErrorMessage = "Le nom de la personne à contacter est requis")]
    [MaxLength(150)]
    public string PersonneContact { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le numéro de la personne à contacter est requis")]
    [MaxLength(50)]
    public string NumeroContact { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour la complétion du profil patient - Étape 5: Déclaration sur l'honneur
/// </summary>
public class DeclarationHonneurDto
{
    /// <summary>Le patient accepte la déclaration sur l'honneur</summary>
    [Required(ErrorMessage = "Vous devez accepter la déclaration sur l'honneur")]
    public bool Acceptee { get; set; }
}

/// <summary>
/// DTO complet pour la complétion du profil patient (toutes les étapes)
/// </summary>
public class CompleteProfileRequest
{
    [Required]
    public PersonalInfoDto PersonalInfo { get; set; } = new();

    [Required]
    public MedicalInfoDto MedicalInfo { get; set; } = new();

    [Required]
    public LifestyleInfoDto LifestyleInfo { get; set; } = new();

    [Required]
    public EmergencyContactDto EmergencyContact { get; set; } = new();

    [Required]
    public DeclarationHonneurDto DeclarationHonneur { get; set; } = new();
}

/// <summary>
/// Réponse après complétion du profil
/// </summary>
public class CompleteProfileResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public PatientProfileSummary? Profile { get; set; }
}

/// <summary>
/// Résumé du profil patient après complétion
/// </summary>
public class PatientProfileSummary
{
    public int IdUser { get; set; }
    public string NomComplet { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telephone { get; set; }
    public DateTime? DateNaissance { get; set; }
    public string? Nationalite { get; set; }
    public string? RegionOrigine { get; set; }
    public string? Sexe { get; set; }
    public string? GroupeSanguin { get; set; }
    public bool ProfileCompleted { get; set; }
    public DateTime? ProfileCompletedAt { get; set; }
}

/// <summary>
/// DTO pour vérifier le statut de complétion du profil
/// </summary>
public class ProfileStatusResponse
{
    public bool ProfileCompleted { get; set; }
    public DateTime? ProfileCompletedAt { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? RedirectTo { get; set; }
}

/// <summary>
/// Options de données pour les formulaires (régions, groupes sanguins, etc.)
/// </summary>
public class ProfileFormOptionsDto
{
    public List<string> Regions { get; set; } = new();
    public List<string> GroupesSanguins { get; set; } = new();
    public List<string> SituationsMatrimoniales { get; set; } = new();
    public List<string> MaladiesChroniquesOptions { get; set; } = new();
    public List<string> FrequencesAlcool { get; set; } = new();
}
