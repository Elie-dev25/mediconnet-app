namespace Mediconnet_Backend.Infrastructure.Caching;

/// <summary>
/// Clés de cache standardisées
/// </summary>
public static class CacheKeys
{
    // Données de référence (longue durée)
    public const string AllServices = "ref:services:all";
    public const string AllSpecialites = "ref:specialites:all";
    public const string AllAssurances = "ref:assurances:all";
    
    // Médecins
    public const string AllMedecins = "medecins:all";
    public static string MedecinById(int id) => $"medecins:{id}";
    public static string MedecinsByService(int serviceId) => $"medecins:service:{serviceId}";
    public static string MedecinsBySpecialite(int specialiteId) => $"medecins:specialite:{specialiteId}";
    
    // Patients
    public static string PatientById(int id) => $"patients:{id}";
    public static string PatientByUserId(int userId) => $"patients:user:{userId}";
    
    // Planning
    public static string MedecinPlanning(int medecinId, DateTime date) => $"planning:{medecinId}:{date:yyyy-MM-dd}";
    public static string CreneauxDisponibles(int medecinId, DateTime date) => $"creneaux:{medecinId}:{date:yyyy-MM-dd}";
    
    // Dashboard
    public const string DashboardStats = "dashboard:stats";
    public static string DashboardStatsByService(int serviceId) => $"dashboard:stats:service:{serviceId}";
    
    // Durées d'expiration
    public static class Expiration
    {
        public static readonly TimeSpan Reference = TimeSpan.FromHours(24);
        public static readonly TimeSpan Medecins = TimeSpan.FromHours(1);
        public static readonly TimeSpan Planning = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan Dashboard = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan Short = TimeSpan.FromMinutes(2);
    }
}
