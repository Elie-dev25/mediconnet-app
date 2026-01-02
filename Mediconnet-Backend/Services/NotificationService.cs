using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.Hubs;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service centralisé de gestion des notifications
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ApplicationDbContext context,
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    // ==================== CRÉATION ====================

    public async Task<NotificationDto> CreateAsync(CreateNotificationRequest request)
    {
        var notification = new Notification
        {
            IdUser = request.IdUser,
            Type = request.Type,
            Titre = request.Titre,
            Message = request.Message,
            Lien = request.Lien,
            Icone = request.Icone ?? GetDefaultIcon(request.Type),
            Priorite = request.Priorite,
            DateExpiration = request.DateExpiration,
            Metadata = request.Metadata
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        var dto = MapToDto(notification);

        if (request.SendRealTime)
        {
            await SendRealTimeNotificationAsync(request.IdUser, dto);
        }

        _logger.LogInformation("Notification créée: {Id} pour user {UserId}", notification.IdNotification, request.IdUser);

        return dto;
    }

    public async Task<List<NotificationDto>> CreateBulkAsync(CreateBulkNotificationRequest request)
    {
        var notifications = request.UserIds.Select(userId => new Notification
        {
            IdUser = userId,
            Type = request.Type,
            Titre = request.Titre,
            Message = request.Message,
            Lien = request.Lien,
            Icone = request.Icone ?? GetDefaultIcon(request.Type),
            Priorite = request.Priorite,
            DateExpiration = request.DateExpiration,
            Metadata = request.Metadata
        }).ToList();

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        var dtos = notifications.Select(MapToDto).ToList();

        if (request.SendRealTime)
        {
            foreach (var (notification, index) in notifications.Select((n, i) => (n, i)))
            {
                await SendRealTimeNotificationAsync(notification.IdUser, dtos[index]);
            }
        }

        _logger.LogInformation("Notifications bulk créées: {Count} notifications", notifications.Count);

        return dtos;
    }

    public async Task<int> CreateForRoleAsync(string role, CreateNotificationRequest request)
    {
        var userIds = await _context.Utilisateurs
            .Where(u => u.Role == role)
            .Select(u => u.IdUser)
            .ToListAsync();

        if (!userIds.Any()) return 0;

        var bulkRequest = new CreateBulkNotificationRequest
        {
            UserIds = userIds,
            Type = request.Type,
            Titre = request.Titre,
            Message = request.Message,
            Lien = request.Lien,
            Icone = request.Icone,
            Priorite = request.Priorite,
            DateExpiration = request.DateExpiration,
            Metadata = request.Metadata,
            SendRealTime = request.SendRealTime
        };

        var created = await CreateBulkAsync(bulkRequest);
        return created.Count;
    }

    // ==================== LECTURE ====================

    public async Task<NotificationListResult> GetUserNotificationsAsync(int userId, NotificationFilter? filter = null)
    {
        filter ??= new NotificationFilter();

        var query = _context.Notifications
            .Where(n => n.IdUser == userId && !n.Supprime)
            .AsQueryable();

        // Filtres
        if (!string.IsNullOrEmpty(filter.Type))
            query = query.Where(n => n.Type == filter.Type);

        if (filter.Lu.HasValue)
            query = query.Where(n => n.Lu == filter.Lu.Value);

        if (!string.IsNullOrEmpty(filter.Priorite))
            query = query.Where(n => n.Priorite == filter.Priorite);

        if (filter.DateDebut.HasValue)
            query = query.Where(n => n.DateCreation >= filter.DateDebut.Value);

        if (filter.DateFin.HasValue)
            query = query.Where(n => n.DateCreation <= filter.DateFin.Value);

        // Exclure les expirées
        query = query.Where(n => n.DateExpiration == null || n.DateExpiration > DateTime.UtcNow);

        var totalCount = await query.CountAsync();
        var unreadCount = await query.CountAsync(n => !n.Lu);

        var notifications = await query
            .OrderByDescending(n => n.DateCreation)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new NotificationListResult
        {
            Notifications = notifications.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            UnreadCount = unreadCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<NotificationDto?> GetByIdAsync(int notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        return notification != null ? MapToDto(notification) : null;
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.IdUser == userId && !n.Lu && !n.Supprime &&
                (n.DateExpiration == null || n.DateExpiration > DateTime.UtcNow));
    }

    // ==================== MISE À JOUR ====================

    public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.IdNotification == notificationId && n.IdUser == userId);

        if (notification == null) return false;

        notification.Lu = true;
        notification.DateLecture = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Envoyer mise à jour du compteur
        await SendUnreadCountUpdateAsync(userId);

        return true;
    }

    public async Task<int> MarkAllAsReadAsync(int userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.IdUser == userId && !n.Lu && !n.Supprime)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.Lu = true;
            notification.DateLecture = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Envoyer mise à jour du compteur
        await SendUnreadCountUpdateAsync(userId);

        return notifications.Count;
    }

    public async Task<int> MarkMultipleAsReadAsync(int userId, List<int> notificationIds)
    {
        var notifications = await _context.Notifications
            .Where(n => notificationIds.Contains(n.IdNotification) && n.IdUser == userId && !n.Lu)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.Lu = true;
            notification.DateLecture = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Envoyer mise à jour du compteur
        await SendUnreadCountUpdateAsync(userId);

        return notifications.Count;
    }

    // ==================== SUPPRESSION ====================

    public async Task<bool> DeleteAsync(int notificationId, int userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.IdNotification == notificationId && n.IdUser == userId);

        if (notification == null) return false;

        notification.Supprime = true;
        await _context.SaveChangesAsync();

        // Envoyer mise à jour du compteur si non lue
        if (!notification.Lu)
        {
            await SendUnreadCountUpdateAsync(userId);
        }

        return true;
    }

    public async Task<int> DeleteAllReadAsync(int userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.IdUser == userId && n.Lu && !n.Supprime)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.Supprime = true;
        }

        await _context.SaveChangesAsync();
        return notifications.Count;
    }

    public async Task<int> CleanupExpiredAsync()
    {
        var expired = await _context.Notifications
            .Where(n => n.DateExpiration != null && n.DateExpiration < DateTime.UtcNow && !n.Supprime)
            .ToListAsync();

        foreach (var notification in expired)
        {
            notification.Supprime = true;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Nettoyage: {Count} notifications expirées supprimées", expired.Count);

        return expired.Count;
    }

    // ==================== TEMPS RÉEL ====================

    public async Task SendRealTimeNotificationAsync(int userId, NotificationDto notification)
    {
        try
        {
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("NewNotification", notification);

            // Envoyer aussi le nouveau compteur
            var unreadCount = await GetUnreadCountAsync(userId);
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("UnreadCountUpdate", unreadCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur envoi notification temps réel à user {UserId}", userId);
        }
    }

    public async Task SendRealTimeNotificationToGroupAsync(string group, NotificationDto notification)
    {
        try
        {
            await _hubContext.Clients.Group(group)
                .SendAsync("NewNotification", notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur envoi notification temps réel au groupe {Group}", group);
        }
    }

    private async Task SendUnreadCountUpdateAsync(int userId)
    {
        try
        {
            var unreadCount = await GetUnreadCountAsync(userId);
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("UnreadCountUpdate", unreadCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur envoi mise à jour compteur à user {UserId}", userId);
        }
    }

    // ==================== HELPERS ====================

    private static NotificationDto MapToDto(Notification n) => new()
    {
        IdNotification = n.IdNotification,
        IdUser = n.IdUser,
        Type = n.Type,
        Titre = n.Titre,
        Message = n.Message,
        Lien = n.Lien,
        Icone = n.Icone,
        Priorite = n.Priorite,
        Lu = n.Lu,
        DateLecture = n.DateLecture,
        DateCreation = n.DateCreation,
        Metadata = n.Metadata
    };

    private static string GetDefaultIcon(string type) => type switch
    {
        NotificationType.RendezVous => "calendar",
        NotificationType.Facture => "credit-card",
        NotificationType.Consultation => "stethoscope",
        NotificationType.Alerte => "alert-triangle",
        NotificationType.AlerteMedicale => "heart-pulse",
        NotificationType.Stock => "package",
        NotificationType.Systeme => "settings",
        NotificationType.Message => "message-circle",
        NotificationType.Rappel => "bell",
        NotificationType.Validation => "check-circle",
        _ => "bell"
    };
}
