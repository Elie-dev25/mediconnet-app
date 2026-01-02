using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities.DMP;

/// <summary>
/// Dossier Médical Partagé d'un patient
/// </summary>
[Table("DossiersMP")]
public class DossierMedicalPartage
{
    [Key]
    public int IdDMP { get; set; }
    
    [Required]
    public int IdPatient { get; set; }
    
    [StringLength(50)]
    public string? IdentifiantNational { get; set; }
    
    [Required]
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    
    public DateTime? DateDerniereSync { get; set; }
    
    [Required]
    [StringLength(20)]
    public string Statut { get; set; } = "actif"; // actif, inactif, ferme
    
    public bool SyncAvecNational { get; set; }
    
    public bool ConsentementPatient { get; set; }
    
    public DateTime? DateConsentement { get; set; }
    
    public DateTime? DateFermeture { get; set; }
    
    public string? MotifFermeture { get; set; }
    
    // Navigation
    [ForeignKey("IdPatient")]
    public virtual Patient? Patient { get; set; }
    
    public virtual ICollection<DocumentDMP> Documents { get; set; } = new List<DocumentDMP>();
    
    public virtual ICollection<AutorisationDMP> Autorisations { get; set; } = new List<AutorisationDMP>();
    
    public virtual ICollection<AccesDMP> Acces { get; set; } = new List<AccesDMP>();
}

/// <summary>
/// Document stocké dans le DMP
/// </summary>
[Table("DocumentsDMP")]
public class DocumentDMP
{
    [Key]
    public int IdDocument { get; set; }
    
    [Required]
    public int IdDMP { get; set; }
    
    [Required]
    public int IdPatient { get; set; }
    
    [Required]
    [StringLength(50)]
    public string TypeDocument { get; set; } = string.Empty; // consultation, ordonnance, resultat_labo, imagerie, compte_rendu
    
    [Required]
    [StringLength(200)]
    public string Titre { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [Required]
    public DateTime DateDocument { get; set; }
    
    [Required]
    public DateTime DateAjout { get; set; } = DateTime.UtcNow;
    
    [StringLength(200)]
    public string? Auteur { get; set; }
    
    [StringLength(200)]
    public string? Etablissement { get; set; }
    
    [Required]
    [StringLength(20)]
    public string Format { get; set; } = "pdf"; // pdf, image, hl7, fhir
    
    public long TailleFichier { get; set; }
    
    public bool Confidentiel { get; set; }
    
    [StringLength(100)]
    public string? ReferenceExterne { get; set; }
    
    public string? CheminFichier { get; set; }
    
    public byte[]? ContenuFichier { get; set; }
    
    public int? IdConsultation { get; set; }
    
    public int? IdOrdonnance { get; set; }
    
    public bool Supprime { get; set; }
    
    public DateTime? DateSuppression { get; set; }
    
    public string? MotifSuppression { get; set; }
    
    // Navigation
    [ForeignKey("IdDMP")]
    public virtual DossierMedicalPartage? DMP { get; set; }
    
    [ForeignKey("IdPatient")]
    public virtual Patient? Patient { get; set; }
}

/// <summary>
/// Autorisation d'accès au DMP
/// </summary>
[Table("AutorisationsDMP")]
public class AutorisationDMP
{
    [Key]
    public int IdAutorisation { get; set; }
    
    [Required]
    public int IdDMP { get; set; }
    
    [Required]
    public int IdProfessionnel { get; set; }
    
    [Required]
    [StringLength(20)]
    public string TypeAcces { get; set; } = "lecture"; // lecture, ecriture, complet
    
    [Required]
    public DateTime DateAutorisation { get; set; } = DateTime.UtcNow;
    
    public DateTime? DateExpiration { get; set; }
    
    public bool Actif { get; set; } = true;
    
    public string? Motif { get; set; }
    
    public int? AccordePar { get; set; }
    
    // Navigation
    [ForeignKey("IdDMP")]
    public virtual DossierMedicalPartage? DMP { get; set; }
    
    [ForeignKey("IdProfessionnel")]
    public virtual Utilisateur? Professionnel { get; set; }
}

/// <summary>
/// Historique des accès au DMP
/// </summary>
[Table("AccesDMP")]
public class AccesDMP
{
    [Key]
    public int IdAcces { get; set; }
    
    [Required]
    public int IdDMP { get; set; }
    
    [Required]
    public int IdProfessionnel { get; set; }
    
    [Required]
    public DateTime DateAcces { get; set; } = DateTime.UtcNow;
    
    [Required]
    [StringLength(20)]
    public string TypeAcces { get; set; } = "lecture"; // lecture, ecriture, export
    
    public int? IdDocumentConsulte { get; set; }
    
    public string? AdresseIP { get; set; }
    
    public string? Details { get; set; }
    
    // Navigation
    [ForeignKey("IdDMP")]
    public virtual DossierMedicalPartage? DMP { get; set; }
    
    [ForeignKey("IdProfessionnel")]
    public virtual Utilisateur? Professionnel { get; set; }
    
    [ForeignKey("IdDocumentConsulte")]
    public virtual DocumentDMP? DocumentConsulte { get; set; }
}
