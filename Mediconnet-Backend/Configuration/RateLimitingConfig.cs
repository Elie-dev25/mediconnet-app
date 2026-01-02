using AspNetCoreRateLimit;

namespace Mediconnet_Backend.Configuration;

/// <summary>
/// Configuration du Rate Limiting pour protéger contre les attaques par force brute
/// </summary>
public static class RateLimitingConfig
{
    /// <summary>
    /// Configure les services de rate limiting
    /// </summary>
    public static IServiceCollection AddRateLimitingServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Charger la configuration depuis appsettings.json
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
        services.Configure<IpRateLimitPolicies>(configuration.GetSection("IpRateLimitPolicies"));

        // Stockage en mémoire pour les compteurs
        services.AddInMemoryRateLimiting();

        // Résolveur de configuration
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

        return services;
    }

    /// <summary>
    /// Obtient la configuration par défaut du rate limiting
    /// </summary>
    public static IpRateLimitOptions GetDefaultOptions()
    {
        return new IpRateLimitOptions
        {
            EnableEndpointRateLimiting = true,
            StackBlockedRequests = false,
            HttpStatusCode = 429,
            RealIpHeader = "X-Real-IP",
            ClientIdHeader = "X-ClientId",
            GeneralRules = new List<RateLimitRule>
            {
                // Règle générale: 100 requêtes par minute
                new RateLimitRule
                {
                    Endpoint = "*",
                    Period = "1m",
                    Limit = 100
                },
                // Règle pour l'authentification: 10 tentatives par minute (anti-brute force)
                new RateLimitRule
                {
                    Endpoint = "*:/api/auth/login",
                    Period = "1m",
                    Limit = 10
                },
                new RateLimitRule
                {
                    Endpoint = "*:/api/auth/register",
                    Period = "1m",
                    Limit = 5
                },
                // Règle pour les endpoints sensibles: 30 par minute
                new RateLimitRule
                {
                    Endpoint = "*:/api/patient/*",
                    Period = "1m",
                    Limit = 30
                },
                new RateLimitRule
                {
                    Endpoint = "*:/api/medecin/*",
                    Period = "1m",
                    Limit = 50
                },
                new RateLimitRule
                {
                    Endpoint = "*:/api/consultations/*",
                    Period = "1m",
                    Limit = 30
                }
            }
        };
    }
}
