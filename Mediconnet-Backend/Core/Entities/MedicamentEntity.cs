using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité Médicament - Mappe à la table 'medicament'
/// </summary>
[Table("medicament")]
public class Medicament
{
    [Key]
    [Column("id_medicament")]
    public int IdMedicament { get; set; }

    [Column("nom")]
    [Required]
    public string Nom { get; set; } = "";

    [Column("dosage")]
    public string? Dosage { get; set; }

    [Column("date_heure_creation")]
    public DateTime DateHeureCreation { get; set; }

    [Column("stock")]
    public int? Stock { get; set; }

    [Column("prix")]
    public float? Prix { get; set; }

    [Column("seuil_stock")]
    public int? SeuilStock { get; set; }

    [Column("code_ATC")]
    public string? CodeATC { get; set; }

    [Column("forme_galenique")]
    public string? FormeGalenique { get; set; }

    [Column("laboratoire")]
    public string? Laboratoire { get; set; }

    [Column("conditionnement")]
    public string? Conditionnement { get; set; }

    [Column("date_peremption")]
    public DateTime? DatePeremption { get; set; }

    [Column("actif")]
    public bool Actif { get; set; } = true;

    [Column("emplacement_rayon")]
    public string? EmplacementRayon { get; set; }

    [Column("temperature_conservation")]
    public string? TemperatureConservation { get; set; }
}
