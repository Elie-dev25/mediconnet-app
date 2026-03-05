using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Mediconnet_Backend.Core.Interfaces.Services.INotificationService;

namespace Mediconnet_Backend.Infrastructure.BackgroundJobs.Jobs;

/// <summary>
/// Job pour gérer les assurances patients expirées ou sur le point d'expirer
/// S'exécute quotidiennement pour détecter et notifier les patients concernés
/// </summary>
public class ExpiredInsuranceJob
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ExpiredInsuranceJob> _logger;

    // Nombre de jours avant expiration pour envoyer un avertissement
    private const int WARNING_DAYS_BEFORE_EXPIRY = 30;
    private const int URGENT_WARNING_DAYS = 7;

    public ExpiredInsuranceJob(
        ApplicationDbContext context,
        INotificationService notificationService,
        ILogger<ExpiredInsuranceJob> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Vérifie les assurances expirées et envoie des notifications aux patients
    /// </summary>
    public async Task ProcessExpiredInsurancesAsync()
    {
        var now = DateTimeHelper.Now;
        var today = now.Date;

        _logger.LogInformation($"[ExpiredInsuranceJob] Début du traitement des assurances expirées ({today:yyyy-MM-dd})");

        try
        {
            // 1. Trouver les patients dont l'assurance a expiré aujourd'hui ou récemment (non encore notifiés)
            var expiredInsurances = await _context.Patients
                .Include(p => p.Utilisateur)
                .Include(p => p.Assurance)
                .Where(p => p.AssuranceId.HasValue)
                .Where(p => p.DateFinValidite.HasValue)
                .Where(p => p.DateFinValidite.Value.Date <= today)
                .Where(p => p.DateFinValidite.Value.Date >= today.AddDays(-7)) // Expirées dans les 7 derniers jours
                .ToListAsync();

            _logger.LogInformation($"[ExpiredInsuranceJob] {expiredInsurances.Count} assurance(s) expirée(s) détectée(s)");

            foreach (var patient in expiredInsurances)
            {
                await NotifyExpiredInsuranceAsync(patient);
            }

            // 2. Trouver les patients dont l'assurance va expirer bientôt (avertissement)
            var warningDate = today.AddDays(WARNING_DAYS_BEFORE_EXPIRY);
            var urgentDate = today.AddDays(URGENT_WARNING_DAYS);

            var expiringInsurances = await _context.Patients
                .Include(p => p.Utilisateur)
                .Include(p => p.Assurance)
                .Where(p => p.AssuranceId.HasValue)
                .Where(p => p.DateFinValidite.HasValue)
                .Where(p => p.DateFinValidite.Value.Date > today)
                .Where(p => p.DateFinValidite.Value.Date <= warningDate)
                .ToListAsync();

            _logger.LogInformation($"[ExpiredInsuranceJob] {expiringInsurances.Count} assurance(s) expirant dans les {WARNING_DAYS_BEFORE_EXPIRY} jours");

            foreach (var patient in expiringInsurances)
            {
                var daysUntilExpiry = (patient.DateFinValidite!.Value.Date - today).Days;
                var isUrgent = daysUntilExpiry <= URGENT_WARNING_DAYS;

                // Envoyer notification seulement à certains intervalles (30j, 14j, 7j, 3j, 1j)
                if (daysUntilExpiry == 30 || daysUntilExpiry == 14 || daysUntilExpiry == 7 || 
                    daysUntilExpiry == 3 || daysUntilExpiry == 1)
                {
                    await NotifyExpiringInsuranceAsync(patient, daysUntilExpiry, isUrgent);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("[ExpiredInsuranceJob] Traitement terminé");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ExpiredInsuranceJob] Erreur globale lors du traitement des assurances expirées");
            throw;
        }
    }

    /// <summary>
    /// Génère un rapport des assurances expirées pour les administrateurs
    /// </summary>
    public async Task GenerateExpiredInsuranceReportAsync()
    {
        var today = DateTimeHelper.Now.Date;

        _logger.LogInformation($"[ExpiredInsuranceJob] Génération du rapport des assurances expirées");

        try
        {
            // Patients avec assurance expirée
            var expiredPatients = await _context.Patients
                .Include(p => p.Utilisateur)
                .Include(p => p.Assurance)
                .Where(p => p.AssuranceId.HasValue)
                .Where(p => p.DateFinValidite.HasValue)
                .Where(p => p.DateFinValidite.Value.Date < today)
                .OrderBy(p => p.DateFinValidite)
                .Select(p => new
                {
                    PatientId = p.IdUser,
                    Nom = p.Utilisateur != null ? $"{p.Utilisateur.Prenom} {p.Utilisateur.Nom}" : "Inconnu",
                    Telephone = p.Utilisateur != null ? p.Utilisateur.Telephone : null,
                    Assurance = p.Assurance != null ? p.Assurance.Nom : "Inconnue",
                    DateExpiration = p.DateFinValidite,
                    JoursExpires = (today - p.DateFinValidite!.Value.Date).Days
                })
                .ToListAsync();

            // Patients avec assurance expirant bientôt
            var warningDate = today.AddDays(WARNING_DAYS_BEFORE_EXPIRY);
            var expiringPatients = await _context.Patients
                .Include(p => p.Utilisateur)
                .Include(p => p.Assurance)
                .Where(p => p.AssuranceId.HasValue)
                .Where(p => p.DateFinValidite.HasValue)
                .Where(p => p.DateFinValidite.Value.Date >= today)
                .Where(p => p.DateFinValidite.Value.Date <= warningDate)
                .OrderBy(p => p.DateFinValidite)
                .Select(p => new
                {
                    PatientId = p.IdUser,
                    Nom = p.Utilisateur != null ? $"{p.Utilisateur.Prenom} {p.Utilisateur.Nom}" : "Inconnu",
                    Telephone = p.Utilisateur != null ? p.Utilisateur.Telephone : null,
                    Assurance = p.Assurance != null ? p.Assurance.Nom : "Inconnue",
                    DateExpiration = p.DateFinValidite,
                    JoursRestants = (p.DateFinValidite!.Value.Date - today).Days
                })
                .ToListAsync();

            if (expiredPatients.Any() || expiringPatients.Any())
            {
                _logger.LogInformation($"[ExpiredInsuranceJob] Rapport: {expiredPatients.Count} expirées, {expiringPatients.Count} expirant bientôt");

                // Notifier les administrateurs
                var admins = await _context.Utilisateurs
                    .Where(u => u.Role == "administrateur" || u.Role == "admin")
                    .Select(u => u.IdUser)
                    .ToListAsync();

                foreach (var adminId in admins)
                {
                    await _notificationService.CreateAsync(new CreateNotificationRequest
                    {
                        IdUser = adminId,
                        Type = "rapport_assurances",
                        Titre = "Rapport assurances patients",
                        Message = $"{expiredPatients.Count} assurance(s) expirée(s), {expiringPatients.Count} expirant dans les {WARNING_DAYS_BEFORE_EXPIRY} jours.",
                        Priorite = expiredPatients.Count > 0 ? "haute" : "normale",
                        Lien = "/admin/users?filter=assurance_expiree",
                        Metadata = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            date = today.ToString("yyyy-MM-dd"),
                            expiredCount = expiredPatients.Count,
                            expiringCount = expiringPatients.Count,
                            expired = expiredPatients.Take(10),
                            expiring = expiringPatients.Take(10)
                        })
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ExpiredInsuranceJob] Erreur lors de la génération du rapport");
        }
    }

    /// <summary>
    /// Notifie un patient que son assurance a expiré
    /// </summary>
    private async Task NotifyExpiredInsuranceAsync(Core.Entities.Patient patient)
    {
        try
        {
            var patientNom = patient.Utilisateur != null 
                ? $"{patient.Utilisateur.Prenom} {patient.Utilisateur.Nom}" 
                : "Patient";
            var assuranceNom = patient.Assurance?.Nom ?? "votre assurance";
            var dateExpiration = patient.DateFinValidite?.ToString("dd/MM/yyyy") ?? "inconnue";

            // Notification pour le patient
            await _notificationService.CreateAsync(new CreateNotificationRequest
            {
                IdUser = patient.IdUser,
                Type = "assurance_expiree",
                Titre = "⚠️ Assurance expirée",
                Message = $"Votre assurance {assuranceNom} a expiré le {dateExpiration}. " +
                         "Vos prochaines consultations ne seront pas couvertes. " +
                         "Veuillez mettre à jour vos informations d'assurance.",
                Priorite = "haute",
                Icone = "shield-alert",
                Lien = "/patient/profil",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    patientId = patient.IdUser,
                    assuranceId = patient.AssuranceId,
                    assuranceNom,
                    dateExpiration = patient.DateFinValidite?.ToString("o")
                })
            });

            _logger.LogInformation($"[ExpiredInsuranceJob] Notification envoyée: assurance expirée pour patient #{patient.IdUser} ({patientNom})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ExpiredInsuranceJob] Erreur notification assurance expirée patient #{patient.IdUser}");
        }
    }

    /// <summary>
    /// Notifie un patient que son assurance va bientôt expirer
    /// </summary>
    private async Task NotifyExpiringInsuranceAsync(Core.Entities.Patient patient, int daysUntilExpiry, bool isUrgent)
    {
        try
        {
            var patientNom = patient.Utilisateur != null 
                ? $"{patient.Utilisateur.Prenom} {patient.Utilisateur.Nom}" 
                : "Patient";
            var assuranceNom = patient.Assurance?.Nom ?? "votre assurance";
            var dateExpiration = patient.DateFinValidite?.ToString("dd/MM/yyyy") ?? "inconnue";

            var titre = isUrgent 
                ? $"🚨 Assurance expire dans {daysUntilExpiry} jour(s)" 
                : $"📅 Assurance expire dans {daysUntilExpiry} jours";

            var message = daysUntilExpiry == 1
                ? $"Votre assurance {assuranceNom} expire DEMAIN ({dateExpiration}). Pensez à la renouveler."
                : $"Votre assurance {assuranceNom} expire le {dateExpiration} (dans {daysUntilExpiry} jours). " +
                  "Pensez à la renouveler pour maintenir votre couverture.";

            await _notificationService.CreateAsync(new CreateNotificationRequest
            {
                IdUser = patient.IdUser,
                Type = "assurance_expiration_proche",
                Titre = titre,
                Message = message,
                Priorite = isUrgent ? "haute" : "normale",
                Icone = isUrgent ? "shield-alert" : "shield",
                Lien = "/patient/profil",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    patientId = patient.IdUser,
                    assuranceId = patient.AssuranceId,
                    assuranceNom,
                    dateExpiration = patient.DateFinValidite?.ToString("o"),
                    daysUntilExpiry,
                    isUrgent
                })
            });

            _logger.LogInformation($"[ExpiredInsuranceJob] Notification envoyée: assurance expire dans {daysUntilExpiry}j pour patient #{patient.IdUser}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ExpiredInsuranceJob] Erreur notification expiration proche patient #{patient.IdUser}");
        }
    }
}
