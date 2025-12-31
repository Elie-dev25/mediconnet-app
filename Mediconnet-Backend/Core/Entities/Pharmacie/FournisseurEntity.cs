using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities.Pharmacie;

/// <summary>
/// Entité Fournisseur - Mappe à la table 'fournisseur'
/// </summary>
[Table("fournisseur")]
public class Fournisseur
{
    [Key]
    [Column("id_fournisseur")]
    public int IdFournisseur { get; set; }

    [Column("nom_fournisseur")]
    [Required]
    public string NomFournisseur { get; set; } = "";

    [Column("contact_nom")]
    public string? ContactNom { get; set; }

    [Column("contact_email")]
    public string? ContactEmail { get; set; }

    [Column("contact_telephone")]
    public string? ContactTelephone { get; set; }

    [Column("adresse")]
    public string? Adresse { get; set; }

    [Column("conditions_paiement")]
    public string? ConditionsPaiement { get; set; }

    [Column("delai_livraison_jours")]
    public int DelaiLivraisonJours { get; set; } = 7;

    [Column("actif")]
    public bool Actif { get; set; } = true;

    [Column("date_creation")]
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual ICollection<CommandePharmacie>? Commandes { get; set; }
}
