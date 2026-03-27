using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities.Pharmacie;

/// <summary>
/// Entité Dispensation - Mappe à la table 'dispensation'
/// Supporte les ventes avec ordonnance ET les ventes directes
/// </summary>
[Table("dispensation")]
public class Dispensation
{
    [Key]
    [Column("id_dispensation")]
    public int IdDispensation { get; set; }

    [Column("id_prescription")]
    public int? IdPrescription { get; set; }

    [Column("id_pharmacien")]
    public int? IdPharmacien { get; set; }

    [Column("id_patient")]
    public int? IdPatient { get; set; }

    [Column("date_dispensation")]
    public DateTime DateDispensation { get; set; } = DateTime.UtcNow;

    [Column("statut")]
    public string Statut { get; set; } = "en_cours";

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("type_vente")]
    [Required]
    public string TypeVente { get; set; } = "avec_ordonnance";

    [Column("nom_client")]
    [MaxLength(100)]
    public string? NomClient { get; set; }

    [Column("telephone_client")]
    [MaxLength(20)]
    public string? TelephoneClient { get; set; }

    [Column("montant_total")]
    public decimal? MontantTotal { get; set; }

    [Column("mode_paiement")]
    [MaxLength(50)]
    public string? ModePaiement { get; set; }

    [Column("numero_ticket")]
    [MaxLength(50)]
    public string? NumeroTicket { get; set; }

    // Navigation
    [ForeignKey("IdPrescription")]
    public virtual Ordonnance? Prescription { get; set; }

    [ForeignKey("IdPharmacien")]
    public virtual Pharmacien? Pharmacien { get; set; }

    [ForeignKey("IdPatient")]
    public virtual Patient? Patient { get; set; }

    public virtual ICollection<DispensationLigne>? Lignes { get; set; }

    // Helpers
    public bool EstVenteDirecte => TypeVente == "vente_directe";
    public bool EstAvecOrdonnance => TypeVente == "avec_ordonnance";
}

/// <summary>
/// Entité DispensationLigne - Mappe à la table 'dispensation_ligne'
/// </summary>
[Table("dispensation_ligne")]
public class DispensationLigne
{
    [Key]
    [Column("id_ligne")]
    public int IdLigne { get; set; }

    [Column("id_dispensation")]
    public int IdDispensation { get; set; }

    [Column("id_medicament")]
    public int IdMedicament { get; set; }

    [Column("quantite_prescrite")]
    public int QuantitePrescrite { get; set; }

    [Column("quantite_dispensee")]
    public int QuantiteDispensee { get; set; }

    [Column("prix_unitaire")]
    public decimal? PrixUnitaire { get; set; }

    [Column("montant_total")]
    public decimal? MontantTotal { get; set; }

    [Column("numero_lot")]
    public string? NumeroLot { get; set; }

    [Column("date_peremption")]
    public DateTime? DatePeremption { get; set; }

    // Navigation
    [ForeignKey("IdDispensation")]
    public virtual Dispensation? Dispensation { get; set; }

    [ForeignKey("IdMedicament")]
    public virtual Medicament? Medicament { get; set; }
}
