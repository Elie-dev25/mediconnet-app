using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mediconnet_Backend.Infrastructure.BackgroundJobs.Jobs;

/// <summary>
/// Job pour envoyer les rappels de rendez-vous
/// </summary>
public class ReminderJob
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ReminderJob> _logger;

    public ReminderJob(
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger<ReminderJob> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Envoie les rappels pour les rendez-vous du lendemain
    /// </summary>
    public async Task SendTomorrowRemindersAsync()
    {
        var tomorrow = DateTime.UtcNow.Date.AddDays(1);
        var dayAfter = tomorrow.AddDays(1);

        var appointments = await _context.RendezVous
            .Include(r => r.Patient)
                .ThenInclude(p => p!.Utilisateur)
            .Include(r => r.Medecin)
                .ThenInclude(m => m!.Utilisateur)
            .Where(r => r.DateHeure >= tomorrow && r.DateHeure < dayAfter)
            .Where(r => r.Statut == "confirmé" || r.Statut == "en_attente")
            .ToListAsync();

        _logger.LogInformation($"Envoi de {appointments.Count} rappels de RDV pour demain");

        foreach (var rdv in appointments)
        {
            try
            {
                var patientEmail = rdv.Patient?.Utilisateur?.Email;
                if (string.IsNullOrEmpty(patientEmail)) continue;

                var medecinNom = $"Dr. {rdv.Medecin?.Utilisateur?.Nom}";
                var dateHeure = rdv.DateHeure.ToString("dd/MM/yyyy à HH:mm");

                await _emailService.SendEmailAsync(
                    patientEmail,
                    "Rappel de rendez-vous - MediConnect",
                    $@"
                    <h2>Rappel de votre rendez-vous</h2>
                    <p>Bonjour {rdv.Patient?.Utilisateur?.Prenom},</p>
                    <p>Nous vous rappelons votre rendez-vous prévu <strong>demain {dateHeure}</strong> avec <strong>{medecinNom}</strong>.</p>
                    <p>Motif: {rdv.Motif ?? "Consultation"}</p>
                    <p>Merci de vous présenter 10 minutes avant l'heure de votre rendez-vous.</p>
                    <p>Cordialement,<br>L'équipe MediConnect</p>
                    "
                );

                _logger.LogDebug($"Rappel envoyé pour RDV #{rdv.IdRendezVous}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur envoi rappel RDV #{rdv.IdRendezVous}");
            }
        }
    }

    /// <summary>
    /// Envoie les rappels 2h avant le rendez-vous
    /// </summary>
    public async Task SendUpcomingRemindersAsync()
    {
        var now = DateTime.UtcNow;
        var twoHoursFromNow = now.AddHours(2);
        var twoHoursAndHalf = now.AddHours(2.5);

        var appointments = await _context.RendezVous
            .Include(r => r.Patient)
                .ThenInclude(p => p!.Utilisateur)
            .Include(r => r.Medecin)
                .ThenInclude(m => m!.Utilisateur)
            .Where(r => r.DateHeure >= twoHoursFromNow && r.DateHeure < twoHoursAndHalf)
            .Where(r => r.Statut == "confirmé")
            .ToListAsync();

        _logger.LogInformation($"Envoi de {appointments.Count} rappels imminents");

        foreach (var rdv in appointments)
        {
            try
            {
                var patientEmail = rdv.Patient?.Utilisateur?.Email;
                if (string.IsNullOrEmpty(patientEmail)) continue;

                var medecinNom = $"Dr. {rdv.Medecin?.Utilisateur?.Nom}";
                var heureRdv = rdv.DateHeure.ToString("HH:mm");

                await _emailService.SendEmailAsync(
                    patientEmail,
                    "Votre rendez-vous dans 2 heures - MediConnect",
                    $@"
                    <h2>Rappel urgent</h2>
                    <p>Bonjour {rdv.Patient?.Utilisateur?.Prenom},</p>
                    <p>Votre rendez-vous avec <strong>{medecinNom}</strong> est prévu dans <strong>2 heures</strong> à <strong>{heureRdv}</strong>.</p>
                    <p>Cordialement,<br>L'équipe MediConnect</p>
                    "
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur envoi rappel imminent RDV #{rdv.IdRendezVous}");
            }
        }
    }
}
