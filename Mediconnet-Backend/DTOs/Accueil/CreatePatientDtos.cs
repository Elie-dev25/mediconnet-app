using System.ComponentModel.DataAnnotations;

namespace Mediconnet_Backend.DTOs.Accueil;

/// <summary>
/// DTO pour la création d'un patient complet par l'accueil
/// Inclut toutes les informations du formulaire register + complétion profil
/// </summary>
public class CreatePatientByReceptionRequest
{
    // ========== Informations personnelles (obligatoires) ==========
    
    [Required(ErrorMessage = "Le nom est requis")]
    [StringLength(100, MinimumLength = 2)]
    public string Nom { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le prénom est requis")]
    [StringLength(100, MinimumLength = 2)]
    public string Prenom { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "La date de naissance est requise")]
    public DateTime DateNaissance { get; set; }
    
    [Required(ErrorMessage = "Le sexe est requis")]
    [RegularExpression("^[MF]$", ErrorMessage = "Le sexe doit être M ou F")]
    public string Sexe { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le téléphone est requis")]
    [Phone(ErrorMessage = "Format de téléphone invalide")]
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
    
    // ========== Informations médicales ==========
    
    public string? GroupeSanguin { get; set; }
    
    /// <summary>Liste des maladies chroniques (séparées par virgule)</summary>
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
    
    /// <summary>ID de l'assurance (null si non assuré)</summary>
    public int? AssuranceId { get; set; }
    
    /// <summary>Numéro de carte d'assurance</summary>
    [StringLength(100)]
    public string? NumeroCarteAssurance { get; set; }
    
    /// <summary>Date de début de validité de l'assurance</summary>
    public DateTime? DateDebutValidite { get; set; }
    
    /// <summary>Date de fin de validité de l'assurance</summary>
    public DateTime? DateFinValidite { get; set; }
    
    /// <summary>Taux de couverture propre au patient (0-100)</summary>
    [Range(0, 100)]
    public decimal? CouvertureAssurance { get; set; }
}

/// <summary>
/// Réponse après création d'un patient par l'accueil
/// </summary>
public class CreatePatientByReceptionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    
    /// <summary>ID de l'utilisateur créé</summary>
    public int? IdUser { get; set; }
    
    /// <summary>Numéro de dossier généré</summary>
    public string? NumeroDossier { get; set; }
    
    /// <summary>Mot de passe temporaire généré (à communiquer au patient)</summary>
    public string? TemporaryPassword { get; set; }
    
    /// <summary>Instructions de première connexion</summary>
    public string? LoginInstructions { get; set; }
    
    /// <summary>Identifiant de connexion (téléphone ou email)</summary>
    public string? LoginIdentifier { get; set; }
}

/// <summary>
/// DTO pour la validation de première connexion (déclaration + changement mot de passe)
/// </summary>
public class FirstLoginValidationRequest
{
    [Required(ErrorMessage = "La déclaration sur l'honneur est requise")]
    public bool DeclarationHonneurAcceptee { get; set; }
    
    [Required(ErrorMessage = "Le nouveau mot de passe est requis")]
    [MinLength(8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères")]
    public string NewPassword { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
    [Compare("NewPassword", ErrorMessage = "Les mots de passe ne correspondent pas")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Requête pour valider uniquement la déclaration sur l'honneur
/// </summary>
public class AcceptDeclarationRequest
{
    public bool DeclarationHonneurAcceptee { get; set; }
}

/// <summary>
/// Réponse de validation de la déclaration
/// </summary>
public class AcceptDeclarationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Réponse de validation de première connexion
/// </summary>
public class FirstLoginValidationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    
    /// <summary>Nouveau token JWT après validation</summary>
    public string? Token { get; set; }
    
    /// <summary>Durée de validité du token en secondes</summary>
    public int ExpiresIn { get; set; }
}

/// <summary>
/// DTO pour récupérer les informations du patient pour la page de première connexion
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
    
    // Informations médicales
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
    
    // Numéro de dossier
    public string? NumeroDossier { get; set; }
    
    // Statuts
    public bool MustChangePassword { get; set; }
    public bool DeclarationHonneurAcceptee { get; set; }
    public bool ProfileCompleted { get; set; }
}
