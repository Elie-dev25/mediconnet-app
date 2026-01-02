using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service d'audit - Persistance en base de données pour conformité RGPD/HDS
/// </summary>
public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task LogActionAsync(int userId, string action, string resourceType, int? resourceId = null, string? details = null, bool success = true)
    {
        await LogActionWithIpAsync(userId, action, resourceType, null, resourceId, details, success);
    }

    /// <inheritdoc />
    public async Task LogActionWithIpAsync(int userId, string action, string resourceType, string? ipAddress, int? resourceId = null, string? details = null, bool success = true)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                ResourceType = resourceType,
                ResourceId = resourceId,
                Details = details,
                IpAddress = ipAddress,
                Success = success,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "[AUDIT] User: {UserId}, Action: {Action}, Resource: {ResourceType}:{ResourceId}, Success: {Success}, IP: {IpAddress}",
                userId, action, resourceType, resourceId, success, ipAddress ?? "N/A");
        }
        catch (Exception ex)
        {
            // Ne pas bloquer l'opération principale en cas d'erreur d'audit
            _logger.LogError(ex, "Erreur lors de l'enregistrement de l'audit: {Message}", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task LogAuthFailureAsync(string identifier, string? ipAddress, string reason)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = 0, // Utilisateur non identifié
                Action = "AUTH_FAILURE",
                ResourceType = "Authentication",
                Details = $"Identifier: {identifier}, Reason: {reason}",
                IpAddress = ipAddress,
                Success = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "[SECURITY] Auth failure for '{Identifier}' from IP {IpAddress}: {Reason}",
                identifier, ipAddress ?? "N/A", reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'enregistrement de l'échec d'auth: {Message}", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task LogSensitiveAccessAsync(int userId, string resourceType, int resourceId, string? ipAddress)
    {
        await LogActionWithIpAsync(
            userId, 
            "SENSITIVE_DATA_ACCESS", 
            resourceType, 
            ipAddress, 
            resourceId, 
            $"Accès aux données sensibles: {resourceType} #{resourceId}",
            true);
    }

    /// <inheritdoc />
    public async Task<List<AuditLogDto>> GetUserAuditHistoryAsync(int userId, int limit = 100)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Action = a.Action,
                ResourceType = a.ResourceType,
                ResourceId = a.ResourceId,
                Details = a.Details,
                IpAddress = a.IpAddress,
                Success = a.Success,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<AuditLogDto>> GetResourceAuditHistoryAsync(string resourceType, int resourceId, int limit = 50)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.ResourceType == resourceType && a.ResourceId == resourceId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Action = a.Action,
                ResourceType = a.ResourceType,
                ResourceId = a.ResourceId,
                Details = a.Details,
                IpAddress = a.IpAddress,
                Success = a.Success,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<AuditLogsPagedResult> GetAuditLogsPagedAsync(
        int page, int pageSize, string? action, string? resourceType,
        int? userId, DateTime? dateFrom, DateTime? dateTo, bool? successOnly)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        // Appliquer les filtres
        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action == action);

        if (!string.IsNullOrEmpty(resourceType))
            query = query.Where(a => a.ResourceType == resourceType);

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (dateFrom.HasValue)
            query = query.Where(a => a.CreatedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(a => a.CreatedAt <= dateTo.Value.AddDays(1));

        if (successOnly.HasValue)
            query = query.Where(a => a.Success == successOnly.Value);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Action = a.Action,
                ResourceType = a.ResourceType,
                ResourceId = a.ResourceId,
                Details = a.Details,
                IpAddress = a.IpAddress,
                Success = a.Success,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return new AuditLogsPagedResult
        {
            Logs = logs,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    /// <inheritdoc />
    public async Task<AuditStatsDto> GetAuditStatsAsync(int days = 7)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        var stats = new AuditStatsDto
        {
            TotalLogs = await _context.AuditLogs.CountAsync(a => a.CreatedAt >= startDate),
            TotalSuccess = await _context.AuditLogs.CountAsync(a => a.CreatedAt >= startDate && a.Success),
            TotalFailures = await _context.AuditLogs.CountAsync(a => a.CreatedAt >= startDate && !a.Success),
            AuthFailures = await _context.AuditLogs.CountAsync(a => a.CreatedAt >= startDate && a.Action == "AUTH_FAILURE"),
            SensitiveAccess = await _context.AuditLogs.CountAsync(a => a.CreatedAt >= startDate && a.Action == "SENSITIVE_DATA_ACCESS")
        };

        // Logs par jour
        stats.LogsByDay = await _context.AuditLogs
            .Where(a => a.CreatedAt >= startDate)
            .GroupBy(a => a.CreatedAt.Date)
            .Select(g => new AuditStatByDay
            {
                Date = g.Key,
                Count = g.Count(),
                Failures = g.Count(a => !a.Success)
            })
            .OrderBy(s => s.Date)
            .ToListAsync();

        // Top actions
        stats.TopActions = await _context.AuditLogs
            .Where(a => a.CreatedAt >= startDate)
            .GroupBy(a => a.Action)
            .Select(g => new AuditStatByAction
            {
                Action = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(s => s.Count)
            .Take(10)
            .ToListAsync();

        return stats;
    }

    /// <inheritdoc />
    public async Task<List<string>> GetDistinctActionsAsync()
    {
        return await _context.AuditLogs
            .Select(a => a.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<string>> GetDistinctResourceTypesAsync()
    {
        return await _context.AuditLogs
            .Select(a => a.ResourceType)
            .Distinct()
            .OrderBy(r => r)
            .ToListAsync();
    }
}
