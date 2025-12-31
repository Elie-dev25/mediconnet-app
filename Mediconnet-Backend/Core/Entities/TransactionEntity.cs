using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité représentant une transaction de paiement
/// </summary>
public class Transaction
{
    [Key]
    public int IdTransaction { get; set; }

    [Required]
    [MaxLength(50)]
    public string NumeroTransaction { get; set; } = string.Empty;

    /// <summary>
    /// UUID unique pour éviter les doublons (idempotence)
    /// </summary>
    [Required]
    [MaxLength(36)]
    public string TransactionUuid { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public int IdFacture { get; set; }

    public int? IdPatient { get; set; }

    [Required]
    public int IdCaissier { get; set; }

    public int? IdSessionCaisse { get; set; }

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal Montant { get; set; }

    [Required]
    [MaxLength(30)]
    public string ModePaiement { get; set; } = "especes"; // especes, carte, virement, cheque, assurance, mobile

    [Required]
    [MaxLength(30)]
    public string Statut { get; set; } = "complete"; // complete, partiel, annule, rembourse, en_attente

    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime DateTransaction { get; set; } = DateTime.UtcNow;

    public DateTime? DateAnnulation { get; set; }

    [MaxLength(500)]
    public string? MotifAnnulation { get; set; }

    public int? AnnulePar { get; set; }

    /// <summary>
    /// Pour les paiements fractionnés (split payment)
    /// </summary>
    public bool EstPaiementPartiel { get; set; } = false;

    /// <summary>
    /// Montant reçu (peut être supérieur au montant dû pour le rendu de monnaie)
    /// </summary>
    [Column(TypeName = "decimal(12,2)")]
    public decimal? MontantRecu { get; set; }

    /// <summary>
    /// Rendu de monnaie
    /// </summary>
    [Column(TypeName = "decimal(12,2)")]
    public decimal? RenduMonnaie { get; set; }

    // Navigation
    public virtual Facture? Facture { get; set; }
    public virtual Patient? Patient { get; set; }
    public virtual Caissier? Caissier { get; set; }
    public virtual SessionCaisse? SessionCaisse { get; set; }
}

/// <summary>
/// Session de caisse (ouverture/fermeture)
/// </summary>
public class SessionCaisse
{
    [Key]
    public int IdSession { get; set; }

    [Required]
    public int IdCaissier { get; set; }

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal MontantOuverture { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? MontantFermeture { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? MontantSysteme { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? Ecart { get; set; }

    public DateTime DateOuverture { get; set; } = DateTime.UtcNow;

    public DateTime? DateFermeture { get; set; }

    [Required]
    [MaxLength(20)]
    public string Statut { get; set; } = "ouverte"; // ouverte, fermee, rapprochee

    [MaxLength(500)]
    public string? NotesOuverture { get; set; }

    [MaxLength(500)]
    public string? NotesFermeture { get; set; }

    [MaxLength(500)]
    public string? NotesRapprochement { get; set; }

    public int? ValidePar { get; set; }

    // Navigation
    public virtual Caissier? Caissier { get; set; }
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
