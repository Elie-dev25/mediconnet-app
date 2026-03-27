namespace Mediconnet_Backend.DTOs.Admin;

/// <summary>
/// DTO complet pour les détails d'un utilisateur (fiche latérale)
/// </summary>
public class UserDetailsDto
{
    // Informations de base (table utilisateurs)
    public int IdUser { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telephone { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime? Naissance { get; set; }
    public string? Sexe { get; set; }
    public string? Adresse { get; set; }
    public string? SituationMatrimoniale { get; set; }
    public string? Nationalite { get; set; }
    public string? RegionOrigine { get; set; }
    public string? Photo { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime? EmailConfirmedAt { get; set; }
    public bool ProfileCompleted { get; set; }
    public DateTime? ProfileCompletedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Données spécifiques au rôle
    public InfirmierDetailsDto? Infirmier { get; set; }
    public MedecinDetailsDto? Medecin { get; set; }
    public PatientDetailsDto? Patient { get; set; }
}

/// <summary>
/// Détails spécifiques à un infirmier
/// </summary>
public class InfirmierDetailsDto
{
    public string? Matricule { get; set; }
    public string Statut { get; set; } = "actif";
    /// <summary>
    /// Service de rattachement de l'infirmier
    /// </summary>
    public int IdService { get; set; }
    public string? NomService { get; set; }
    public bool IsMajor { get; set; }
    public int? IdServiceMajor { get; set; }
    public string? NomServiceMajor { get; set; }
    public DateTime? DateNominationMajor { get; set; }
    public string? Accreditations { get; set; }
    
    /// <summary>
    /// Spécialité de l'infirmier
    /// </summary>
    public int? IdSpecialite { get; set; }
    public string? CodeSpecialite { get; set; }
    public string? NomSpecialite { get; set; }
    
    /// <summary>
    /// Titre affiché (ex: "Infirmier" ou "Major Pédiatrie")
    /// </summary>
    public string TitreAffiche { get; set; } = "Infirmier";
}

/// <summary>
/// Détails spécifiques à un médecin
/// </summary>
public class MedecinDetailsDto
{
    public string? NumeroOrdre { get; set; }
    public int? IdSpecialite { get; set; }
    public string? NomSpecialite { get; set; }
    public int? IdService { get; set; }
    public string? NomService { get; set; }
}

/// <summary>
/// Détails spécifiques à un patient
/// </summary>
public class PatientDetailsDto
{
    public string? NumeroPatient { get; set; }
    public string? GroupeSanguin { get; set; }
    public string? Allergies { get; set; }
    public string? AntecedentsMedicaux { get; set; }
    public string? ContactUrgenceNom { get; set; }
    public string? ContactUrgenceTelephone { get; set; }
    public bool DeclarationHonneurAcceptee { get; set; }
    public DateTime? DateDeclarationHonneur { get; set; }
    public int? IdAssurance { get; set; }
    public string? NomAssurance { get; set; }
    public string? NumeroAssurance { get; set; }
}

/// <summary>
/// Request pour mettre à jour le statut d'un infirmier
/// </summary>
public class UpdateInfirmierStatutRequest
{
    public string Statut { get; set; } = string.Empty; // actif, bloque, suspendu
}

/// <summary>
/// Request pour nommer un infirmier Major
/// </summary>
public class NommerInfirmierMajorRequest
{
    public int IdService { get; set; }
}

/// <summary>
/// Request pour révoquer la nomination Major
/// </summary>
public class RevoquerMajorRequest
{
    public string? Motif { get; set; }
}

/// <summary>
/// Request pour mettre à jour les accréditations
/// </summary>
public class UpdateAccreditationsRequest
{
    public string? Accreditations { get; set; }
}
