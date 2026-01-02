using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mediconnet_Backend.Core.Interfaces.Services;
using System.Security.Claims;

namespace Mediconnet_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        INotificationService notificationService,
        ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Récupérer les notifications de l'utilisateur connecté
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] string? type = null,
        [FromQuery] bool? lu = null,
        [FromQuery] string? priorite = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var filter = new NotificationFilter
            {
                Type = type,
                Lu = lu,
                Priorite = priorite,
                Page = page,
                PageSize = Math.Min(pageSize, 50) // Max 50 par page
            };

            var result = await _notificationService.GetUserNotificationsAsync(userId.Value, filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération notifications");
            return StatusCode(500, new { message = "Erreur lors de la récupération des notifications" });
        }
    }

    /// <summary>
    /// Récupérer le nombre de notifications non lues
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var count = await _notificationService.GetUnreadCountAsync(userId.Value);
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur comptage notifications");
            return StatusCode(500, new { message = "Erreur lors du comptage" });
        }
    }

    /// <summary>
    /// Récupérer une notification par ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var notification = await _notificationService.GetByIdAsync(id);
            if (notification == null) return NotFound();

            var userId = GetCurrentUserId();
            if (notification.IdUser != userId) return Forbid();

            return Ok(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération notification {Id}", id);
            return StatusCode(500, new { message = "Erreur lors de la récupération" });
        }
    }

    /// <summary>
    /// Marquer une notification comme lue
    /// </summary>
    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var result = await _notificationService.MarkAsReadAsync(id, userId.Value);
            return result ? Ok(new { message = "Notification marquée comme lue" }) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur marquage notification {Id}", id);
            return StatusCode(500, new { message = "Erreur lors du marquage" });
        }
    }

    /// <summary>
    /// Marquer toutes les notifications comme lues
    /// </summary>
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var count = await _notificationService.MarkAllAsReadAsync(userId.Value);
            return Ok(new { message = $"{count} notifications marquées comme lues", count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur marquage toutes notifications");
            return StatusCode(500, new { message = "Erreur lors du marquage" });
        }
    }

    /// <summary>
    /// Marquer plusieurs notifications comme lues
    /// </summary>
    [HttpPatch("read-multiple")]
    public async Task<IActionResult> MarkMultipleAsRead([FromBody] MarkMultipleRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var count = await _notificationService.MarkMultipleAsReadAsync(userId.Value, request.Ids);
            return Ok(new { message = $"{count} notifications marquées comme lues", count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur marquage multiple notifications");
            return StatusCode(500, new { message = "Erreur lors du marquage" });
        }
    }

    /// <summary>
    /// Supprimer une notification
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var result = await _notificationService.DeleteAsync(id, userId.Value);
            return result ? Ok(new { message = "Notification supprimée" }) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur suppression notification {Id}", id);
            return StatusCode(500, new { message = "Erreur lors de la suppression" });
        }
    }

    /// <summary>
    /// Supprimer toutes les notifications lues
    /// </summary>
    [HttpDelete("read")]
    public async Task<IActionResult> DeleteAllRead()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var count = await _notificationService.DeleteAllReadAsync(userId.Value);
            return Ok(new { message = $"{count} notifications supprimées", count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur suppression notifications lues");
            return StatusCode(500, new { message = "Erreur lors de la suppression" });
        }
    }

    /// <summary>
    /// Créer une notification (Admin uniquement)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "administrateur")]
    public async Task<IActionResult> Create([FromBody] CreateNotificationRequest request)
    {
        try
        {
            var notification = await _notificationService.CreateAsync(request);
            return Ok(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur création notification");
            return StatusCode(500, new { message = "Erreur lors de la création" });
        }
    }

    /// <summary>
    /// Créer une notification pour un rôle (Admin uniquement)
    /// </summary>
    [HttpPost("role/{role}")]
    [Authorize(Roles = "administrateur")]
    public async Task<IActionResult> CreateForRole(string role, [FromBody] CreateNotificationRequest request)
    {
        try
        {
            var count = await _notificationService.CreateForRoleAsync(role, request);
            return Ok(new { message = $"{count} notifications créées", count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur création notifications pour rôle {Role}", role);
            return StatusCode(500, new { message = "Erreur lors de la création" });
        }
    }
}

public class MarkMultipleRequest
{
    public List<int> Ids { get; set; } = new();
}
