using System.Text.Json.Serialization;
namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Interface du service centralisÃ© de notifications
/// </summary>
public interface INotificationService
{
    // ==================== CRÃ‰ATION ====================
    /// <summary>CrÃ©er une notification pour un utilisateur</summary>
    Task<NotificationDto> CreateAsync(CreateNotificationRequest request);

    /// <summary>CrÃ©er une notification pour plusieurs utilisateurs</summary>
    Task<List<NotificationDto>> CreateBulkAsync(CreateBulkNotificationRequest request);

    /// <summary>CrÃ©er une notification pour tous les utilisateurs d'un rÃ´le</summary>
    Task<int> CreateForRoleAsync(string role, CreateNotificationRequest request);

    // ==================== LECTURE ====================
    /// <summary>RÃ©cupÃ©rer les notifications d'un utilisateur</summary>
    Task<NotificationListResult> GetUserNotificationsAsync(int userId, NotificationFilter? filter = null);

    /// <summary>RÃ©cupÃ©rer une notification par ID</summary>
    Task<NotificationDto?> GetByIdAsync(int notificationId);

    /// <summary>Compter les notifications non lues</summary>
    Task<int> GetUnreadCountAsync(int userId);

    // ==================== MISE Ã€ JOUR ====================
    /// <summary>Marquer une notification comme lue</summary>
    Task<bool> MarkAsReadAsync(int notificationId, int userId);

    /// <summary>Marquer toutes les notifications comme lues</summary>
    Task<int> MarkAllAsReadAsync(int userId);

    /// <summary>Marquer plusieurs notifications comme lues</summary>
    Task<int> MarkMultipleAsReadAsync(int userId, List<int> notificationIds);

    // ==================== SUPPRESSION ====================
    /// <summary>Supprimer une notification</summary>
    Task<bool> DeleteAsync(int notificationId, int userId);

    /// <summary>Supprimer toutes les notifications lues</summary>
    Task<int> DeleteAllReadAsync(int userId);

    /// <summary>Nettoyer les notifications expirÃ©es</summary>
    Task<int> CleanupExpiredAsync();

    // ==================== TEMPS RÃ‰EL ====================
    /// <summary>Envoyer une notification en temps rÃ©el via SignalR</summary>
    Task SendRealTimeNotificationAsync(int userId, NotificationDto notification);

    /// <summary>Envoyer une notification Ã  un groupe</summary>
    Task SendRealTimeNotificationToGroupAsync(string group, NotificationDto notification);
}

// ==================== DTOs ====================

public class NotificationDto
{
    public int IdNotification { get; set; }
    public int IdUser { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Titre { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Lien { get; set; }
    public string? Icone { get; set; }
    public string Priorite { get; set; } = "normale";
    public bool Lu { get; set; }
    public DateTime? DateLecture { get; set; }
    public DateTime DateCreation { get; set; }
    public string? Metadata { get; set; }
    public string TempsEcoule => GetTempsEcoule();

    private string GetTempsEcoule()
    {
        var diff = DateTime.UtcNow - DateCreation;
        if (diff.TotalMinutes < 1) return "Ã€ l'instant";
        if (diff.TotalMinutes < 60) return $"Il y a {(int)diff.TotalMinutes} min";
        if (diff.TotalHours < 24) return $"Il y a {(int)diff.TotalHours}h";
        if (diff.TotalDays < 7) return $"Il y a {(int)diff.TotalDays}j";
        return DateCreation.ToString("dd/MM/yyyy");
    }
}

public class CreateNotificationRequest
{
    [JsonRequired]
    public int IdUser { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Titre { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Lien { get; set; }
    public string? Icone { get; set; }
    public string Priorite { get; set; } = "normale";
    public DateTime? DateExpiration { get; set; }
    public string? Metadata { get; set; }
    public bool SendRealTime { get; set; } = true;
}

public class CreateBulkNotificationRequest
{
    public List<int> UserIds { get; set; } = new();
    public string Type { get; set; } = string.Empty;
    public string Titre { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Lien { get; set; }
    public string? Icone { get; set; }
    public string Priorite { get; set; } = "normale";
    public DateTime? DateExpiration { get; set; }
    public string? Metadata { get; set; }
    public bool SendRealTime { get; set; } = true;
}

public class NotificationFilter
{
    public string? Type { get; set; }
    public bool? Lu { get; set; }
    public string? Priorite { get; set; }
    public DateTime? DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class NotificationListResult
{
    public List<NotificationDto> Notifications { get; set; } = new();
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
