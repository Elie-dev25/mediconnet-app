using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité AuditLog - Journal d'audit pour la traçabilité des actions
/// Conformité RGPD et HDS (Hébergeur de Données de Santé)
/// </summary>
[Table("audit_logs")]
public class AuditLog
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>ID de l'utilisateur ayant effectué l'action (0 si non authentifié)</summary>
    [Column("user_id")]
    public int UserId { get; set; }

    /// <summary>Action effectuée (ex: LOGIN, CREATE, UPDATE, DELETE, SENSITIVE_DATA_ACCESS)</summary>
    [Required]
    [MaxLength(100)]
    [Column("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>Type de ressource affectée (ex: Patient, Consultation, Ordonnance)</summary>
    [Required]
    [MaxLength(100)]
    [Column("resource_type")]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>ID de la ressource affectée</summary>
    [Column("resource_id")]
    public int? ResourceId { get; set; }

    /// <summary>Détails supplémentaires (JSON ou texte libre)</summary>
    [Column("details")]
    public string? Details { get; set; }

    /// <summary>Adresse IP de l'utilisateur</summary>
    [MaxLength(45)]
    [Column("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>User Agent du navigateur</summary>
    [MaxLength(500)]
    [Column("user_agent")]
    public string? UserAgent { get; set; }

    /// <summary>Indique si l'action a réussi</summary>
    [Column("success")]
    public bool Success { get; set; } = true;

    /// <summary>Date et heure de l'action</summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation (optionnelle)
    [ForeignKey("UserId")]
    public virtual Utilisateur? User { get; set; }
}
