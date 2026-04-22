using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mediconnet_Backend.DTOs.Patient;

/// <summary>
/// DTO pour la complÃ©tion du profil patient - Ã‰tape 1: Informations personnelles
/// </summary>
public class PersonalInfoDto
{
    [Required(ErrorMessage = "La date de naissance est requise")]
    [JsonRequired]
    public DateTime DateNaissance { get; set; }

    [Required(ErrorMessage = "La nationalitÃ© est requise")]
    [MaxLength(100)]
    public string Nationalite { get; set; } = "Cameroun";

    [Required(ErrorMessage = "La rÃ©gion d'origine est requise")]
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

    /// <summary>Adresse complÃ¨te</summary>
    [MaxLength(500)]
    public string? Adresse { get; set; }
}

/// <summary>
/// DTO pour la complÃ©tion du profil patient - Ã‰tape 2: Informations mÃ©dicales
/// </summary>
public class MedicalInfoDto
{
    [MaxLength(10)]
    public string? GroupeSanguin { get; set; }

    [MaxLength(255)]
    public string? Profession { get; set; }

    /// <summary>Liste des maladies chroniques sÃ©lectionnÃ©es</summary>
    public List<string> MaladiesChroniques { get; set; } = new();

    /// <summary>Autre maladie chronique (si "Autres" sÃ©lectionnÃ©)</summary>
    public string? AutreMaladieChronique { get; set; }

    /// <summary>A eu des opÃ©rations chirurgicales</summary>
    [JsonRequired]
    public bool OperationsChirurgicales { get; set; }

    /// <summary>DÃ©tails des opÃ©rations si oui</summary>
    public string? OperationsDetails { get; set; }

    /// <summary>A des allergies connues</summary>
    [JsonRequired]
    public bool AllergiesConnues { get; set; }

    /// <summary>DÃ©tails des allergies si oui</summary>
    public string? AllergiesDetails { get; set; }

    /// <summary>A des antÃ©cÃ©dents familiaux</summary>
    [JsonRequired]
    public bool AntecedentsFamiliaux { get; set; }

    /// <summary>DÃ©tails des antÃ©cÃ©dents si oui</summary>
    public string? AntecedentsFamiliauxDetails { get; set; }
}

/// <summary>
/// DTO pour la complÃ©tion du profil patient - Ã‰tape 3: Habitudes de vie
/// </summary>
public class LifestyleInfoDto
{
    /// <summary>Consomme de l'alcool</summary>
    [JsonRequired]
    public bool ConsommationAlcool { get; set; }

    /// <summary>FrÃ©quence de consommation: occasionnel, regulier, quotidien</summary>
    [MaxLength(50)]
    public string? FrequenceAlcool { get; set; }

    /// <summary>Fumeur</summary>
    [JsonRequired]
    public bool Tabagisme { get; set; }

    /// <summary>Pratique une activitÃ© physique rÃ©guliÃ¨re</summary>
    [JsonRequired]
    public bool ActivitePhysique { get; set; }
}

/// <summary>
/// DTO pour la complÃ©tion du profil patient - Ã‰tape 4: Contacts d'urgence
/// </summary>
public class EmergencyContactDto
{
    [Required(ErrorMessage = "Le nom de la personne Ã  contacter est requis")]
    [MaxLength(150)]
    public string PersonneContact { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le numÃ©ro de la personne Ã  contacter est requis")]
    [MaxLength(50)]
    public string NumeroContact { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour la complÃ©tion du profil patient - Ã‰tape 5: DÃ©claration sur l'honneur
/// </summary>
public class DeclarationHonneurDto
{
    /// <summary>Le patient accepte la dÃ©claration sur l'honneur</summary>
    [Required(ErrorMessage = "Vous devez accepter la dÃ©claration sur l'honneur")]
    [JsonRequired]
    public bool Acceptee { get; set; }
}

/// <summary>
/// DTO complet pour la complÃ©tion du profil patient (toutes les Ã©tapes)
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
/// RÃ©ponse aprÃ¨s complÃ©tion du profil
/// </summary>
public class CompleteProfileResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public PatientProfileSummary? Profile { get; set; }
}

/// <summary>
/// RÃ©sumÃ© du profil patient aprÃ¨s complÃ©tion
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
/// DTO pour vÃ©rifier le statut de complÃ©tion du profil
/// </summary>
public class ProfileStatusResponse
{
    public bool ProfileCompleted { get; set; }
    public DateTime? ProfileCompletedAt { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? RedirectTo { get; set; }
}

/// <summary>
/// Options de donnÃ©es pour les formulaires (rÃ©gions, groupes sanguins, etc.)
/// </summary>
public class ProfileFormOptionsDto
{
    public List<string> Regions { get; set; } = new();
    public List<string> GroupesSanguins { get; set; } = new();
    public List<string> SituationsMatrimoniales { get; set; } = new();
    public List<string> MaladiesChroniquesOptions { get; set; } = new();
    public List<string> FrequencesAlcool { get; set; } = new();
}
