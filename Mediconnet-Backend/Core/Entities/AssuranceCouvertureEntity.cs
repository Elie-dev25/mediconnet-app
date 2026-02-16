using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Définit le taux de couverture d'une assurance pour un type de prestation donné.
/// Permet de différencier la prise en charge selon le type d'acte médical.
/// </summary>
[Table("assurance_couverture")]
public class AssuranceCouverture
{
    [Key]
    [Column("id_couverture")]
    public int IdCouverture { get; set; }

    [Column("id_assurance")]
    public int IdAssurance { get; set; }

    /// <summary>
    /// Type de prestation : consultation, hospitalisation, examen, pharmacie
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("type_prestation")]
    public string TypePrestation { get; set; } = "";

    /// <summary>
    /// Taux de couverture en pourcentage (0-100). Ex: 80 = 80%
    /// </summary>
    [Column("taux_couverture")]
    public decimal TauxCouverture { get; set; }

    /// <summary>
    /// Plafond annuel de remboursement pour ce type de prestation (null = pas de plafond)
    /// </summary>
    [Column("plafond_annuel")]
    public decimal? PlafondAnnuel { get; set; }

    /// <summary>
    /// Plafond par acte/facture pour ce type de prestation (null = pas de plafond)
    /// </summary>
    [Column("plafond_par_acte")]
    public decimal? PlafondParActe { get; set; }

    /// <summary>
    /// Franchise (montant minimum non couvert par l'assurance, à la charge du patient)
    /// </summary>
    [Column("franchise")]
    public decimal? Franchise { get; set; }

    [Column("actif")]
    public bool Actif { get; set; } = true;

    [Column("notes")]
    [MaxLength(500)]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("IdAssurance")]
    public virtual Assurance? Assurance { get; set; }
}
