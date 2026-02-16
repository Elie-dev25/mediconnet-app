using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Table de référence: Types de prestation pour facturation (consultation, hospitalisation, examen, pharmacie)
/// </summary>
[Table("type_prestation")]
public class TypePrestation
{
    [Key]
    [Column("code")]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Column("libelle")]
    [MaxLength(100)]
    public string Libelle { get; set; } = string.Empty;

    [Column("description")]
    [MaxLength(255)]
    public string? Description { get; set; }

    [Column("icone")]
    [MaxLength(50)]
    public string? Icone { get; set; }

    [Column("ordre")]
    public int Ordre { get; set; } = 0;

    [Column("actif")]
    public bool Actif { get; set; } = true;
}

/// <summary>
/// Table de référence: Catégories de bénéficiaires (salariés, familles, retraités, etc.)
/// </summary>
[Table("categorie_beneficiaire")]
public class CategorieBeneficiaire
{
    [Key]
    [Column("id_categorie")]
    public int IdCategorie { get; set; }

    [Required]
    [Column("code")]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Column("libelle")]
    [MaxLength(100)]
    public string Libelle { get; set; } = string.Empty;

    [Column("description")]
    [MaxLength(255)]
    public string? Description { get; set; }

    [Column("actif")]
    public bool Actif { get; set; } = true;

    // Navigation - Assurances qui couvrent cette catégorie
    public virtual ICollection<Assurance> Assurances { get; set; } = new List<Assurance>();
}

/// <summary>
/// Table de référence: Modes de paiement (mobile money, virement, prélèvement, etc.)
/// </summary>
[Table("mode_paiement")]
public class ModePaiement
{
    [Key]
    [Column("id_mode")]
    public int IdMode { get; set; }

    [Required]
    [Column("code")]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Column("libelle")]
    [MaxLength(100)]
    public string Libelle { get; set; } = string.Empty;

    [Column("description")]
    [MaxLength(255)]
    public string? Description { get; set; }

    [Column("actif")]
    public bool Actif { get; set; } = true;

    // Navigation - Assurances qui acceptent ce mode
    public virtual ICollection<Assurance> Assurances { get; set; } = new List<Assurance>();
}

/// <summary>
/// Table de référence: Zones de couverture géographique (national, régional, international, etc.)
/// </summary>
[Table("zone_couverture")]
public class ZoneCouverture
{
    [Key]
    [Column("id_zone")]
    public int IdZone { get; set; }

    [Required]
    [Column("code")]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Column("libelle")]
    [MaxLength(100)]
    public string Libelle { get; set; } = string.Empty;

    [Column("description")]
    [MaxLength(255)]
    public string? Description { get; set; }

    [Column("actif")]
    public bool Actif { get; set; } = true;

    // Navigation - Assurances dans cette zone
    public virtual ICollection<Assurance> Assurances { get; set; } = new List<Assurance>();
}

/// <summary>
/// Table de référence: Types de couverture santé (hospitalisation, maternité, dentaire, etc.)
/// </summary>
[Table("type_couverture_sante")]
public class TypeCouvertureSante
{
    [Key]
    [Column("id_type_couverture")]
    public int IdTypeCouverture { get; set; }

    [Required]
    [Column("code")]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Column("libelle")]
    [MaxLength(100)]
    public string Libelle { get; set; } = string.Empty;

    [Column("description")]
    [MaxLength(255)]
    public string? Description { get; set; }

    [Column("actif")]
    public bool Actif { get; set; } = true;

    // Navigation - Assurances qui offrent ce type de couverture
    public virtual ICollection<Assurance> Assurances { get; set; } = new List<Assurance>();
}
