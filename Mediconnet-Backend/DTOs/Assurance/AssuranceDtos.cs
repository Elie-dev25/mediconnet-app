using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mediconnet_Backend.DTOs.Assurance;

// ==================== ASSURANCE DTOs ====================

/// <summary>
/// DTO pour afficher une assurance dans une liste
/// </summary>
public class AssuranceListDto
{
    public int IdAssurance { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string TypeAssurance { get; set; } = string.Empty;
    public string? Groupe { get; set; }
    public string? ZoneCouverture { get; set; }
    public bool IsActive { get; set; }
    public int NombrePatientsAssures { get; set; }
}

/// <summary>
/// DTO dÃ©taillÃ© pour une assurance
/// </summary>
public class AssuranceDetailDto
{
    public int IdAssurance { get; set; }
    
    // Identification
    public string Nom { get; set; } = string.Empty;
    public string TypeAssurance { get; set; } = string.Empty;
    public string? SiteWeb { get; set; }
    public string? TelephoneServiceClient { get; set; }
    
    // Informations administratives
    public string? Groupe { get; set; }
    public string? PaysOrigine { get; set; }
    public string? StatutJuridique { get; set; }
    public string? Description { get; set; }
    
    // Couverture santÃ© (normalisÃ©)
    public bool IsComplementaire { get; set; }
    public int? IdZoneCouverture { get; set; }
    public ZoneInfoDto? Zone { get; set; }
    
    // Relations many-to-many (normalisÃ©)
    public List<ReferenceCodeDto> TypesCouvertureSante { get; set; } = new();
    public List<ReferenceCodeDto> CategoriesBeneficiaires { get; set; } = new();
    public List<ReferenceCodeDto> ModesPaiement { get; set; } = new();
    
    // ValiditÃ© et fonctionnement
    public string? ConditionsAdhesion { get; set; }
    public bool IsActive { get; set; }
    
    // Champs legacy (pour compatibilitÃ©)
    public string? TypeCouverture { get; set; }
    public string? CategorieBeneficiaires { get; set; }
    public string? ZoneCouverture { get; set; }
    public string? ModePaiement { get; set; }
    
    // MÃ©tadonnÃ©es
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int NombrePatientsAssures { get; set; }
}

/// <summary>
/// DTO simple pour une rÃ©fÃ©rence (code + libellÃ©)
/// </summary>
public class ReferenceCodeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour la zone de couverture
/// </summary>
public class ZoneInfoDto
{
    public int IdZone { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour crÃ©er une nouvelle assurance (Ã©tape par Ã©tape)
/// </summary>
public class CreateAssuranceDto
{
    // Ã‰tape 1: Identification
    [Required(ErrorMessage = "Le nom de l'assurance est obligatoire")]
    [MaxLength(150)]
    public string Nom { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le type d'assurance est obligatoire")]
    public string TypeAssurance { get; set; } = "privee";

    [MaxLength(255)]
    public string? SiteWeb { get; set; }

    [MaxLength(30)]
    public string? TelephoneServiceClient { get; set; }

    // Ã‰tape 2: Informations administratives
    [MaxLength(100)]
    public string? Groupe { get; set; }

    [MaxLength(100)]
    public string? PaysOrigine { get; set; }

    public string? StatutJuridique { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    // Ã‰tape 3: Couverture santÃ© (normalisÃ©)
    public bool IsComplementaire { get; set; } = false;
    
    /// <summary>ID de la zone de couverture gÃ©ographique</summary>
    public int? IdZoneCouverture { get; set; }
    
    /// <summary>IDs des types de couverture santÃ© (hospitalisation, maternitÃ©, etc.)</summary>
    public List<int> TypesCouvertureSanteIds { get; set; } = new();
    
    /// <summary>IDs des catÃ©gories de bÃ©nÃ©ficiaires</summary>
    public List<int> CategoriesBeneficiairesIds { get; set; } = new();
    
    /// <summary>IDs des modes de paiement acceptÃ©s</summary>
    public List<int> ModesPaiementIds { get; set; } = new();

    // Ã‰tape 4: Fonctionnement
    [MaxLength(1000)]
    public string? ConditionsAdhesion { get; set; }

    public bool IsActive { get; set; } = true;
    
    // Champs legacy (pour compatibilitÃ© pendant la migration)
    [MaxLength(500)]
    public string? TypeCouverture { get; set; }

    [MaxLength(255)]
    public string? CategorieBeneficiaires { get; set; }

    [MaxLength(100)]
    public string? ZoneCouverture { get; set; }

    [MaxLength(255)]
    public string? ModePaiement { get; set; }
}

/// <summary>
/// DTO pour mettre Ã  jour une assurance
/// </summary>
public class UpdateAssuranceDto : CreateAssuranceDto
{
    [JsonRequired]
    public int IdAssurance { get; set; }
}

// ==================== PATIENT ASSURANCE DTOs ====================

/// <summary>
/// DTO pour afficher l'assurance d'un patient
/// </summary>
public class PatientAssuranceInfoDto
{
    public bool EstAssure { get; set; }
    public int? AssuranceId { get; set; }
    public string? NomAssurance { get; set; }
    public string? TypeAssurance { get; set; }
    
    /// <summary>Override manuel du taux de couverture (si dÃ©fini)</summary>
    public decimal? TauxCouvertureOverride { get; set; }
    
    public string? NumeroCarteAssurance { get; set; }
    public DateTime? DateDebutValidite { get; set; }
    public DateTime? DateFinValidite { get; set; }
    public bool EstValide { get; set; }
    
    /// <summary>Retourne le taux override si dÃ©fini</summary>
    public decimal? TauxEffectif => TauxCouvertureOverride;
    
    /// <summary>Alias pour compatibilitÃ©</summary>
    public decimal? CouvertureAssurance => TauxCouvertureOverride;
}

/// <summary>
/// DTO pour mettre Ã  jour l'assurance d'un patient
/// </summary>
public class UpdatePatientAssuranceDto
{
    public int? AssuranceId { get; set; }

    [MaxLength(100)]
    public string? NumeroCarteAssurance { get; set; }

    public DateTime? DateDebutValidite { get; set; }

    public DateTime? DateFinValidite { get; set; }
    
    /// <summary>Override manuel du taux de couverture (prioritÃ© sur config assurance)</summary>
    [Range(0, 100)]
    public decimal? TauxCouvertureOverride { get; set; }
    
    /// <summary>Alias pour compatibilitÃ©</summary>
    [Range(0, 100)]
    public decimal? CouvertureAssurance { get => TauxCouvertureOverride; set => TauxCouvertureOverride = value; }
}

// ==================== RESPONSE DTOs ====================

public class AssuranceResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public AssuranceDetailDto? Data { get; set; }
}

public class AssuranceListResponse
{
    public bool Success { get; set; } = true;
    public List<AssuranceListDto> Data { get; set; } = new();
    public int Total { get; set; }
    public int TotalActives { get; set; }
}

public class PatientAssuranceResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public PatientAssuranceInfoDto? Data { get; set; }
}

/// <summary>
/// Options de filtre pour les assurances
/// </summary>
public class AssuranceFilterDto
{
    public string? TypeAssurance { get; set; }
    public string? ZoneCouverture { get; set; }
    public bool? IsActive { get; set; }
    public string? Recherche { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// ==================== PATIENT INSURANCE STATUS DTOs ====================

/// <summary>
/// DTO pour un patient avec statut d'assurance
/// </summary>
public class PatientInsuranceStatusDto
{
    public int IdPatient { get; set; }
    public string NomComplet { get; set; } = string.Empty;
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    
    // Informations assurance
    public int? AssuranceId { get; set; }
    public string? NomAssurance { get; set; }
    public string? NumeroCarteAssurance { get; set; }
    public DateTime? DateDebutValidite { get; set; }
    public DateTime? DateFinValidite { get; set; }
    
    // Statut calculÃ©
    public string StatutAssurance { get; set; } = "non_assure"; // non_assure, valide, expire_bientot, expiree
    public int? JoursRestants { get; set; }
    public int? JoursExpires { get; set; }
    public bool EstValide { get; set; }
}

/// <summary>
/// RÃ©sultat de la liste des patients avec statut d'assurance
/// </summary>
public class PatientInsuranceStatusListResponse
{
    public bool Success { get; set; } = true;
    public List<PatientInsuranceStatusDto> Data { get; set; } = new();
    public int Total { get; set; }
    public int TotalExpirees { get; set; }
    public int TotalExpirantBientot { get; set; }
    public int TotalNonAssures { get; set; }
}

/// <summary>
/// Filtre pour les patients par statut d'assurance
/// </summary>
public class PatientInsuranceFilterDto
{
    /// <summary>Filtrer par statut: all, valide, expire_bientot, expiree, non_assure</summary>
    public string? StatutAssurance { get; set; }
    
    /// <summary>Filtrer par assurance spÃ©cifique</summary>
    public int? AssuranceId { get; set; }
    
    /// <summary>Recherche par nom/prÃ©nom/tÃ©lÃ©phone</summary>
    public string? Recherche { get; set; }
    
    /// <summary>Nombre de jours pour "expire bientÃ´t" (dÃ©faut: 30)</summary>
    public int JoursAvertissement { get; set; } = 30;
    
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
