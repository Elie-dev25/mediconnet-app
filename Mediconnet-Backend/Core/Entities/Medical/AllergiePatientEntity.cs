using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities.Medical;

/// <summary>
/// Entité représentant une allergie d'un patient
/// </summary>
[Table("AllergiesPatients")]
public class AllergiePatient
{
    [Key]
    public int IdAllergie { get; set; }
    
    [Required]
    public int IdPatient { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Allergene { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string TypeAllergene { get; set; } = "medicament"; // medicament, aliment, environnement, autre
    
    [Required]
    [StringLength(20)]
    public string Severite { get; set; } = "moderee"; // legere, moderee, severe, anaphylaxie
    
    [StringLength(200)]
    public string? TypeReaction { get; set; }
    
    public DateTime DateDecouverte { get; set; } = DateTime.UtcNow;
    
    public string? Notes { get; set; }
    
    public bool Actif { get; set; } = true;
    
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    
    public int? CreePar { get; set; }
    
    // Navigation
    [ForeignKey("IdPatient")]
    public virtual Patient? Patient { get; set; }
}

/// <summary>
/// Entité représentant une interaction médicamenteuse connue
/// </summary>
[Table("InteractionsMedicamenteuses")]
public class InteractionMedicamenteuse
{
    [Key]
    public int IdInteraction { get; set; }
    
    [Required]
    public int IdMedicament1 { get; set; }
    
    [Required]
    public int IdMedicament2 { get; set; }
    
    [Required]
    [StringLength(100)]
    public string TypeInteraction { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    public string Severite { get; set; } = "moderee"; // faible, moderee, severe, critique
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    public string? Recommandation { get; set; }
    
    public bool Actif { get; set; } = true;
    
    // Navigation
    [ForeignKey("IdMedicament1")]
    public virtual Medicament? Medicament1 { get; set; }
    
    [ForeignKey("IdMedicament2")]
    public virtual Medicament? Medicament2 { get; set; }
}

/// <summary>
/// Entité représentant une contre-indication médicamenteuse
/// </summary>
[Table("ContreIndications")]
public class ContreIndication
{
    [Key]
    public int IdContreIndication { get; set; }
    
    [Required]
    public int IdMedicament { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Condition { get; set; } = string.Empty; // grossesse, insuffisance_renale, diabete, etc.
    
    [Required]
    [StringLength(20)]
    public string TypeContreIndication { get; set; } = "relative"; // absolue, relative
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    public string? Recommandation { get; set; }
    
    public bool Actif { get; set; } = true;
    
    // Navigation
    [ForeignKey("IdMedicament")]
    public virtual Medicament? Medicament { get; set; }
}

/// <summary>
/// Historique des alertes médicales générées
/// </summary>
[Table("AlertesMedicales")]
public class AlerteMedicale
{
    [Key]
    public int IdAlerte { get; set; }
    
    [Required]
    public int IdPatient { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty; // interaction, allergie, contre_indication, dosage
    
    [Required]
    public string Message { get; set; } = string.Empty;
    
    public string? Details { get; set; }
    
    public int? IdMedicament { get; set; }
    
    public int? IdConsultation { get; set; }
    
    public DateTime DateAlerte { get; set; } = DateTime.UtcNow;
    
    public bool Resolue { get; set; }
    
    public DateTime? DateResolution { get; set; }
    
    public int? ResoluePar { get; set; }
    
    public string? ActionPrise { get; set; }
    
    // Navigation
    [ForeignKey("IdPatient")]
    public virtual Patient? Patient { get; set; }
    
    [ForeignKey("IdMedicament")]
    public virtual Medicament? Medicament { get; set; }
}
