namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Interface pour le service d'audit - Traçabilité des actions utilisateurs
/// Conformité RGPD et HDS (Hébergeur de Données de Santé)
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Enregistre une action utilisateur en base de données
    /// </summary>
    Task LogActionAsync(int userId, string action, string resourceType, int? resourceId = null, string? details = null, bool success = true);

    /// <summary>
    /// Enregistre une action utilisateur avec l'adresse IP
    /// </summary>
    Task LogActionWithIpAsync(int userId, string action, string resourceType, string? ipAddress, int? resourceId = null, string? details = null, bool success = true);

    /// <summary>
    /// Enregistre un échec d'authentification
    /// </summary>
    Task LogAuthFailureAsync(string identifier, string? ipAddress, string reason);

    /// <summary>
    /// Enregistre un accès aux données sensibles (données médicales)
    /// </summary>
    Task LogSensitiveAccessAsync(int userId, string resourceType, int resourceId, string? ipAddress);

    /// <summary>
    /// Récupère l'historique d'audit pour un utilisateur
    /// </summary>
    Task<List<AuditLogDto>> GetUserAuditHistoryAsync(int userId, int limit = 100);

    /// <summary>
    /// Récupère l'historique d'audit pour une ressource
    /// </summary>
    Task<List<AuditLogDto>> GetResourceAuditHistoryAsync(string resourceType, int resourceId, int limit = 50);

    /// <summary>
    /// Récupère les logs d'audit avec pagination et filtres (Admin)
    /// </summary>
    Task<AuditLogsPagedResult> GetAuditLogsPagedAsync(
        int page, int pageSize, string? action, string? resourceType, 
        int? userId, DateTime? dateFrom, DateTime? dateTo, bool? successOnly);

    /// <summary>
    /// Récupère les statistiques d'audit
    /// </summary>
    Task<AuditStatsDto> GetAuditStatsAsync(int days = 7);

    /// <summary>
    /// Récupère les actions distinctes pour le filtrage
    /// </summary>
    Task<List<string>> GetDistinctActionsAsync();

    /// <summary>
    /// Récupère les types de ressources distincts pour le filtrage
    /// </summary>
    Task<List<string>> GetDistinctResourceTypesAsync();
}

/// <summary>
/// Résultat paginé des logs d'audit
/// </summary>
public class AuditLogsPagedResult
{
    public List<AuditLogDto> Logs { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// Statistiques d'audit
/// </summary>
public class AuditStatsDto
{
    public int TotalLogs { get; set; }
    public int TotalSuccess { get; set; }
    public int TotalFailures { get; set; }
    public int AuthFailures { get; set; }
    public int SensitiveAccess { get; set; }
    public List<AuditStatByDay> LogsByDay { get; set; } = new();
    public List<AuditStatByAction> TopActions { get; set; } = new();
}

public class AuditStatByDay
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public int Failures { get; set; }
}

public class AuditStatByAction
{
    public string Action { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// DTO pour les logs d'audit
/// </summary>
public class AuditLogDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public int? ResourceId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public bool Success { get; set; }
    public DateTime CreatedAt { get; set; }
}
