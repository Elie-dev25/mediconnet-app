using Mediconnet_Backend.Infrastructure.BackgroundJobs.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mediconnet_Backend.Infrastructure.BackgroundJobs;

/// <summary>
/// Service hébergé pour exécuter les jobs en arrière-plan de manière périodique
/// </summary>
public class BackgroundJobHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundJobHostedService> _logger;

    // Intervalles d'exécution
    private static readonly TimeSpan ExpiredAppointmentInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan ReminderInterval = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan DailyReportTime = TimeSpan.FromHours(24);
    private static readonly TimeSpan MissedCareInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ExpiredInsuranceInterval = TimeSpan.FromHours(6); // Vérification toutes les 6h

    public BackgroundJobHostedService(
        IServiceProvider serviceProvider,
        ILogger<BackgroundJobHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[BackgroundJobHostedService] Démarrage du service de jobs en arrière-plan");

        // Démarrer les différentes tâches en parallèle
        var tasks = new List<Task>
        {
            RunExpiredAppointmentJobAsync(stoppingToken),
            RunCleanupJobAsync(stoppingToken),
            RunReminderJobAsync(stoppingToken),
            RunDailyReportsAsync(stoppingToken),
            RunMissedCareJobAsync(stoppingToken),
            RunExpiredInsuranceJobAsync(stoppingToken)
        };

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Exécute le job de détection des RDV expirés toutes les 15 minutes
    /// </summary>
    private async Task RunExpiredAppointmentJobAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[BackgroundJobHostedService] Job ExpiredAppointment configuré (intervalle: 15 min)");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService<ExpiredAppointmentJob>();

                await job.ProcessExpiredAppointmentsAsync();
                await job.ReleaseAbsentSlotsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BackgroundJobHostedService] Erreur dans ExpiredAppointmentJob");
            }

            await Task.Delay(ExpiredAppointmentInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Exécute le job de nettoyage toutes les heures
    /// </summary>
    private async Task RunCleanupJobAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[BackgroundJobHostedService] Job Cleanup configuré (intervalle: 1h)");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService<CleanupJob>();

                await job.CleanExpiredSlotLocksAsync();
                await job.CleanExpiredEmailTokensAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BackgroundJobHostedService] Erreur dans CleanupJob");
            }

            await Task.Delay(CleanupInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Exécute le job de rappels toutes les 30 minutes
    /// </summary>
    private async Task RunReminderJobAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[BackgroundJobHostedService] Job Reminder configuré (intervalle: 30 min)");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService<ReminderJob>();

                await job.SendTomorrowRemindersAsync();
                await job.SendUpcomingRemindersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BackgroundJobHostedService] Erreur dans ReminderJob");
            }

            await Task.Delay(ReminderInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Exécute les rapports quotidiens à minuit
    /// </summary>
    private async Task RunDailyReportsAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[BackgroundJobHostedService] Job DailyReports configuré (exécution quotidienne)");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Calculer le temps jusqu'à la prochaine exécution (6h du matin)
                var now = DateTime.UtcNow;
                var nextRun = now.Date.AddDays(1).AddHours(6); // 6h UTC = 7h heure locale
                
                if (now.Hour < 6)
                {
                    nextRun = now.Date.AddHours(6);
                }

                var delay = nextRun - now;
                _logger.LogDebug($"[BackgroundJobHostedService] Prochain rapport quotidien dans {delay.TotalHours:F1} heures");

                await Task.Delay(delay, stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService<ExpiredAppointmentJob>();

                await job.GenerateDailyAbsenceReportAsync();
                await job.IdentifyRepeatNoShowsAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BackgroundJobHostedService] Erreur dans DailyReports");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    /// <summary>
    /// Exécute le job de détection des soins manqués toutes les 15 minutes
    /// </summary>
    private async Task RunMissedCareJobAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[BackgroundJobHostedService] Job MissedCare configuré (intervalle: 15 min)");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService<MissedCareJob>();

                await job.ProcessMissedCareAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BackgroundJobHostedService] Erreur dans MissedCareJob");
            }

            await Task.Delay(MissedCareInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Exécute le job de vérification des assurances expirées toutes les 6 heures
    /// </summary>
    private async Task RunExpiredInsuranceJobAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[BackgroundJobHostedService] Job ExpiredInsurance configuré (intervalle: 6h)");

        // Attendre 5 minutes au démarrage pour laisser le temps aux autres services de s'initialiser
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService<ExpiredInsuranceJob>();

                await job.ProcessExpiredInsurancesAsync();
                await job.GenerateExpiredInsuranceReportAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BackgroundJobHostedService] Erreur dans ExpiredInsuranceJob");
            }

            await Task.Delay(ExpiredInsuranceInterval, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[BackgroundJobHostedService] Arrêt du service de jobs en arrière-plan");
        await base.StopAsync(cancellationToken);
    }
}
