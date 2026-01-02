using Mediconnet_Backend.Core.CQRS.Queries;
using Mediconnet_Backend.Core.CQRS.Queries.Dashboard;
using Mediconnet_Backend.Core.Interfaces.Repositories;
using Mediconnet_Backend.Infrastructure.BackgroundJobs.Jobs;
using Mediconnet_Backend.Infrastructure.Caching;
using Mediconnet_Backend.Infrastructure.CQRS.Queries.Dashboard;
using Mediconnet_Backend.Infrastructure.HealthChecks;
using Mediconnet_Backend.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Mediconnet_Backend.Configuration;

/// <summary>
/// Extensions pour configurer l'infrastructure
/// </summary>
public static class InfrastructureExtensions
{
    /// <summary>
    /// Ajoute le Repository Pattern et Unit of Work
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        return services;
    }

    /// <summary>
    /// Ajoute les Query Handlers CQRS
    /// </summary>
    public static IServiceCollection AddCQRS(this IServiceCollection services)
    {
        // Dashboard queries
        services.AddScoped<IQueryHandler<GetDashboardStatsQuery, DashboardStatsResult>, GetDashboardStatsQueryHandler>();
        
        return services;
    }

    /// <summary>
    /// Ajoute le service de cache (Memory Cache par défaut)
    /// </summary>
    public static IServiceCollection AddCaching(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        
        return services;
    }

    /// <summary>
    /// Ajoute les jobs en arrière-plan
    /// </summary>
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services)
    {
        services.AddScoped<ReminderJob>();
        services.AddScoped<CleanupJob>();
        
        return services;
    }

    /// <summary>
    /// Configure les Health Checks
    /// </summary>
    public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "db", "sql" })
            .AddCheck<EmailServiceHealthCheck>("email", tags: new[] { "smtp", "external" });
        
        return services;
    }
}
