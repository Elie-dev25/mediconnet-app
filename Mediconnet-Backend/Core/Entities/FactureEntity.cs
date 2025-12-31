using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité représentant une facture patient
/// </summary>
public class Facture
{
    [Key]
    public int IdFacture { get; set; }

    [Required]
    [MaxLength(30)]
    public string NumeroFacture { get; set; } = string.Empty;

    [Required]
    public int IdPatient { get; set; }

    public int? IdMedecin { get; set; }

    public int? IdService { get; set; }

    public int? IdSpecialite { get; set; }

    public int? IdConsultation { get; set; }

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal MontantTotal { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal MontantPaye { get; set; } = 0;

    [Column(TypeName = "decimal(12,2)")]
    public decimal MontantRestant { get; set; }

    [Required]
    [MaxLength(30)]
    public string Statut { get; set; } = "en_attente"; // en_attente, partiel, payee, annulee, remboursee

    [MaxLength(50)]
    public string? TypeFacture { get; set; } // consultation, examen, pharmacie, hospitalisation

    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public DateTime? DateEcheance { get; set; }

    public DateTime? DatePaiement { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool CouvertureAssurance { get; set; } = false;

    public int? IdAssurance { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? TauxCouverture { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? MontantAssurance { get; set; }

    // Navigation
    public virtual Patient? Patient { get; set; }
    public virtual Medecin? Medecin { get; set; }
    public virtual Service? Service { get; set; }
    public virtual Specialite? Specialite { get; set; }
    public virtual Consultation? Consultation { get; set; }
    public virtual ICollection<LigneFacture> Lignes { get; set; } = new List<LigneFacture>();
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

/// <summary>
/// Ligne de détail d'une facture
/// </summary>
public class LigneFacture
{
    [Key]
    public int IdLigne { get; set; }

    [Required]
    public int IdFacture { get; set; }

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Code { get; set; }

    [Required]
    public int Quantite { get; set; } = 1;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal PrixUnitaire { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Montant { get; set; }

    [MaxLength(50)]
    public string? Categorie { get; set; } // acte, medicament, materiel, etc.

    // Navigation
    public virtual Facture? Facture { get; set; }
}
