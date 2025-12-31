using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities.Pharmacie;

/// <summary>
/// Entité CommandePharmacie - Mappe à la table 'commande_pharmacie'
/// </summary>
[Table("commande_pharmacie")]
public class CommandePharmacie
{
    [Key]
    [Column("id_commande")]
    public int IdCommande { get; set; }

    [Column("id_fournisseur")]
    public int IdFournisseur { get; set; }

    [Column("date_commande")]
    public DateTime DateCommande { get; set; }

    [Column("date_reception_prevue")]
    public DateTime? DateReceptionPrevue { get; set; }

    [Column("date_reception_reelle")]
    public DateTime? DateReceptionReelle { get; set; }

    [Column("statut")]
    public string Statut { get; set; } = "brouillon";

    [Column("montant_total")]
    public decimal MontantTotal { get; set; }

    [Column("id_user")]
    public int IdUser { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("IdFournisseur")]
    public virtual Fournisseur? Fournisseur { get; set; }

    [ForeignKey("IdUser")]
    public virtual Utilisateur? Utilisateur { get; set; }

    public virtual ICollection<CommandeLigne>? Lignes { get; set; }
}

/// <summary>
/// Entité CommandeLigne - Mappe à la table 'commande_ligne'
/// </summary>
[Table("commande_ligne")]
public class CommandeLigne
{
    [Key]
    [Column("id_ligne_commande")]
    public int IdLigneCommande { get; set; }

    [Column("id_commande")]
    public int IdCommande { get; set; }

    [Column("id_medicament")]
    public int IdMedicament { get; set; }

    [Column("quantite_commandee")]
    public int QuantiteCommandee { get; set; }

    [Column("quantite_recue")]
    public int QuantiteRecue { get; set; }

    [Column("prix_achat")]
    public decimal PrixAchat { get; set; }

    [Column("date_peremption")]
    public DateTime? DatePeremption { get; set; }

    [Column("numero_lot")]
    public string? NumeroLot { get; set; }

    // Navigation
    [ForeignKey("IdCommande")]
    public virtual CommandePharmacie? Commande { get; set; }

    [ForeignKey("IdMedicament")]
    public virtual Medicament? Medicament { get; set; }
}
