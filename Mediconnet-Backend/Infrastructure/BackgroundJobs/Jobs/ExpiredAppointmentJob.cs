using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Mediconnet_Backend.Core.Interfaces.Services.INotificationService;

namespace Mediconnet_Backend.Infrastructure.BackgroundJobs.Jobs;

/// <summary>
/// Job pour gérer les rendez-vous expirés et les absences
/// S'exécute toutes les 15 minutes pour détecter les RDV non honorés
/// </summary>
public class ExpiredAppointmentJob
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ExpiredAppointmentJob> _logger;

    // Délai de grâce après l'heure du RDV avant de marquer comme absent (en minutes)
    private const int GRACE_PERIOD_MINUTES = 30;

    public ExpiredAppointmentJob(
        ApplicationDbContext context,
        INotificationService notificationService,
        ILogger<ExpiredAppointmentJob> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Détecte et marque les rendez-vous expirés comme "absent"
    /// Un RDV est considéré expiré si:
    /// - La date/heure + durée + délai de grâce est dépassée
    /// - Le statut est "planifie", "confirme" ou "en_attente"
    /// - Aucune consultation associée n'a été démarrée
    /// </summary>
    public async Task ProcessExpiredAppointmentsAsync()
    {
        var now = DateTimeHelper.Now;
        var cutoffTime = now.AddMinutes(-GRACE_PERIOD_MINUTES);

        _logger.LogInformation($"[ExpiredAppointmentJob] Début du traitement des RDV expirés (cutoff: {cutoffTime:yyyy-MM-dd HH:mm})");

        try
        {
            // Récupérer les RDV potentiellement expirés
            var expiredAppointments = await _context.RendezVous
                .Include(r => r.Patient).ThenInclude(p => p!.Utilisateur)
                .Include(r => r.Medecin).ThenInclude(m => m!.Utilisateur)
                .Include(r => r.Service)
                .Where(r => r.DateHeure.AddMinutes(r.Duree) < cutoffTime)
                .Where(r => r.Statut == "planifie" || r.Statut == "confirme" || r.Statut == "en_attente")
                .ToListAsync();

            if (!expiredAppointments.Any())
            {
                _logger.LogDebug("[ExpiredAppointmentJob] Aucun RDV expiré trouvé");
                return;
            }

            _logger.LogInformation($"[ExpiredAppointmentJob] {expiredAppointments.Count} RDV expirés détectés");

            var processedCount = 0;
            var errorCount = 0;

            foreach (var rdv in expiredAppointments)
            {
                try
                {
                    // Vérifier si une consultation a été démarrée pour ce RDV
                    var consultationDemarree = await _context.Consultations
                        .AnyAsync(c => c.IdRendezVous == rdv.IdRendezVous && 
                                      (c.Statut == "en_cours" || c.Statut == "terminee"));

                    if (consultationDemarree)
                    {
                        // La consultation a été faite, mettre à jour le RDV comme terminé
                        rdv.Statut = "termine";
                        rdv.DateModification = now;
                        _logger.LogDebug($"[ExpiredAppointmentJob] RDV #{rdv.IdRendezVous} marqué comme terminé (consultation existante)");
                    }
                    else
                    {
                        // Aucune consultation, marquer comme absent
                        rdv.Statut = "absent";
                        rdv.DateModification = now;
                        rdv.Notes = string.IsNullOrEmpty(rdv.Notes) 
                            ? $"Patient absent - Marqué automatiquement le {now:dd/MM/yyyy à HH:mm}"
                            : $"{rdv.Notes}\n[Auto] Patient absent - {now:dd/MM/yyyy HH:mm}";

                        // Mettre à jour la consultation associée si elle existe
                        var consultation = await _context.Consultations
                            .FirstOrDefaultAsync(c => c.IdRendezVous == rdv.IdRendezVous);
                        
                        if (consultation != null && consultation.Statut == "planifie")
                        {
                            consultation.Statut = "annule";
                            consultation.UpdatedAt = now;
                        }

                        // Créer une notification pour le personnel
                        await CreateAbsenceNotificationAsync(rdv);

                        _logger.LogInformation($"[ExpiredAppointmentJob] RDV #{rdv.IdRendezVous} marqué comme absent - Patient: {rdv.Patient?.Utilisateur?.Nom} {rdv.Patient?.Utilisateur?.Prenom}");
                    }

                    processedCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex, $"[ExpiredAppointmentJob] Erreur traitement RDV #{rdv.IdRendezVous}");
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"[ExpiredAppointmentJob] Traitement terminé: {processedCount} RDV traités, {errorCount} erreurs");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ExpiredAppointmentJob] Erreur globale lors du traitement des RDV expirés");
            throw;
        }
    }

    /// <summary>
    /// Libère les créneaux des RDV marqués comme absents pour permettre la reprogrammation
    /// </summary>
    public async Task ReleaseAbsentSlotsAsync()
    {
        var now = DateTimeHelper.Now;
        var today = now.Date;

        _logger.LogInformation("[ExpiredAppointmentJob] Vérification des créneaux à libérer");

        try
        {
            // Trouver les RDV absents d'aujourd'hui qui pourraient être reprogrammés
            var absentToday = await _context.RendezVous
                .Where(r => r.DateHeure.Date == today)
                .Where(r => r.Statut == "absent")
                .Where(r => r.DateHeure > now) // Créneaux futurs seulement
                .CountAsync();

            if (absentToday > 0)
            {
                _logger.LogInformation($"[ExpiredAppointmentJob] {absentToday} créneaux libérés suite à des absences aujourd'hui");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ExpiredAppointmentJob] Erreur lors de la libération des créneaux");
        }
    }

    /// <summary>
    /// Génère un rapport quotidien des absences
    /// </summary>
    public async Task GenerateDailyAbsenceReportAsync()
    {
        var yesterday = DateTimeHelper.Now.Date.AddDays(-1);

        _logger.LogInformation($"[ExpiredAppointmentJob] Génération du rapport d'absences pour {yesterday:yyyy-MM-dd}");

        try
        {
            var absences = await _context.RendezVous
                .Include(r => r.Patient).ThenInclude(p => p!.Utilisateur)
                .Include(r => r.Medecin).ThenInclude(m => m!.Utilisateur)
                .Include(r => r.Service)
                .Where(r => r.DateHeure.Date == yesterday)
                .Where(r => r.Statut == "absent")
                .ToListAsync();

            if (!absences.Any())
            {
                _logger.LogInformation($"[ExpiredAppointmentJob] Aucune absence enregistrée pour {yesterday:yyyy-MM-dd}");
                return;
            }

            // Grouper par service
            var absencesByService = absences
                .GroupBy(r => r.Service?.NomService ?? "Non assigné")
                .Select(g => new
                {
                    Service = g.Key,
                    Count = g.Count(),
                    Patients = g.Select(r => new
                    {
                        Nom = $"{r.Patient?.Utilisateur?.Prenom} {r.Patient?.Utilisateur?.Nom}",
                        Medecin = $"Dr. {r.Medecin?.Utilisateur?.Nom}",
                        Heure = r.DateHeure.ToString("HH:mm")
                    }).ToList()
                })
                .ToList();

            _logger.LogInformation($"[ExpiredAppointmentJob] Rapport d'absences {yesterday:yyyy-MM-dd}: {absences.Count} absences totales");

            foreach (var service in absencesByService)
            {
                _logger.LogInformation($"  - {service.Service}: {service.Count} absence(s)");
            }

            // Notifier les administrateurs
            await _notificationService.CreateAsync(new CreateNotificationRequest
            {
                IdUser = 0, // Notification système
                Type = "rapport_absences",
                Titre = $"Rapport d'absences du {yesterday:dd/MM/yyyy}",
                Message = $"{absences.Count} patient(s) absent(s) hier. Consultez le rapport détaillé.",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    date = yesterday.ToString("yyyy-MM-dd"),
                    totalAbsences = absences.Count,
                    byService = absencesByService
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ExpiredAppointmentJob] Erreur lors de la génération du rapport d'absences");
        }
    }

    /// <summary>
    /// Identifie les patients avec des absences répétées (no-show récidivistes)
    /// </summary>
    public async Task IdentifyRepeatNoShowsAsync()
    {
        var threeMonthsAgo = DateTimeHelper.Now.AddMonths(-3);

        _logger.LogInformation("[ExpiredAppointmentJob] Identification des patients avec absences répétées");

        try
        {
            var repeatNoShows = await _context.RendezVous
                .Include(r => r.Patient).ThenInclude(p => p!.Utilisateur)
                .Where(r => r.DateHeure >= threeMonthsAgo)
                .Where(r => r.Statut == "absent")
                .GroupBy(r => r.IdPatient)
                .Select(g => new
                {
                    PatientId = g.Key,
                    AbsenceCount = g.Count(),
                    LastAbsence = g.Max(r => r.DateHeure)
                })
                .Where(x => x.AbsenceCount >= 3) // 3+ absences en 3 mois
                .ToListAsync();

            if (!repeatNoShows.Any())
            {
                _logger.LogDebug("[ExpiredAppointmentJob] Aucun patient avec absences répétées");
                return;
            }

            _logger.LogWarning($"[ExpiredAppointmentJob] {repeatNoShows.Count} patient(s) avec 3+ absences en 3 mois");

            foreach (var noShow in repeatNoShows)
            {
                var patient = await _context.Patients
                    .Include(p => p.Utilisateur)
                    .FirstOrDefaultAsync(p => p.IdUser == noShow.PatientId);

                if (patient != null)
                {
                    _logger.LogWarning($"  - Patient #{noShow.PatientId} ({patient.Utilisateur?.Prenom} {patient.Utilisateur?.Nom}): {noShow.AbsenceCount} absences");

                    // Créer une alerte pour le personnel
                    await _notificationService.CreateAsync(new CreateNotificationRequest
                    {
                        IdUser = 0,
                        Type = "alerte_absences_repetees",
                        Titre = "Patient avec absences répétées",
                        Message = $"{patient.Utilisateur?.Prenom} {patient.Utilisateur?.Nom} a {noShow.AbsenceCount} absences sur les 3 derniers mois.",
                        Metadata = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            patientId = noShow.PatientId,
                            patientNom = $"{patient.Utilisateur?.Prenom} {patient.Utilisateur?.Nom}",
                            absenceCount = noShow.AbsenceCount,
                            lastAbsence = noShow.LastAbsence.ToString("yyyy-MM-dd")
                        })
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ExpiredAppointmentJob] Erreur lors de l'identification des no-shows répétés");
        }
    }

    /// <summary>
    /// Crée une notification d'absence pour le médecin et l'accueil
    /// </summary>
    private async Task CreateAbsenceNotificationAsync(Core.Entities.RendezVous rdv)
    {
        try
        {
            var patientNom = $"{rdv.Patient?.Utilisateur?.Prenom} {rdv.Patient?.Utilisateur?.Nom}";
            var medecinNom = $"Dr. {rdv.Medecin?.Utilisateur?.Nom}";
            var heureRdv = rdv.DateHeure.ToString("HH:mm");
            var dateRdv = rdv.DateHeure.ToString("dd/MM/yyyy");

            // Notification pour le médecin
            if (rdv.IdMedecin > 0)
            {
                await _notificationService.CreateAsync(new CreateNotificationRequest
                {
                    IdUser = rdv.IdMedecin,
                    Type = "patient_absent",
                    Titre = "Patient absent",
                    Message = $"{patientNom} ne s'est pas présenté(e) au RDV de {heureRdv} le {dateRdv}.",
                    Metadata = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        rdvId = rdv.IdRendezVous,
                        patientId = rdv.IdPatient,
                        patientNom,
                        dateHeure = rdv.DateHeure.ToString("o"),
                        motif = rdv.Motif
                    })
                });
            }

            // Notification système pour l'accueil (userId = 0 pour broadcast)
            await _notificationService.CreateAsync(new CreateNotificationRequest
            {
                IdUser = 0,
                Type = "patient_absent",
                Titre = "Absence patient",
                Message = $"{patientNom} absent au RDV avec {medecinNom} ({heureRdv})",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    rdvId = rdv.IdRendezVous,
                    patientId = rdv.IdPatient,
                    medecinId = rdv.IdMedecin,
                    serviceId = rdv.IdService,
                    dateHeure = rdv.DateHeure.ToString("o")
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ExpiredAppointmentJob] Erreur création notification absence RDV #{rdv.IdRendezVous}");
        }
    }
}
