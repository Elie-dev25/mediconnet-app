using Mediconnet_Backend.Core.CQRS.Queries;
using Mediconnet_Backend.Core.CQRS.Queries.Dashboard;
using Mediconnet_Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Infrastructure.CQRS.Queries.Dashboard;

/// <summary>
/// Handler pour les statistiques du tableau de bord
/// Optimisé pour la lecture (CQRS - Query Side)
/// </summary>
public class GetDashboardStatsQueryHandler : IQueryHandler<GetDashboardStatsQuery, DashboardStatsResult>
{
    private readonly ApplicationDbContext _context;

    public GetDashboardStatsQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStatsResult> HandleAsync(
        GetDashboardStatsQuery query, 
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var dateFrom = query.DateFrom ?? today.AddDays(-30);
        var dateTo = query.DateTo ?? today.AddDays(1);

        // Requêtes optimisées en parallèle
        var totalPatientsTask = _context.Patients.CountAsync(cancellationToken);
        var newPatientsTodayTask = _context.Patients
            .CountAsync(p => p.DateCreation.Date == today, cancellationToken);
        
        var totalConsultationsTask = _context.Consultations.CountAsync(cancellationToken);
        var consultationsTodayTask = _context.Consultations
            .CountAsync(c => c.DateHeure.Date == today, cancellationToken);
        
        var totalRendezVousTask = _context.RendezVous.CountAsync(cancellationToken);
        var rendezVousTodayTask = _context.RendezVous
            .CountAsync(r => r.DateHeure.Date == today, cancellationToken);
        
        var totalMedecinsTask = _context.Medecins.CountAsync(cancellationToken);

        // Revenus
        var revenueTotalTask = _context.Factures
            .Where(f => f.Statut == "payee")
            .SumAsync(f => f.MontantTotal, cancellationToken);
        
        var revenueTodayTask = _context.Factures
            .Where(f => f.Statut == "payee" && f.DatePaiement.HasValue && f.DatePaiement.Value.Date == today)
            .SumAsync(f => f.MontantTotal, cancellationToken);

        // Consultations par service
        var consultationsByServiceTask = _context.Consultations
            .Where(c => c.DateHeure >= dateFrom && c.DateHeure <= dateTo)
            .GroupBy(c => c.Medecin!.Service!.NomService)
            .Select(g => new ConsultationsByService
            {
                ServiceName = g.Key ?? "Non assigné",
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync(cancellationToken);

        // Statistiques journalières (7 derniers jours)
        var sevenDaysAgo = today.AddDays(-7);
        var dailyStatsTask = _context.Consultations
            .Where(c => c.DateHeure >= sevenDaysAgo)
            .GroupBy(c => c.DateHeure.Date)
            .Select(g => new DailyStats
            {
                Date = g.Key,
                Consultations = g.Count(),
                RendezVous = 0,
                Revenue = 0
            })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        // Attendre toutes les tâches
        await Task.WhenAll(
            totalPatientsTask, newPatientsTodayTask,
            totalConsultationsTask, consultationsTodayTask,
            totalRendezVousTask, rendezVousTodayTask,
            totalMedecinsTask, revenueTotalTask, revenueTodayTask,
            consultationsByServiceTask, dailyStatsTask
        );

        return new DashboardStatsResult
        {
            TotalPatients = await totalPatientsTask,
            NewPatientsToday = await newPatientsTodayTask,
            TotalConsultations = await totalConsultationsTask,
            ConsultationsToday = await consultationsTodayTask,
            TotalRendezVous = await totalRendezVousTask,
            RendezVousToday = await rendezVousTodayTask,
            TotalMedecins = await totalMedecinsTask,
            RevenueTotal = await revenueTotalTask,
            RevenueToday = await revenueTodayTask,
            ConsultationsByService = await consultationsByServiceTask,
            DailyStats = await dailyStatsTask
        };
    }
}
