using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities.Pharmacie;

/// <summary>
/// Entité Inventaire - Mappe à la table 'inventaire'
/// </summary>
[Table("inventaire")]
public class Inventaire
{
    [Key]
    [Column("id_inventaire")]
    public int IdInventaire { get; set; }

    [Column("date_inventaire")]
    public DateTime DateInventaire { get; set; }

    [Column("statut")]
    public string Statut { get; set; } = "planifie";

    [Column("id_user_responsable")]
    public int IdUserResponsable { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("IdUserResponsable")]
    public virtual Utilisateur? Responsable { get; set; }

    public virtual ICollection<InventaireLigne>? Lignes { get; set; }
}

/// <summary>
/// Entité InventaireLigne - Mappe à la table 'inventaire_ligne'
/// </summary>
[Table("inventaire_ligne")]
public class InventaireLigne
{
    [Key]
    [Column("id_ligne_inventaire")]
    public int IdLigneInventaire { get; set; }

    [Column("id_inventaire")]
    public int IdInventaire { get; set; }

    [Column("id_medicament")]
    public int IdMedicament { get; set; }

    [Column("quantite_theorique")]
    public int QuantiteTheorique { get; set; }

    [Column("quantite_reelle")]
    public int QuantiteReelle { get; set; }

    [Column("ecart")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public int? Ecart { get; set; }

    [Column("commentaire")]
    public string? Commentaire { get; set; }

    // Navigation
    [ForeignKey("IdInventaire")]
    public virtual Inventaire? Inventaire { get; set; }

    [ForeignKey("IdMedicament")]
    public virtual Medicament? Medicament { get; set; }
}
