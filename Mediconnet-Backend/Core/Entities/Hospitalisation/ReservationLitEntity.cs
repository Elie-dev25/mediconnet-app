using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities.GestionLits;

/// <summary>
/// Entité représentant une réservation de lit
/// </summary>
[Table("ReservationsLits")]
public class ReservationLit
{
    [Key]
    public int IdReservation { get; set; }
    
    [Required]
    public int IdLit { get; set; }
    
    [Required]
    public int IdPatient { get; set; }
    
    [Required]
    public DateTime DateReservation { get; set; }
    
    public DateTime? DateExpiration { get; set; }
    
    [Required]
    [StringLength(20)]
    public string Statut { get; set; } = "active"; // active, utilisee, expiree, annulee
    
    public string? Notes { get; set; }
    
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    
    public int? CreePar { get; set; }
    
    // Navigation
    [ForeignKey("IdLit")]
    public virtual Lit? Lit { get; set; }
    
    [ForeignKey("IdPatient")]
    public virtual Patient? Patient { get; set; }
}

/// <summary>
/// Entité représentant un transfert de patient entre lits
/// </summary>
[Table("TransfertsLits")]
public class TransfertLit
{
    [Key]
    public int IdTransfert { get; set; }
    
    [Required]
    public int IdAdmission { get; set; }
    
    [Required]
    public int IdPatient { get; set; }
    
    [Required]
    public int IdLitOrigine { get; set; }
    
    [Required]
    public int IdLitDestination { get; set; }
    
    [Required]
    public string Motif { get; set; } = string.Empty;
    
    [Required]
    public DateTime DateTransfert { get; set; } = DateTime.UtcNow;
    
    public int? EffectuePar { get; set; }
    
    public string? Notes { get; set; }
    
    // Navigation
    [ForeignKey("IdAdmission")]
    public virtual Mediconnet_Backend.Core.Entities.Hospitalisation? HospitalisationEntity { get; set; }
    
    [ForeignKey("IdPatient")]
    public virtual Patient? Patient { get; set; }
    
    [ForeignKey("IdLitOrigine")]
    public virtual Lit? LitOrigine { get; set; }
    
    [ForeignKey("IdLitDestination")]
    public virtual Lit? LitDestination { get; set; }
}

/// <summary>
/// Entité représentant la maintenance d'un lit
/// </summary>
[Table("MaintenancesLits")]
public class MaintenanceLit
{
    [Key]
    public int IdMaintenance { get; set; }
    
    [Required]
    public int IdLit { get; set; }
    
    [Required]
    public string Motif { get; set; } = string.Empty;
    
    [Required]
    public DateTime DateDebut { get; set; } = DateTime.UtcNow;
    
    public DateTime? DateFinPrevue { get; set; }
    
    public DateTime? DateFin { get; set; }
    
    [Required]
    [StringLength(20)]
    public string Statut { get; set; } = "en_cours"; // en_cours, terminee
    
    public int? EffectuePar { get; set; }
    
    public string? Notes { get; set; }
    
    // Navigation
    [ForeignKey("IdLit")]
    public virtual Lit? Lit { get; set; }
}
