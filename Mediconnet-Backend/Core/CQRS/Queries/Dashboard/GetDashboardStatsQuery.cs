namespace Mediconnet_Backend.Core.CQRS.Queries.Dashboard;

/// <summary>
/// Requête pour récupérer les statistiques du tableau de bord
/// </summary>
public class GetDashboardStatsQuery : IQuery<DashboardStatsResult>
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? ServiceId { get; set; }
}

/// <summary>
/// Résultat des statistiques du tableau de bord
/// </summary>
public class DashboardStatsResult
{
    public int TotalPatients { get; set; }
    public int NewPatientsToday { get; set; }
    public int TotalConsultations { get; set; }
    public int ConsultationsToday { get; set; }
    public int TotalRendezVous { get; set; }
    public int RendezVousToday { get; set; }
    public int TotalMedecins { get; set; }
    public decimal RevenueTotal { get; set; }
    public decimal RevenueToday { get; set; }
    public List<ConsultationsByService> ConsultationsByService { get; set; } = new();
    public List<DailyStats> DailyStats { get; set; } = new();
}

public class ConsultationsByService
{
    public string ServiceName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DailyStats
{
    public DateTime Date { get; set; }
    public int Consultations { get; set; }
    public int RendezVous { get; set; }
    public decimal Revenue { get; set; }
}
