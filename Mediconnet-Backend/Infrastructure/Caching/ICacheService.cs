namespace Mediconnet_Backend.Infrastructure.Caching;

/// <summary>
/// Interface pour le service de cache
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Récupère une valeur du cache
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stocke une valeur dans le cache
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Supprime une valeur du cache
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Supprime toutes les valeurs correspondant à un pattern
    /// </summary>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Récupère ou crée une valeur en cache
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key, 
        Func<Task<T>> factory, 
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Vérifie si une clé existe dans le cache
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}
