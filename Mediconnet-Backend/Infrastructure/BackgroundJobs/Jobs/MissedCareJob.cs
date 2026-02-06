using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Mediconnet_Backend.Core.Interfaces.Services.INotificationService;

namespace Mediconnet_Backend.Infrastructure.BackgroundJobs.Jobs;

/// <summary>
/// Job pour détecter les soins manqués et envoyer des notifications
/// S'exécute toutes les 15 minutes pour détecter les exécutions de soins non effectuées
/// </summary>
public class MissedCareJob
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly ILogger<MissedCareJob> _logger;

    // Délai de grâce après l'heure prévue avant de marquer comme manqué (en minutes)
    private const int GRACE_PERIOD_NORMAL = 60;
    private const int GRACE_PERIOD_HAUTE = 30;
    private const int GRACE_PERIOD_URGENTE = 15;

    public MissedCareJob(
        ApplicationDbContext context,
        INotificationService notificationService,
        IEmailService emailService,
        ILogger<MissedCareJob> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Détecte et marque les exécutions de soins manquées
    /// Une exécution est considérée manquée si:
    /// - La date prévue + heure prévue + délai de grâce est dépassée
    /// - Le statut est "prevu"
    /// </summary>
    public async Task ProcessMissedCareAsync()
    {
        var now = DateTimeHelper.Now;
        
        _logger.LogInformation("[MissedCareJob] Début du traitement des soins manqués");

        try
        {
            // Récupérer les exécutions potentiellement manquées
            var missedExecutions = await _context.ExecutionsSoins
                .Include(e => e.Soin)
                    .ThenInclude(s => s!.Hospitalisation)
                        .ThenInclude(h => h!.Patient)
                            .ThenInclude(p => p!.Utilisateur)
                .Include(e => e.Soin)
                    .ThenInclude(s => s!.Prescripteur)
                        .ThenInclude(m => m!.Utilisateur)
                .Where(e => e.Statut == "prevu")
                .Where(e => e.DatePrevue.HasValue && e.DatePrevue.Value.Date <= now.Date)
                .ToListAsync();

            var markedAsMissed = 0;

            foreach (var execution in missedExecutions)
            {
                // Calculer l'heure limite selon la priorité du soin
                var datePrevue = execution.DatePrevue!.Value.Date;
                var heurePrevue = execution.HeurePrevue ?? new TimeSpan(23, 59, 0);
                var dateHeurePrevue = datePrevue.Add(heurePrevue);
                
                // Délai de grâce selon la priorité
                var priorite = execution.Soin?.Priorite?.ToLower() ?? "normale";
                var gracePeriod = priorite switch
                {
                    "urgente" => GRACE_PERIOD_URGENTE,
                    "haute" => GRACE_PERIOD_HAUTE,
                    _ => GRACE_PERIOD_NORMAL
                };
                
                var dateLimite = dateHeurePrevue.AddMinutes(gracePeriod);

                if (now > dateLimite)
                {
                    // Marquer comme manqué
                    execution.Statut = "manque";
                    execution.UpdatedAt = now;
                    markedAsMissed++;

                    // Envoyer les notifications
                    await SendMissedCareNotificationsAsync(execution);
                }
            }

            if (markedAsMissed > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("[MissedCareJob] {Count} exécutions marquées comme manquées", markedAsMissed);
            }
            else
            {
                _logger.LogInformation("[MissedCareJob] Aucune exécution manquée détectée");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MissedCareJob] Erreur lors du traitement des soins manqués");
        }
    }

    /// <summary>
    /// Envoie les notifications (cloche + email) pour un soin manqué
    /// Destinataires: médecin, patient, infirmiers, major
    /// </summary>
    private async Task SendMissedCareNotificationsAsync(Core.Entities.ExecutionSoin execution)
    {
        try
        {
            var soin = execution.Soin;
            if (soin == null) return;

            var hospitalisation = soin.Hospitalisation;
            if (hospitalisation == null) return;

            var destinataires = new List<int>();
            var emailDestinataires = new List<(int userId, string email, string nom)>();

            // Médecin prescripteur
            if (soin.IdPrescripteur.HasValue)
            {
                destinataires.Add(soin.IdPrescripteur.Value);
                var medecin = soin.Prescripteur?.Utilisateur;
                if (medecin?.Email != null)
                {
                    emailDestinataires.Add((medecin.IdUser, medecin.Email, $"Dr {medecin.Prenom} {medecin.Nom}"));
                }
            }

            // Patient
            var patient = hospitalisation.Patient?.Utilisateur;
            if (patient != null)
            {
                destinataires.Add(patient.IdUser);
                if (patient.Email != null)
                {
                    emailDestinataires.Add((patient.IdUser, patient.Email, $"{patient.Prenom} {patient.Nom}"));
                }
            }

            // Infirmiers (limité à 10)
            var infirmiers = await _context.Utilisateurs
                .Where(u => u.Role == "infirmier")
                .Take(10)
                .ToListAsync();
            foreach (var inf in infirmiers)
            {
                destinataires.Add(inf.IdUser);
                if (inf.Email != null)
                {
                    emailDestinataires.Add((inf.IdUser, inf.Email, $"{inf.Prenom} {inf.Nom}"));
                }
            }

            // Major
            var majors = await _context.Utilisateurs
                .Where(u => u.Role == "major")
                .ToListAsync();
            foreach (var major in majors)
            {
                destinataires.Add(major.IdUser);
                if (major.Email != null)
                {
                    emailDestinataires.Add((major.IdUser, major.Email, $"{major.Prenom} {major.Nom}"));
                }
            }

            var patientNom = patient != null ? $"{patient.Prenom} {patient.Nom}" : "Patient";
            var message = $"⚠️ SOIN MANQUÉ - {soin.TypeSoin}: {soin.Description} pour {patientNom}. " +
                         $"Prévu le {execution.DatePrevue:dd/MM/yyyy} à {execution.HeurePrevue?.ToString(@"hh\:mm") ?? "N/A"}";

            // 1. Notifications cloche
            if (destinataires.Any())
            {
                await _notificationService.CreateBulkAsync(new CreateBulkNotificationRequest
                {
                    UserIds = destinataires.Distinct().ToList(),
                    Type = "soin_manque",
                    Titre = "⚠️ Soin manqué",
                    Message = message,
                    Lien = $"/hospitalisations/{soin.IdHospitalisation}/soins/{soin.IdSoin}",
                    Icone = "alert-triangle",
                    Priorite = "haute",
                    SendRealTime = true
                });
            }

            // 2. Emails
            foreach (var (userId, email, nom) in emailDestinataires.DistinctBy(e => e.email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        email,
                        "⚠️ Alerte: Soin manqué - MediConnect",
                        GetMissedCareEmailBody(soin, execution, patientNom, nom)
                    );
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning("Erreur envoi email soin manqué à {Email}: {Error}", email, emailEx.Message);
                }
            }

            _logger.LogInformation("[MissedCareJob] Notifications envoyées pour exécution {IdExecution}", execution.IdExecution);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[MissedCareJob] Erreur envoi notifications: {Error}", ex.Message);
        }
    }

    private string GetMissedCareEmailBody(
        Core.Entities.SoinHospitalisation soin, 
        Core.Entities.ExecutionSoin execution,
        string patientNom,
        string destinataireNom)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #ef4444; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9fafb; padding: 20px; border: 1px solid #e5e7eb; }}
        .alert {{ background: #fef2f2; border: 1px solid #fecaca; padding: 15px; border-radius: 8px; margin: 15px 0; }}
        .details {{ background: white; padding: 15px; border-radius: 8px; margin: 15px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #6b7280; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>⚠️ Alerte: Soin Manqué</h1>
        </div>
        <div class='content'>
            <p>Bonjour {destinataireNom},</p>
            
            <div class='alert'>
                <strong>Un soin n'a pas été effectué dans les délais prévus.</strong>
            </div>
            
            <div class='details'>
                <p><strong>Patient:</strong> {patientNom}</p>
                <p><strong>Type de soin:</strong> {soin.TypeSoin}</p>
                <p><strong>Description:</strong> {soin.Description}</p>
                <p><strong>Date prévue:</strong> {execution.DatePrevue:dd/MM/yyyy}</p>
                <p><strong>Heure prévue:</strong> {execution.HeurePrevue?.ToString(@"hh\:mm") ?? "Non spécifiée"}</p>
                <p><strong>Séance:</strong> {execution.NumeroSeance}</p>
            </div>
            
            <p>Veuillez prendre les mesures nécessaires pour assurer la continuité des soins.</p>
            
            <p>Cordialement,<br>L'équipe MediConnect</p>
        </div>
        <div class='footer'>
            <p>Cet email a été envoyé automatiquement par le système MediConnect.</p>
        </div>
    </div>
</body>
</html>";
    }
}
