using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mediconnet_Backend.DTOs.Accueil;

/// <summary>
/// DTO pour la crÃ©ation d'un patient complet par l'accueil
/// Inclut toutes les informations du formulaire register + complÃ©tion profil
/// </summary>
public class CreatePatientByReceptionRequest
{
    // ========== Informations personnelles (obligatoires) ==========
    
    [Required(ErrorMessage = "Le nom est requis")]
    [StringLength(100, MinimumLength = 2)]
    public string Nom { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le prÃ©nom est requis")]
    [StringLength(100, MinimumLength = 2)]
    public string Prenom { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "La date de naissance est requise")]
    [JsonRequired]
    public DateTime DateNaissance { get; set; }
    
    [Required(ErrorMessage = "Le sexe est requis")]
    [RegularExpression("^[MF]$", ErrorMessage = "Le sexe doit Ãªtre M ou F")]
    public string Sexe { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le tÃ©lÃ©phone est requis")]
    [Phone(ErrorMessage = "Format de tÃ©lÃ©phone invalide")]
    public string Telephone { get; set; } = string.Empty;
    
    // ========== Informations personnelles (optionnelles) ==========
    
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string? Email { get; set; }
    
    public string? SituationMatrimoniale { get; set; }
    
    [Required(ErrorMessage = "L'adresse est requise")]
    public string Adresse { get; set; } = string.Empty;
    
    public string Nationalite { get; set; } = "Cameroun";
    
    public string? RegionOrigine { get; set; }
    
    public string? Ethnie { get; set; }
    
    public string? Profession { get; set; }
    
    // ========== Informations mÃ©dicales ==========
    
    public string? GroupeSanguin { get; set; }
    
    /// <summary>Liste des maladies chroniques (sÃ©parÃ©es par virgule)</summary>
    public string? MaladiesChroniques { get; set; }
    
    public bool? OperationsChirurgicales { get; set; }
    
    public string? OperationsDetails { get; set; }
    
    public bool? AllergiesConnues { get; set; }
    
    public string? AllergiesDetails { get; set; }
    
    public bool? AntecedentsFamiliaux { get; set; }
    
    public string? AntecedentsFamiliauxDetails { get; set; }
    
    // ========== Habitudes de vie ==========
    
    public bool? ConsommationAlcool { get; set; }
    
    public string? FrequenceAlcool { get; set; }
    
    public bool? Tabagisme { get; set; }
    
    public bool? ActivitePhysique { get; set; }
    
    // ========== Contacts d'urgence ==========
    
    public int? NbEnfants { get; set; }
    
    public string? PersonneContact { get; set; }
    
    public string? NumeroContact { get; set; }
    
    // ========== Assurance ==========
    
    /// <summary>ID de l'assurance (null si non assurÃ©)</summary>
    public int? AssuranceId { get; set; }
    
    /// <summary>NumÃ©ro de carte d'assurance</summary>
    [StringLength(100)]
    public string? NumeroCarteAssurance { get; set; }
    
    /// <summary>Date de dÃ©but de validitÃ© de l'assurance</summary>
    public DateTime? DateDebutValidite { get; set; }
    
    /// <summary>Date de fin de validitÃ© de l'assurance</summary>
    public DateTime? DateFinValidite { get; set; }
    
    /// <summary>Taux de couverture propre au patient (0-100)</summary>
    [Range(0, 100)]
    public decimal? TauxCouvertureOverride { get; set; }

    /// <summary>
    /// Alias pour compatibilitÃ© avec l'ancien champ CouvertureAssurance.
    /// Permet aux anciens formulaires de continuer Ã  envoyer la valeur.
    /// </summary>
    [Range(0, 100)]
    public decimal? CouvertureAssurance
    {
        get => TauxCouvertureOverride;
        set => TauxCouvertureOverride = value;
    }
}

/// <summary>
/// RÃ©ponse aprÃ¨s crÃ©ation d'un patient par l'accueil
/// </summary>
public class CreatePatientByReceptionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    
    /// <summary>ID de l'utilisateur crÃ©Ã©</summary>
    public int? IdUser { get; set; }
    
    /// <summary>NumÃ©ro de dossier gÃ©nÃ©rÃ©</summary>
    public string? NumeroDossier { get; set; }
    
    /// <summary>Mot de passe temporaire gÃ©nÃ©rÃ© (Ã  communiquer au patient)</summary>
    public string? TemporaryPassword { get; set; }
    
    /// <summary>Instructions de premiÃ¨re connexion</summary>
    public string? LoginInstructions { get; set; }
    
    /// <summary>Identifiant de connexion (tÃ©lÃ©phone ou email)</summary>
    public string? LoginIdentifier { get; set; }
}

/// <summary>
/// DTO pour la validation de premiÃ¨re connexion (dÃ©claration + changement mot de passe)
/// </summary>
public class FirstLoginValidationRequest
{
    [Required(ErrorMessage = "La dÃ©claration sur l'honneur est requise")]
    [JsonRequired]
    public bool DeclarationHonneurAcceptee { get; set; }
    
    [Required(ErrorMessage = "Le nouveau mot de passe est requis")]
    [MinLength(8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractÃ¨res")]
    public string NewPassword { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
    [Compare("NewPassword", ErrorMessage = "Les mots de passe ne correspondent pas")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// RequÃªte pour valider uniquement la dÃ©claration sur l'honneur
/// </summary>
public class AcceptDeclarationRequest
{
    [JsonRequired]
    public bool DeclarationHonneurAcceptee { get; set; }
}

/// <summary>
/// RÃ©ponse de validation de la dÃ©claration
/// </summary>
public class AcceptDeclarationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// RÃ©ponse de validation de premiÃ¨re connexion
/// </summary>
public class FirstLoginValidationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    
    /// <summary>Nouveau token JWT aprÃ¨s validation</summary>
    public string? Token { get; set; }
    
    /// <summary>DurÃ©e de validitÃ© du token en secondes</summary>
    public int ExpiresIn { get; set; }
}

/// <summary>
/// DTO pour rÃ©cupÃ©rer les informations du patient pour la page de premiÃ¨re connexion
/// </summary>
public class FirstLoginPatientInfoResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    
    // Informations personnelles
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public DateTime? DateNaissance { get; set; }
    public string? Sexe { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? SituationMatrimoniale { get; set; }
    public string? Adresse { get; set; }
    public string? Nationalite { get; set; }
    public string? RegionOrigine { get; set; }
    public string? Ethnie { get; set; }
    public string? Profession { get; set; }
    
    // Informations mÃ©dicales
    public string? GroupeSanguin { get; set; }
    public string? MaladiesChroniques { get; set; }
    public bool? OperationsChirurgicales { get; set; }
    public string? OperationsDetails { get; set; }
    public bool? AllergiesConnues { get; set; }
    public string? AllergiesDetails { get; set; }
    public bool? AntecedentsFamiliaux { get; set; }
    public string? AntecedentsFamiliauxDetails { get; set; }
    
    // Habitudes de vie
    public bool? ConsommationAlcool { get; set; }
    public string? FrequenceAlcool { get; set; }
    public bool? Tabagisme { get; set; }
    public bool? ActivitePhysique { get; set; }
    
    // Contacts d'urgence
    public int? NbEnfants { get; set; }
    public string? PersonneContact { get; set; }
    public string? NumeroContact { get; set; }
    
    // NumÃ©ro de dossier
    public string? NumeroDossier { get; set; }
    
    // Statuts
    public bool MustChangePassword { get; set; }
    public bool DeclarationHonneurAcceptee { get; set; }
    public bool ProfileCompleted { get; set; }
}
