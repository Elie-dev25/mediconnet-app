using Microsoft.Extensions.Caching.Memory;

namespace Mediconnet_Backend.Infrastructure.Caching;

/// <summary>
/// Implémentation du cache en mémoire (fallback si Redis n'est pas disponible)
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly HashSet<string> _keys;
    private readonly object _lock = new();

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
        _keys = new HashSet<string>();
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = _cache.TryGetValue(key, out T? result) ? result : default;
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var options = new MemoryCacheEntryOptions();
        
        if (expiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiration;
        }
        else
        {
            options.SlidingExpiration = TimeSpan.FromMinutes(30);
        }

        options.RegisterPostEvictionCallback((key, value, reason, state) =>
        {
            lock (_lock)
            {
                _keys.Remove(key.ToString()!);
            }
        });

        _cache.Set(key, value, options);
        
        lock (_lock)
        {
            _keys.Add(key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
        lock (_lock)
        {
            _keys.Remove(key);
        }
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var regex = new System.Text.RegularExpressions.Regex(
            "^" + pattern.Replace("*", ".*") + "$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        List<string> keysToRemove;
        lock (_lock)
        {
            keysToRemove = _keys.Where(k => regex.IsMatch(k)).ToList();
        }

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
        }

        lock (_lock)
        {
            foreach (var key in keysToRemove)
            {
                _keys.Remove(key);
            }
        }

        return Task.CompletedTask;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var value = await GetAsync<T>(key, cancellationToken);
        
        if (value != null)
        {
            return value;
        }

        value = await factory();
        await SetAsync(key, value, expiration, cancellationToken);
        
        return value;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_cache.TryGetValue(key, out _));
    }
}
