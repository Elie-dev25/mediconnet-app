using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité Voie d'Administration - Mappe à la table 'voie_administration'
/// </summary>
[Table("voie_administration")]
public class VoieAdministration
{
    [Key]
    [Column("id_voie")]
    public int IdVoie { get; set; }

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
    public virtual ICollection<MedicamentVoie> MedicamentVoies { get; set; } = new List<MedicamentVoie>();
}

/// <summary>
/// Table de liaison Medicament-Voie (many-to-many)
/// </summary>
[Table("medicament_voie")]
public class MedicamentVoie
{
    [Column("id_medicament")]
    public int IdMedicament { get; set; }

    [Column("id_voie")]
    public int IdVoie { get; set; }

    [Column("est_defaut")]
    public bool EstDefaut { get; set; } = false;

    // Navigation
    [ForeignKey("IdMedicament")]
    public virtual Medicament Medicament { get; set; } = null!;

    [ForeignKey("IdVoie")]
    public virtual VoieAdministration VoieAdministration { get; set; } = null!;
}
