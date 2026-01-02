using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité représentant une notification utilisateur
/// </summary>
[Table("notifications")]
public class Notification
{
    [Key]
    [Column("id_notification")]
    public int IdNotification { get; set; }

    [Required]
    [Column("id_user")]
    public int IdUser { get; set; }

    [Required]
    [Column("type")]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty; // rdv, facture, alerte, systeme, stock, consultation, etc.

    [Required]
    [Column("titre")]
    [StringLength(200)]
    public string Titre { get; set; } = string.Empty;

    [Required]
    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("lien")]
    [StringLength(500)]
    public string? Lien { get; set; } // Lien vers la ressource concernée (ex: /patient/consultations/123)

    [Column("icone")]
    [StringLength(50)]
    public string? Icone { get; set; } // Nom de l'icône Lucide (calendar, credit-card, alert-triangle, etc.)

    [Column("priorite")]
    [StringLength(20)]
    public string Priorite { get; set; } = "normale"; // basse, normale, haute, urgente

    [Column("lu")]
    public bool Lu { get; set; } = false;

    [Column("date_lecture")]
    public DateTime? DateLecture { get; set; }

    [Column("date_creation")]
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    [Column("date_expiration")]
    public DateTime? DateExpiration { get; set; }

    [Column("metadata")]
    public string? Metadata { get; set; } // JSON avec données supplémentaires

    [Column("supprime")]
    public bool Supprime { get; set; } = false;

    // Navigation
    [ForeignKey("IdUser")]
    public virtual Utilisateur? Utilisateur { get; set; }
}

/// <summary>
/// Types de notifications prédéfinis
/// </summary>
public static class NotificationType
{
    public const string RendezVous = "rdv";
    public const string Facture = "facture";
    public const string Consultation = "consultation";
    public const string Alerte = "alerte";
    public const string AlerteMedicale = "alerte_medicale";
    public const string Stock = "stock";
    public const string Systeme = "systeme";
    public const string Message = "message";
    public const string Rappel = "rappel";
    public const string Validation = "validation";
}

/// <summary>
/// Priorités de notifications
/// </summary>
public static class NotificationPriority
{
    public const string Basse = "basse";
    public const string Normale = "normale";
    public const string Haute = "haute";
    public const string Urgente = "urgente";
}
