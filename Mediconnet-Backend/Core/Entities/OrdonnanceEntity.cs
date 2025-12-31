using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité Prescription (Ordonnance) - Mappe à la table 'prescription'
/// </summary>
[Table("prescription")]
public class Ordonnance
{
    [Key]
    [Column("id_ord")]
    public int IdOrdonnance { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("id_consultation")]
    public int IdConsultation { get; set; }

    [Column("commentaire")]
    public string? Commentaire { get; set; }

    // Navigation
    [ForeignKey("IdConsultation")]
    public virtual Consultation? Consultation { get; set; }

    public virtual ICollection<PrescriptionMedicament>? Medicaments { get; set; }
}

/// <summary>
/// Entité PrescriptionMedicament - Mappe à la table 'prescription_medicament'
/// </summary>
[Table("prescription_medicament")]
public class PrescriptionMedicament
{
    [Column("id_ord")]
    public int IdOrdonnance { get; set; }

    [Column("id_medicament")]
    public int IdMedicament { get; set; }

    [Column("quantite")]
    public int Quantite { get; set; } = 1;

    [Column("duree_traitement")]
    public string? DureeTraitement { get; set; }

    [Column("posologie")]
    public string? Posologie { get; set; }

    // Navigation
    [ForeignKey("IdOrdonnance")]
    public virtual Ordonnance? Ordonnance { get; set; }

    [ForeignKey("IdMedicament")]
    public virtual Medicament? Medicament { get; set; }
}
