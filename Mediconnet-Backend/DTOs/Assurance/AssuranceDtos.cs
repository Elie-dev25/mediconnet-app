using System.ComponentModel.DataAnnotations;

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
/// DTO détaillé pour une assurance
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
    
    // Couverture santé (normalisé)
    public bool IsComplementaire { get; set; }
    public int? IdZoneCouverture { get; set; }
    public ZoneInfoDto? Zone { get; set; }
    
    // Relations many-to-many (normalisé)
    public List<ReferenceCodeDto> TypesCouvertureSante { get; set; } = new();
    public List<ReferenceCodeDto> CategoriesBeneficiaires { get; set; } = new();
    public List<ReferenceCodeDto> ModesPaiement { get; set; } = new();
    
    // Validité et fonctionnement
    public string? ConditionsAdhesion { get; set; }
    public bool IsActive { get; set; }
    
    // Champs legacy (pour compatibilité)
    public string? TypeCouverture { get; set; }
    public string? CategorieBeneficiaires { get; set; }
    public string? ZoneCouverture { get; set; }
    public string? ModePaiement { get; set; }
    
    // Métadonnées
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int NombrePatientsAssures { get; set; }
}

/// <summary>
/// DTO simple pour une référence (code + libellé)
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
/// DTO pour créer une nouvelle assurance (étape par étape)
/// </summary>
public class CreateAssuranceDto
{
    // Étape 1: Identification
    [Required(ErrorMessage = "Le nom de l'assurance est obligatoire")]
    [MaxLength(150)]
    public string Nom { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le type d'assurance est obligatoire")]
    public string TypeAssurance { get; set; } = "privee";

    [MaxLength(255)]
    public string? SiteWeb { get; set; }

    [MaxLength(30)]
    public string? TelephoneServiceClient { get; set; }

    // Étape 2: Informations administratives
    [MaxLength(100)]
    public string? Groupe { get; set; }

    [MaxLength(100)]
    public string? PaysOrigine { get; set; }

    public string? StatutJuridique { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    // Étape 3: Couverture santé (normalisé)
    public bool IsComplementaire { get; set; } = false;
    
    /// <summary>ID de la zone de couverture géographique</summary>
    public int? IdZoneCouverture { get; set; }
    
    /// <summary>IDs des types de couverture santé (hospitalisation, maternité, etc.)</summary>
    public List<int> TypesCouvertureSanteIds { get; set; } = new();
    
    /// <summary>IDs des catégories de bénéficiaires</summary>
    public List<int> CategoriesBeneficiairesIds { get; set; } = new();
    
    /// <summary>IDs des modes de paiement acceptés</summary>
    public List<int> ModesPaiementIds { get; set; } = new();

    // Étape 4: Fonctionnement
    [MaxLength(1000)]
    public string? ConditionsAdhesion { get; set; }

    public bool IsActive { get; set; } = true;
    
    // Champs legacy (pour compatibilité pendant la migration)
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
/// DTO pour mettre à jour une assurance
/// </summary>
public class UpdateAssuranceDto : CreateAssuranceDto
{
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
    
    /// <summary>Override manuel du taux de couverture (si défini)</summary>
    public decimal? TauxCouvertureOverride { get; set; }
    
    public string? NumeroCarteAssurance { get; set; }
    public DateTime? DateDebutValidite { get; set; }
    public DateTime? DateFinValidite { get; set; }
    public bool EstValide { get; set; }
    
    /// <summary>Retourne le taux override si défini</summary>
    public decimal? TauxEffectif => TauxCouvertureOverride;
    
    /// <summary>Alias pour compatibilité</summary>
    public decimal? CouvertureAssurance => TauxCouvertureOverride;
}

/// <summary>
/// DTO pour mettre à jour l'assurance d'un patient
/// </summary>
public class UpdatePatientAssuranceDto
{
    public int? AssuranceId { get; set; }

    [MaxLength(100)]
    public string? NumeroCarteAssurance { get; set; }

    public DateTime? DateDebutValidite { get; set; }

    public DateTime? DateFinValidite { get; set; }
    
    /// <summary>Override manuel du taux de couverture (priorité sur config assurance)</summary>
    [Range(0, 100)]
    public decimal? TauxCouvertureOverride { get; set; }
    
    /// <summary>Alias pour compatibilité</summary>
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
