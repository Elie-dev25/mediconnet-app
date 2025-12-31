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
    
    // Couverture santé
    public string? TypeCouverture { get; set; }
    public bool IsComplementaire { get; set; }
    public string? CategorieBeneficiaires { get; set; }
    
    // Validité et fonctionnement
    public string? ConditionsAdhesion { get; set; }
    public string? ZoneCouverture { get; set; }
    public string? ModePaiement { get; set; }
    public bool IsActive { get; set; }
    
    // Métadonnées
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int NombrePatientsAssures { get; set; }
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

    // Étape 3: Couverture santé
    [MaxLength(500)]
    public string? TypeCouverture { get; set; }

    public bool IsComplementaire { get; set; } = false;

    [MaxLength(255)]
    public string? CategorieBeneficiaires { get; set; }

    // Étape 4: Fonctionnement
    [MaxLength(1000)]
    public string? ConditionsAdhesion { get; set; }

    [MaxLength(100)]
    public string? ZoneCouverture { get; set; }

    [MaxLength(255)]
    public string? ModePaiement { get; set; }

    public bool IsActive { get; set; } = true;
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
    public decimal? CouvertureAssurance { get; set; } // Taux de couverture du patient
    public string? NumeroCarteAssurance { get; set; }
    public DateTime? DateDebutValidite { get; set; }
    public DateTime? DateFinValidite { get; set; }
    public bool EstValide { get; set; }
    
    /// <summary>Retourne le taux effectif</summary>
    public decimal TauxEffectif => CouvertureAssurance ?? 0;
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
    
    [Range(0, 100)]
    public decimal? CouvertureAssurance { get; set; }
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
