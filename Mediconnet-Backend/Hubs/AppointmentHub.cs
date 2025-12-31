using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Mediconnet_Backend.Hubs;

/// <summary>
/// Hub SignalR pour la synchronisation en temps réel des rendez-vous
/// Permet aux médecins et patients de recevoir les mises à jour instantanément
/// </summary>
[Authorize]
public class AppointmentHub : Hub
{
    private readonly ILogger<AppointmentHub> _logger;

    public AppointmentHub(ILogger<AppointmentHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Connexion d'un utilisateur au hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            // Ajouter l'utilisateur à son groupe personnel
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            // Si c'est un médecin, l'ajouter au groupe des médecins
            if (role == "medecin")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"medecin_{userId}");
                await Groups.AddToGroupAsync(Context.ConnectionId, "medecins");
            }

            // Si c'est un patient, l'ajouter au groupe des patients
            if (role == "patient")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"patient_{userId}");
            }

            if (role == "caissier")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"caissier_{userId}");
                await Groups.AddToGroupAsync(Context.ConnectionId, "caissiers");
            }

            if (role == "accueil")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"accueil_{userId}");
                await Groups.AddToGroupAsync(Context.ConnectionId, "accueils");
            }

            if (role == "infirmier")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"infirmier_{userId}");
                await Groups.AddToGroupAsync(Context.ConnectionId, "infirmiers");
            }

            _logger.LogInformation($"User {userId} ({role}) connected to AppointmentHub");
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Déconnexion d'un utilisateur
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            _logger.LogInformation($"User {userId} disconnected from AppointmentHub");
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// S'abonner aux mises à jour d'un médecin spécifique
    /// </summary>
    public async Task SubscribeToMedecin(int medecinId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"medecin_{medecinId}_updates");
        _logger.LogInformation($"Connection {Context.ConnectionId} subscribed to medecin_{medecinId}_updates");
    }

    /// <summary>
    /// Se désabonner des mises à jour d'un médecin
    /// </summary>
    public async Task UnsubscribeFromMedecin(int medecinId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"medecin_{medecinId}_updates");
    }

    /// <summary>
    /// Demander un rafraîchissement des créneaux
    /// </summary>
    public async Task RequestSlotRefresh(int medecinId)
    {
        await Clients.Caller.SendAsync("SlotsRefreshRequested", medecinId);
    }
}

/// <summary>
/// Interface pour envoyer des notifications depuis les services
/// </summary>
public interface IAppointmentNotificationService
{
    /// <summary>Notifier qu'un rendez-vous a été créé</summary>
    Task NotifyAppointmentCreatedAsync(int medecinId, int patientId, object appointment);
    
    /// <summary>Notifier qu'un rendez-vous a été modifié</summary>
    Task NotifyAppointmentUpdatedAsync(int medecinId, int patientId, object appointment);
    
    /// <summary>Notifier qu'un rendez-vous a été annulé</summary>
    Task NotifyAppointmentCancelledAsync(int medecinId, int patientId, int appointmentId);
    
    /// <summary>Notifier qu'un créneau a été verrouillé</summary>
    Task NotifySlotLockedAsync(int medecinId, DateTime dateHeure);
    
    /// <summary>Notifier qu'un créneau a été libéré</summary>
    Task NotifySlotUnlockedAsync(int medecinId, DateTime dateHeure);
    
    /// <summary>Demander le rafraîchissement des créneaux d'un médecin</summary>
    Task RequestSlotsRefreshAsync(int medecinId);

    /// <summary>Notifier qu'une facture a été créée (ex: consultation enregistrée)</summary>
    Task NotifyFactureCreatedAsync(object facture);

    /// <summary>Notifier qu'une facture a été payée</summary>
    Task NotifyFacturePaidAsync(object facture);

    /// <summary>Notifier que les paramètres infirmiers ont été enregistrés (patient prêt pour consultation)</summary>
    Task NotifyVitalsRecordedAsync(int medecinId, int consultationId, int? rendezVousId);

    /// <summary>Demander un rafraîchissement de la file d'attente infirmier</summary>
    Task NotifyNurseQueueRefreshAsync();
}

/// <summary>
/// Service pour envoyer des notifications via SignalR
/// </summary>
public class AppointmentNotificationService : IAppointmentNotificationService
{
    private readonly IHubContext<AppointmentHub> _hubContext;
    private readonly ILogger<AppointmentNotificationService> _logger;

    public AppointmentNotificationService(
        IHubContext<AppointmentHub> hubContext,
        ILogger<AppointmentNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyAppointmentCreatedAsync(int medecinId, int patientId, object appointment)
    {
        // Notifier le médecin
        await _hubContext.Clients.Group($"medecin_{medecinId}")
            .SendAsync("AppointmentCreated", appointment);

        // Notifier le patient
        await _hubContext.Clients.Group($"patient_{patientId}")
            .SendAsync("AppointmentCreated", appointment);

        // Notifier tous les abonnés aux mises à jour du médecin
        await _hubContext.Clients.Group($"medecin_{medecinId}_updates")
            .SendAsync("SlotsUpdated", new { medecinId, action = "created" });

        _logger.LogInformation($"Notification: RDV créé pour médecin {medecinId}, patient {patientId}");
    }

    public async Task NotifyAppointmentUpdatedAsync(int medecinId, int patientId, object appointment)
    {
        await _hubContext.Clients.Group($"medecin_{medecinId}")
            .SendAsync("AppointmentUpdated", appointment);

        await _hubContext.Clients.Group($"patient_{patientId}")
            .SendAsync("AppointmentUpdated", appointment);

        await _hubContext.Clients.Group($"medecin_{medecinId}_updates")
            .SendAsync("SlotsUpdated", new { medecinId, action = "updated" });

        _logger.LogInformation($"Notification: RDV modifié pour médecin {medecinId}, patient {patientId}");
    }

    public async Task NotifyAppointmentCancelledAsync(int medecinId, int patientId, int appointmentId)
    {
        var data = new { appointmentId, medecinId, patientId };

        await _hubContext.Clients.Group($"medecin_{medecinId}")
            .SendAsync("AppointmentCancelled", data);

        await _hubContext.Clients.Group($"patient_{patientId}")
            .SendAsync("AppointmentCancelled", data);

        await _hubContext.Clients.Group($"medecin_{medecinId}_updates")
            .SendAsync("SlotsUpdated", new { medecinId, action = "cancelled" });

        _logger.LogInformation($"Notification: RDV {appointmentId} annulé");
    }

    public async Task NotifySlotLockedAsync(int medecinId, DateTime dateHeure)
    {
        await _hubContext.Clients.Group($"medecin_{medecinId}_updates")
            .SendAsync("SlotLocked", new { medecinId, dateHeure });
    }

    public async Task NotifySlotUnlockedAsync(int medecinId, DateTime dateHeure)
    {
        await _hubContext.Clients.Group($"medecin_{medecinId}_updates")
            .SendAsync("SlotUnlocked", new { medecinId, dateHeure });
    }

    public async Task RequestSlotsRefreshAsync(int medecinId)
    {
        await _hubContext.Clients.Group($"medecin_{medecinId}_updates")
            .SendAsync("SlotsRefreshRequested", medecinId);
    }

    public async Task NotifyFactureCreatedAsync(object facture)
    {
        await _hubContext.Clients.Group("caissiers")
            .SendAsync("FactureCreated", facture);
    }

    public async Task NotifyFacturePaidAsync(object facture)
    {
        await _hubContext.Clients.Group("caissiers")
            .SendAsync("FacturePaid", facture);
    }

    public async Task NotifyVitalsRecordedAsync(int medecinId, int consultationId, int? rendezVousId)
    {
        var payload = new
        {
            idConsultation = consultationId,
            idRendezVous = rendezVousId,
            idMedecin = medecinId,
            statut = "pret_consultation"
        };

        await _hubContext.Clients.Group($"medecin_{medecinId}")
            .SendAsync("VitalsRecorded", payload);

        await _hubContext.Clients.Group($"medecin_{medecinId}_updates")
            .SendAsync("SlotsUpdated", new { medecinId, action = "vitals" });

        await _hubContext.Clients.Group("infirmiers")
            .SendAsync("VitalsRecorded", payload);

        _logger.LogInformation($"Notification: VitalsRecorded consultation={consultationId}, rdv={rendezVousId}, medecin={medecinId}");
    }

    public async Task NotifyNurseQueueRefreshAsync()
    {
        await _hubContext.Clients.Group("infirmiers")
            .SendAsync("NurseQueueRefresh", new { action = "refresh" });
    }
}
