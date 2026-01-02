using Mediconnet_Backend.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Mediconnet_Backend.Infrastructure.HealthChecks;

/// <summary>
/// Health Check pour la base de données
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;

    public DatabaseHealthCheck(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Vérifie la connexion à la base de données
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            
            if (canConnect)
            {
                return HealthCheckResult.Healthy("Database connection is healthy");
            }
            
            return HealthCheckResult.Unhealthy("Cannot connect to database");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database health check failed", ex);
        }
    }
}
