using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities.Pharmacie;

/// <summary>
/// Entité MouvementStock - Mappe à la table 'mouvement_stock'
/// </summary>
[Table("mouvement_stock")]
public class MouvementStock
{
    [Key]
    [Column("id_mouvement")]
    public int IdMouvement { get; set; }

    [Column("id_medicament")]
    public int IdMedicament { get; set; }

    [Column("type_mouvement")]
    public string TypeMouvement { get; set; } = "entree";

    [Column("quantite")]
    public int Quantite { get; set; }

    [Column("date_mouvement")]
    public DateTime DateMouvement { get; set; } = DateTime.UtcNow;

    [Column("motif")]
    public string? Motif { get; set; }

    [Column("reference_id")]
    public int? ReferenceId { get; set; }

    [Column("reference_type")]
    public string? ReferenceType { get; set; }

    [Column("id_user")]
    public int IdUser { get; set; }

    [Column("stock_apres_mouvement")]
    public int StockApresMouvement { get; set; }

    // Navigation
    [ForeignKey("IdMedicament")]
    public virtual Medicament? Medicament { get; set; }

    [ForeignKey("IdUser")]
    public virtual Utilisateur? Utilisateur { get; set; }
}
