using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities.Prescription;

/// <summary>
/// Entité représentant une ordonnance électronique
/// </summary>
[Table("OrdonnancesElectroniques")]
public class OrdonnanceElectronique
{
    [Key]
    public int IdOrdonnance { get; set; }
    
    [Required]
    [StringLength(50)]
    public string CodeUnique { get; set; } = string.Empty;
    
    [Required]
    public int IdPatient { get; set; }
    
    [Required]
    public int IdMedecin { get; set; }
    
    public int? IdConsultation { get; set; }
    
    [Required]
    public DateTime DatePrescription { get; set; } = DateTime.UtcNow;
    
    [Required]
    public DateTime DateExpiration { get; set; }
    
    [Required]
    [StringLength(20)]
    public string Statut { get; set; } = "active"; // active, transmise, dispensee, expiree, annulee
    
    public bool Renouvelable { get; set; }
    
    public int? NombreRenouvellements { get; set; }
    
    public int? RenouvellementRestants { get; set; }
    
    public string? Notes { get; set; }
    
    public string? QRCodeData { get; set; }
    
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    
    // Transmission
    public int? IdPharmacieExterne { get; set; }
    
    public DateTime? DateTransmission { get; set; }
    
    public string? ReferenceTransmission { get; set; }
    
    // Navigation
    [ForeignKey("IdPatient")]
    public virtual Patient? Patient { get; set; }
    
    [ForeignKey("IdMedecin")]
    public virtual Medecin? Medecin { get; set; }
    
    [ForeignKey("IdConsultation")]
    public virtual Consultation? Consultation { get; set; }
    
    [ForeignKey("IdPharmacieExterne")]
    public virtual PharmacieExterne? PharmacieExterne { get; set; }
    
    public virtual ICollection<LignePrescription> Lignes { get; set; } = new List<LignePrescription>();
}

/// <summary>
/// Ligne d'une ordonnance électronique
/// </summary>
[Table("LignesPrescription")]
public class LignePrescription
{
    [Key]
    public int IdLigne { get; set; }
    
    [Required]
    public int IdOrdonnance { get; set; }
    
    [Required]
    public int IdMedicament { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Dosage { get; set; } = string.Empty;
    
    [Required]
    public int Quantite { get; set; }
    
    [Required]
    [StringLength(500)]
    public string Posologie { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? DureeTraitement { get; set; }
    
    public string? Instructions { get; set; }
    
    public bool Substitutable { get; set; } = true;
    
    public bool Dispense { get; set; }
    
    public DateTime? DateDispensation { get; set; }
    
    public int? QuantiteDispensee { get; set; }
    
    [StringLength(200)]
    public string? MedicamentSubstitue { get; set; }
    
    // Navigation
    [ForeignKey("IdOrdonnance")]
    public virtual OrdonnanceElectronique? Ordonnance { get; set; }
    
    [ForeignKey("IdMedicament")]
    public virtual Medicament? Medicament { get; set; }
}

/// <summary>
/// Pharmacie externe partenaire
/// </summary>
[Table("PharmaciesExternes")]
public class PharmacieExterne
{
    [Key]
    public int IdPharmacie { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Nom { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string Adresse { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Ville { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string? Telephone { get; set; }
    
    [StringLength(100)]
    public string? Email { get; set; }
    
    public bool EstConnectee { get; set; }
    
    [StringLength(500)]
    public string? HorairesOuverture { get; set; }
    
    public double? Latitude { get; set; }
    
    public double? Longitude { get; set; }
    
    [StringLength(100)]
    public string? ApiEndpoint { get; set; }
    
    [StringLength(200)]
    public string? ApiKey { get; set; }
    
    public bool Actif { get; set; } = true;
    
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
}
