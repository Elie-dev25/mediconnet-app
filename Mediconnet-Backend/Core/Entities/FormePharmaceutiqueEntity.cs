using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité Forme Pharmaceutique - Mappe à la table 'forme_pharmaceutique'
/// </summary>
[Table("forme_pharmaceutique")]
public class FormePharmaceutique
{
    [Key]
    [Column("id_forme")]
    public int IdForme { get; set; }

    [Column("code")]
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = "";

    [Column("libelle")]
    [Required]
    [MaxLength(100)]
    public string Libelle { get; set; } = "";

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

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation - Médicaments associés via table de liaison
    public virtual ICollection<MedicamentForme> MedicamentFormes { get; set; } = new List<MedicamentForme>();
}

/// <summary>
/// Table de liaison Medicament-Forme (many-to-many)
/// </summary>
[Table("medicament_forme")]
public class MedicamentForme
{
    [Column("id_medicament")]
    public int IdMedicament { get; set; }

    [Column("id_forme")]
    public int IdForme { get; set; }

    [Column("est_defaut")]
    public bool EstDefaut { get; set; } = false;

    // Navigation
    [ForeignKey("IdMedicament")]
    public virtual Medicament Medicament { get; set; } = null!;

    [ForeignKey("IdForme")]
    public virtual FormePharmaceutique FormePharmaceutique { get; set; } = null!;
}
