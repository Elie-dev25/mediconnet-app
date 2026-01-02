using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Mediconnet_Backend.Hubs;

/// <summary>
/// Hub SignalR pour les notifications temps réel
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        var userRole = GetUserRole();

        if (userId.HasValue)
        {
            // Rejoindre le groupe personnel de l'utilisateur
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            // Rejoindre le groupe du rôle
            if (!string.IsNullOrEmpty(userRole))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"role_{userRole}");
            }

            _logger.LogInformation("User {UserId} ({Role}) connecté aux notifications", userId, userRole);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        var userRole = GetUserRole();

        if (userId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");

            if (!string.IsNullOrEmpty(userRole))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"role_{userRole}");
            }

            _logger.LogInformation("User {UserId} déconnecté des notifications", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Rejoindre un groupe spécifique (ex: service, département)
    /// </summary>
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Connexion {ConnectionId} a rejoint le groupe {Group}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Quitter un groupe spécifique
    /// </summary>
    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Connexion {ConnectionId} a quitté le groupe {Group}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Marquer une notification comme lue (appelé depuis le client)
    /// </summary>
    public async Task MarkAsRead(int notificationId)
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            // Cette méthode est un raccourci, le vrai traitement se fait via l'API
            await Clients.Caller.SendAsync("NotificationMarkedAsRead", notificationId);
        }
    }

    private int? GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string? GetUserRole()
    {
        return Context.User?.FindFirst(ClaimTypes.Role)?.Value;
    }
}
