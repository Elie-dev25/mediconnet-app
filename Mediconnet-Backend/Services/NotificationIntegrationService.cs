using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service d'intégration des notifications pour les événements métier
/// Centralise la création de notifications pour tous les services
/// </summary>
public class NotificationIntegrationService
{
    private readonly INotificationService _notificationService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationIntegrationService> _logger;

    public NotificationIntegrationService(
        INotificationService notificationService,
        ApplicationDbContext context,
        ILogger<NotificationIntegrationService> logger)
    {
        _notificationService = notificationService;
        _context = context;
        _logger = logger;
    }

    // ==================== RENDEZ-VOUS ====================

    public async Task NotifyRendezVousCreatedAsync(int medecinId, int patientId, string patientNom, DateTime dateRdv)
    {
        // Notification au médecin
        await _notificationService.CreateAsync(new CreateNotificationRequest
        {
            IdUser = medecinId,
            Type = NotificationType.RendezVous,
            Titre = "Nouveau rendez-vous",
            Message = $"Nouveau RDV avec {patientNom} le {dateRdv:dd/MM/yyyy à HH:mm}",
            Lien = "/medecin/agenda",
            Priorite = NotificationPriority.Normale,
            SendRealTime = true
        });

        // Notification au patient
        await _notificationService.CreateAsync(new CreateNotificationRequest
        {
            IdUser = patientId,
            Type = NotificationType.RendezVous,
            Titre = "Rendez-vous créé",
            Message = $"Votre demande de RDV pour le {dateRdv:dd/MM/yyyy à HH:mm} a été envoyée",
            Lien = "/patient/rendez-vous",
            Priorite = NotificationPriority.Normale,
            SendRealTime = true
        });
    }

    public async Task NotifyRendezVousConfirmedAsync(int patientId, string medecinNom, DateTime dateRdv)
    {
        await _notificationService.CreateAsync(new CreateNotificationRequest
        {
            IdUser = patientId,
            Type = NotificationType.Validation,
            Titre = "Rendez-vous confirmé",
            Message = $"Votre RDV avec Dr. {medecinNom} le {dateRdv:dd/MM/yyyy à HH:mm} a été confirmé",
            Lien = "/patient/rendez-vous",
            Priorite = NotificationPriority.Normale,
            SendRealTime = true
        });
    }

    public async Task NotifyRendezVousCancelledAsync(int patientId, string medecinNom, string raison, DateTime dateRdv)
    {
        await _notificationService.CreateAsync(new CreateNotificationRequest
        {
            IdUser = patientId,
            Type = NotificationType.Annulation,
            Titre = "Rendez-vous annulé",
            Message = $"Votre RDV avec Dr. {medecinNom} du {dateRdv:dd/MM/yyyy à HH:mm} a été annulé. Raison: {raison}",
            Lien = "/patient/rendez-vous",
            Priorite = NotificationPriority.Haute,
            SendRealTime = true
        });
    }

    public async Task NotifyRendezVousReminderAsync(int patientId, string medecinNom, DateTime dateRdv)
    {
        await _notificationService.CreateAsync(new CreateNotificationRequest
        {
            IdUser = patientId,
            Type = NotificationType.Rappel,
            Titre = "Rappel de rendez-vous",
            Message = $"N'oubliez pas votre RDV avec Dr. {medecinNom} demain à {dateRdv:HH:mm}",
            Lien = "/patient/rendez-vous",
            Priorite = NotificationPriority.Haute,
            SendRealTime = true
        });
    }

    // ==================== CONSULTATIONS ====================

    public async Task NotifyConsultationCompletedAsync(int patientId, string medecinNom)
    {
        await _notificationService.CreateAsync(new CreateNotificationRequest
        {
            IdUser = patientId,
            Type = NotificationType.Consultation,
            Titre = "Consultation terminée",
            Message = $"Votre consultation avec Dr. {medecinNom} est terminée. Consultez votre dossier médical.",
            Lien = "/patient/dossier-medical",
            Priorite = NotificationPriority.Normale,
            SendRealTime = true
        });
    }

    public async Task NotifyVitalsRecordedAsync(int medecinId, string patientNom, int consultationId)
    {
        await _notificationService.CreateAsync(new CreateNotificationRequest
        {
            IdUser = medecinId,
            Type = NotificationType.Consultation,
            Titre = "Patient prêt",
            Message = $"Les paramètres de {patientNom} sont enregistrés. Patient prêt pour la consultation.",
            Lien = $"/medecin/consultations/{consultationId}",
            Priorite = NotificationPriority.Haute,
            SendRealTime = true
        });
    }

    // ==================== FACTURATION ====================

    public async Task NotifyFactureCreatedAsync(int patientId, string numeroFacture, decimal montant)
    {
        await _notificationService.CreateAsync(new CreateNotificationRequest
        {
            IdUser = patientId,
            Type = NotificationType.Facture,
            Titre = "Nouvelle facture",
            Message = $"Facture {numeroFacture} de {montant:N0} FCFA à régler",
            Lien = "/patient/factures",
            Priorite = NotificationPriority.Normale,
            SendRealTime = true
        });
    }

    public async Task NotifyFacturePaidAsync(int patientId, string numeroFacture)
    {
        await _notificationService.CreateAsync(new CreateNotificationRequest
        {
            IdUser = patientId,
            Type = NotificationType.Validation,
            Titre = "Paiement reçu",
            Message = $"Merci ! Votre paiement pour la facture {numeroFacture} a été enregistré.",
            Lien = "/patient/factures",
            Priorite = NotificationPriority.Normale,
            SendRealTime = true
        });
    }

    public async Task NotifyEcheanceProximateAsync(int patientId, decimal montant, DateTime dateEcheance)
    {
        await _notificationService.CreateAsync(new CreateNotificationRequest
        {
            IdUser = patientId,
            Type = NotificationType.Rappel,
            Titre = "Échéance de paiement",
            Message = $"Rappel: échéance de {montant:N0} FCFA le {dateEcheance:dd/MM/yyyy}",
            Lien = "/patient/factures",
            Priorite = NotificationPriority.Haute,
            SendRealTime = true
        });
    }

    // ==================== STOCK PHARMACIE ====================

    public async Task NotifyStockBas(int medicamentId, string nomMedicament, int stockActuel, int seuilStock)
    {
        var pharmaciens = await _context.Utilisateurs
            .Where(u => u.Role == "pharmacien" || u.Role == "administrateur")
            .Select(u => u.IdUser)
            .ToListAsync();

        foreach (var userId in pharmaciens)
        {
            await _notificationService.CreateAsync(new CreateNotificationRequest
            {
                IdUser = userId,
                Type = NotificationType.Stock,
                Titre = "Stock bas",
                Message = $"⚠️ {nomMedicament}: stock actuel ({stockActuel}) sous le seuil ({seuilStock})",
                Lien = "/pharmacie/stock",
                Priorite = NotificationPriority.Haute,
                SendRealTime = true
            });
        }
    }

    public async Task NotifyRuptureStock(string nomMedicament)
    {
        var admins = await _context.Utilisateurs
            .Where(u => u.Role == "administrateur" || u.Role == "pharmacien")
            .Select(u => u.IdUser)
            .ToListAsync();

        foreach (var userId in admins)
        {
            await _notificationService.CreateAsync(new CreateNotificationRequest
            {
                IdUser = userId,
                Type = NotificationType.Alerte,
                Titre = "Rupture de stock",
                Message = $"🚨 URGENT: {nomMedicament} est en rupture de stock !",
                Lien = "/pharmacie/stock",
                Priorite = NotificationPriority.Urgente,
                SendRealTime = true
            });
        }
    }

    public async Task NotifyMedicamentPerime(string nomMedicament, DateTime datePeremption)
    {
        var pharmaciens = await _context.Utilisateurs
            .Where(u => u.Role == "pharmacien" || u.Role == "administrateur")
            .Select(u => u.IdUser)
            .ToListAsync();

        foreach (var userId in pharmaciens)
        {
            await _notificationService.CreateAsync(new CreateNotificationRequest
            {
                IdUser = userId,
                Type = NotificationType.Alerte,
                Titre = "Médicament périmé",
                Message = $"⛔ {nomMedicament} est périmé depuis le {datePeremption:dd/MM/yyyy}",
                Lien = "/pharmacie/stock",
                Priorite = NotificationPriority.Urgente,
                SendRealTime = true
            });
        }
    }

    // ==================== HOSPITALISATION ====================

    public async Task NotifyAdmissionPatientAsync(int medecinId, int infirmierId, string patientNom, string numeroChambre)
    {
        var message = $"Admission de {patientNom} en chambre {numeroChambre}";

        await _notificationService.CreateAsync(new CreateNotificationRequest
        {
            IdUser = medecinId,
            Type = NotificationType.Alerte,
            Titre = "Nouvelle admission",
            Message = message,
            Lien = "/hospitalisation",
            Priorite = NotificationPriority.Haute,
            SendRealTime = true
        });

        if (infirmierId > 0)
        {
            await _notificationService.CreateAsync(new CreateNotificationRequest
            {
                IdUser = infirmierId,
                Type = NotificationType.Alerte,
                Titre = "Nouvelle admission",
                Message = message,
                Lien = "/infirmier/patients",
                Priorite = NotificationPriority.Haute,
                SendRealTime = true
            });
        }
    }

    public async Task NotifyTransfertPatientAsync(int medecinId, string patientNom, string ancienLit, string nouveauLit)
    {
        await _notificationService.CreateAsync(new CreateNotificationRequest
        {
            IdUser = medecinId,
            Type = NotificationType.Alerte,
            Titre = "Transfert de patient",
            Message = $"{patientNom} transféré de {ancienLit} vers {nouveauLit}",
            Lien = "/hospitalisation",
            Priorite = NotificationPriority.Haute,
            SendRealTime = true
        });
    }

    // ==================== ALERTES MÉDICALES ====================

    public async Task NotifyAlerteMedicaleAsync(int medecinId, string patientNom, string typeAlerte, string message)
    {
        await _notificationService.CreateAsync(new CreateNotificationRequest
        {
            IdUser = medecinId,
            Type = NotificationType.AlerteMedicale,
            Titre = $"Alerte médicale - {typeAlerte}",
            Message = $"Patient {patientNom}: {message}",
            Priorite = NotificationPriority.Urgente,
            SendRealTime = true
        });
    }

    // ==================== PRESCRIPTIONS ====================

    public async Task NotifyOrdonnanceARenouvelerAsync(int medecinId, string patientNom, string codeOrdonnance)
    {
        await _notificationService.CreateAsync(new CreateNotificationRequest
        {
            IdUser = medecinId,
            Type = NotificationType.Rappel,
            Titre = "Ordonnance à renouveler",
            Message = $"L'ordonnance {codeOrdonnance} de {patientNom} arrive à expiration",
            Lien = "/medecin/ordonnances",
            Priorite = NotificationPriority.Normale,
            SendRealTime = true
        });
    }

    // ==================== SYSTÈME ====================

    public async Task NotifyNewUserRegisteredAsync(string nomComplet, string role)
    {
        var admins = await _context.Utilisateurs
            .Where(u => u.Role == "administrateur")
            .Select(u => u.IdUser)
            .ToListAsync();

        foreach (var adminId in admins)
        {
            await _notificationService.CreateAsync(new CreateNotificationRequest
            {
                IdUser = adminId,
                Type = NotificationType.Systeme,
                Titre = "Nouveau compte",
                Message = $"Nouvel utilisateur inscrit: {nomComplet} ({role})",
                Lien = "/admin/users",
                Priorite = NotificationPriority.Basse,
                SendRealTime = true
            });
        }
    }

    public async Task NotifyWelcomeUserAsync(int userId, string prenom)
    {
        await _notificationService.CreateAsync(new CreateNotificationRequest
        {
            IdUser = userId,
            Type = NotificationType.Systeme,
            Titre = $"Bienvenue {prenom} !",
            Message = "Votre compte MediConnect est actif. Complétez votre profil pour commencer.",
            Lien = "/profil",
            Priorite = NotificationPriority.Normale,
            SendRealTime = true
        });
    }
}
