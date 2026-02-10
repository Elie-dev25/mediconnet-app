using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Mediconnet_Backend.Core.Entities;

namespace Mediconnet_Backend.Core.Entities.Documents;

/// <summary>
/// Entité représentant un document médical stocké physiquement
/// Table: documents_medicaux
/// </summary>
[Table("documents_medicaux")]
public class DocumentMedical
{
    [Key]
    [Column("uuid")]
    [StringLength(36)]
    public string Uuid { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [Column("nom_fichier_original")]
    [StringLength(255)]
    public string NomFichierOriginal { get; set; } = string.Empty;
    
    [Required]
    [Column("nom_fichier_stockage")]
    [StringLength(255)]
    public string NomFichierStockage { get; set; } = string.Empty;
    
    [Required]
    [Column("chemin_relatif")]
    [StringLength(500)]
    public string CheminRelatif { get; set; } = string.Empty;
    
    [Column("extension")]
    [StringLength(20)]
    public string? Extension { get; set; }
    
    [Required]
    [Column("mime_type")]
    [StringLength(100)]
    public string MimeType { get; set; } = string.Empty;
    
    [Column("taille_octets")]
    public ulong TailleOctets { get; set; }
    
    [Column("hash_sha256")]
    [StringLength(64)]
    public string? HashSha256 { get; set; }
    
    [Column("hash_calcule_at")]
    public DateTime? HashCalculeAt { get; set; }
    
    [Required]
    [Column("type_document")]
    [StringLength(50)]
    public string TypeDocument { get; set; } = "autre";
    
    [Column("sous_type")]
    [StringLength(100)]
    public string? SousType { get; set; }
    
    [Required]
    [Column("niveau_confidentialite")]
    [StringLength(20)]
    public string NiveauConfidentialite { get; set; } = "normal";
    
    [Column("acces_patient")]
    public bool AccesPatient { get; set; } = true;
    
    [Column("acces_restreint_roles")]
    public string? AccesRestreintRoles { get; set; }
    
    [Required]
    [Column("id_patient")]
    public int IdPatient { get; set; }
    
    [Column("id_consultation")]
    public int? IdConsultation { get; set; }
    
    [Column("id_bulletin_examen")]
    public int? IdBulletinExamen { get; set; }
    
    [Column("id_hospitalisation")]
    public int? IdHospitalisation { get; set; }
    
    [Column("id_dmp")]
    public int? IdDmp { get; set; }
    
    [Required]
    [Column("id_createur")]
    public int IdCreateur { get; set; }
    
    [Column("id_validateur")]
    public int? IdValidateur { get; set; }
    
    [Column("date_validation")]
    public DateTime? DateValidation { get; set; }
    
    [Column("version")]
    public uint Version { get; set; } = 1;
    
    [Column("uuid_version_precedente")]
    [StringLength(36)]
    public string? UuidVersionPrecedente { get; set; }
    
    [Column("est_version_courante")]
    public bool EstVersionCourante { get; set; } = true;
    
    [Column("date_document")]
    public DateTime? DateDocument { get; set; }
    
    [Column("description")]
    public string? Description { get; set; }
    
    [Column("tags")]
    public string? Tags { get; set; }
    
    [Required]
    [Column("statut")]
    [StringLength(20)]
    public string Statut { get; set; } = "actif";
    
    [Column("date_archivage")]
    public DateTime? DateArchivage { get; set; }
    
    [Column("motif_archivage")]
    [StringLength(500)]
    public string? MotifArchivage { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    [ForeignKey("IdPatient")]
    public virtual Patient? Patient { get; set; }
    
    [ForeignKey("IdCreateur")]
    public virtual Utilisateur? Createur { get; set; }
    
    [ForeignKey("IdValidateur")]
    public virtual Utilisateur? Validateur { get; set; }
}

/// <summary>
/// Audit des accès aux documents
/// Table: audit_acces_documents
/// </summary>
[Table("audit_acces_documents")]
public class AuditAccesDocument
{
    [Key]
    [Column("id_audit")]
    public long IdAudit { get; set; }
    
    [Required]
    [Column("document_uuid")]
    [StringLength(36)]
    public string DocumentUuid { get; set; } = string.Empty;
    
    [Required]
    [Column("id_utilisateur")]
    public int IdUtilisateur { get; set; }
    
    [Required]
    [Column("role_utilisateur")]
    [StringLength(50)]
    public string RoleUtilisateur { get; set; } = string.Empty;
    
    [Required]
    [Column("type_action")]
    [StringLength(30)]
    public string TypeAction { get; set; } = string.Empty;
    
    [Column("autorise")]
    public bool Autorise { get; set; } = true;
    
    [Column("motif_refus")]
    [StringLength(255)]
    public string? MotifRefus { get; set; }
    
    [Column("ip_address")]
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    [Column("user_agent")]
    [StringLength(500)]
    public string? UserAgent { get; set; }
    
    [Column("session_id")]
    [StringLength(100)]
    public string? SessionId { get; set; }
    
    [Column("endpoint_api")]
    [StringLength(255)]
    public string? EndpointApi { get; set; }
    
    [Column("contexte")]
    public string? Contexte { get; set; }
    
    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // Navigation
    [ForeignKey("IdUtilisateur")]
    public virtual Utilisateur? Utilisateur { get; set; }
}

/// <summary>
/// Vérification d'intégrité des documents
/// Table: verification_integrite
/// </summary>
[Table("verification_integrite")]
public class VerificationIntegrite
{
    [Key]
    [Column("id_verification")]
    public long IdVerification { get; set; }
    
    [Required]
    [Column("document_uuid")]
    [StringLength(36)]
    public string DocumentUuid { get; set; } = string.Empty;
    
    [Required]
    [Column("statut_verification")]
    [StringLength(20)]
    public string StatutVerification { get; set; } = string.Empty;
    
    [Column("hash_attendu")]
    [StringLength(64)]
    public string? HashAttendu { get; set; }
    
    [Column("hash_calcule")]
    [StringLength(64)]
    public string? HashCalcule { get; set; }
    
    [Column("taille_attendue")]
    public ulong? TailleAttendue { get; set; }
    
    [Column("taille_reelle")]
    public ulong? TailleReelle { get; set; }
    
    [Required]
    [Column("type_verification")]
    [StringLength(20)]
    public string TypeVerification { get; set; } = "automatique";
    
    [Column("id_declencheur")]
    public int? IdDeclencheur { get; set; }
    
    [Column("action_corrective")]
    [StringLength(255)]
    public string? ActionCorrective { get; set; }
    
    [Column("alerte_envoyee")]
    public bool AlerteEnvoyee { get; set; }
    
    [Column("date_alerte")]
    public DateTime? DateAlerte { get; set; }
    
    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Alertes système pour le monitoring
/// Table: alertes_systeme
/// </summary>
[Table("alertes_systeme")]
public class AlerteSysteme
{
    [Key]
    [Column("id_alerte")]
    public long IdAlerte { get; set; }
    
    [Required]
    [Column("type_alerte")]
    [StringLength(50)]
    public string TypeAlerte { get; set; } = string.Empty;
    
    [Required]
    [Column("message")]
    public string Message { get; set; } = string.Empty;
    
    [Required]
    [Column("severite")]
    [StringLength(20)]
    public string Severite { get; set; } = "warning";
    
    [Column("source")]
    [StringLength(100)]
    public string? Source { get; set; } = "system";
    
    [Column("details")]
    public string? Details { get; set; }
    
    [Column("acquittee")]
    public bool Acquittee { get; set; }
    
    [Column("acquittee_par")]
    public int? AcquitteePar { get; set; }
    
    [Column("date_acquittement")]
    public DateTime? DateAcquittement { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
