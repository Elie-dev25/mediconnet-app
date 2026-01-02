using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities.Facturation;

/// <summary>
/// Entité représentant un échéancier de paiement pour une facture
/// </summary>
[Table("Echeanciers")]
public class Echeancier
{
    [Key]
    public int IdEcheancier { get; set; }
    
    [Required]
    public int IdFacture { get; set; }
    
    [Required]
    public decimal MontantTotal { get; set; }
    
    [Required]
    public int NombreEcheances { get; set; }
    
    [Required]
    public decimal MontantParEcheance { get; set; }
    
    [Required]
    public DateTime DateDebut { get; set; }
    
    [Required]
    [StringLength(20)]
    public string Frequence { get; set; } = "mensuel"; // mensuel, bimensuel, hebdomadaire
    
    [Required]
    [StringLength(20)]
    public string Statut { get; set; } = "actif"; // actif, termine, annule
    
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    
    public int? CreePar { get; set; }
    
    // Navigation
    [ForeignKey("IdFacture")]
    public virtual Facture? Facture { get; set; }
    
    public virtual ICollection<Echeance> Echeances { get; set; } = new List<Echeance>();
}

/// <summary>
/// Entité représentant une échéance individuelle
/// </summary>
[Table("Echeances")]
public class Echeance
{
    [Key]
    public int IdEcheance { get; set; }
    
    [Required]
    public int IdEcheancier { get; set; }
    
    [Required]
    public int NumeroEcheance { get; set; }
    
    [Required]
    public decimal Montant { get; set; }
    
    [Required]
    public DateTime DateEcheance { get; set; }
    
    public DateTime? DatePaiement { get; set; }
    
    [Required]
    [StringLength(20)]
    public string Statut { get; set; } = "en_attente"; // en_attente, payee, en_retard
    
    public int? IdTransaction { get; set; }
    
    public string? Notes { get; set; }
    
    // Navigation
    [ForeignKey("IdEcheancier")]
    public virtual Echeancier? Echeancier { get; set; }
    
    [ForeignKey("IdTransaction")]
    public virtual Transaction? Transaction { get; set; }
}

/// <summary>
/// Entité représentant une demande de remboursement assurance
/// </summary>
[Table("DemandesRemboursement")]
public class DemandeRemboursement
{
    [Key]
    public int IdDemande { get; set; }
    
    [Required]
    [StringLength(50)]
    public string NumeroDemande { get; set; } = string.Empty;
    
    [Required]
    public int IdFacture { get; set; }
    
    [Required]
    public int IdAssurance { get; set; }
    
    [Required]
    public int IdPatient { get; set; }
    
    [Required]
    public decimal MontantDemande { get; set; }
    
    public decimal? MontantApprouve { get; set; }
    
    [Required]
    [StringLength(20)]
    public string Statut { get; set; } = "en_attente"; // en_attente, approuvee, rejetee, payee
    
    [Required]
    public DateTime DateDemande { get; set; } = DateTime.UtcNow;
    
    public DateTime? DateTraitement { get; set; }
    
    public string? MotifRejet { get; set; }
    
    public string? ReferenceAssurance { get; set; }
    
    public string? Justificatif { get; set; }
    
    public int? TraitePar { get; set; }
    
    // Navigation
    [ForeignKey("IdFacture")]
    public virtual Facture? Facture { get; set; }
    
    [ForeignKey("IdAssurance")]
    public virtual Assurance? Assurance { get; set; }
    
    [ForeignKey("IdPatient")]
    public virtual Patient? Patient { get; set; }
}
