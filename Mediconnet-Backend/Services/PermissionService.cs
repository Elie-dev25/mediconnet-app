using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Mediconnet_Backend.Services;

public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PermissionService> _logger;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public PermissionService(ApplicationDbContext context, ILogger<PermissionService> logger, IMemoryCache cache)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
    }

    #region Méthodes existantes (rôles)

    public async Task<bool> HasRoleAsync(int userId, string role)
    {
        var utilisateur = await _context.Utilisateurs
            .FirstOrDefaultAsync(u => u.IdUser == userId);
        
        if (utilisateur == null)
            return false;

        return utilisateur.Role == role;
    }

    public async Task<string> GetUserRoleAsync(int userId)
    {
        var utilisateur = await _context.Utilisateurs
            .FirstOrDefaultAsync(u => u.IdUser == userId);
        
        if (utilisateur == null)
            return "unknown";

        return utilisateur.Role ?? "patient";
    }

    #endregion

    #region Nouvelles méthodes RBAC

    /// <summary>
    /// Vérifie si un utilisateur a une permission spécifique
    /// Logique: (permissions du rôle + permissions accordées) - permissions révoquées
    /// </summary>
    public async Task<bool> HasPermissionAsync(int userId, string permissionCode)
    {
        try
        {
            // 1. Récupérer le rôle de l'utilisateur
            var userRole = await GetUserRoleAsync(userId);
            if (userRole == "unknown")
                return false;

            // 2. Admin a toutes les permissions
            if (userRole == "administrateur")
                return true;

            // 3. Vérifier les permissions spécifiques à l'utilisateur (override)
            var userPermission = await _context.UserPermissions
                .Include(up => up.Permission)
                .FirstOrDefaultAsync(up => up.IdUser == userId && 
                                          up.Permission != null && 
                                          up.Permission.Code == permissionCode &&
                                          up.Permission.Actif);

            if (userPermission != null)
            {
                // L'utilisateur a une permission spécifique (accordée ou révoquée)
                return userPermission.Granted;
            }

            // 4. Vérifier les permissions du rôle
            return await HasPermissionByRoleAsync(userRole, permissionCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la vérification de permission {Permission} pour l'utilisateur {UserId}", permissionCode, userId);
            return false;
        }
    }

    /// <summary>
    /// Vérifie si un rôle a une permission spécifique
    /// </summary>
    public async Task<bool> HasPermissionByRoleAsync(string role, string permissionCode)
    {
        var cacheKey = $"role_permission_{role}_{permissionCode}";
        
        if (_cache.TryGetValue(cacheKey, out bool hasPermission))
            return hasPermission;

        hasPermission = await _context.RolePermissions
            .Include(rp => rp.Permission)
            .AnyAsync(rp => rp.Role == role && 
                           rp.Actif &&
                           rp.Permission != null && 
                           rp.Permission.Code == permissionCode &&
                           rp.Permission.Actif);

        _cache.Set(cacheKey, hasPermission, CacheDuration);
        return hasPermission;
    }

    /// <summary>
    /// Récupère toutes les permissions effectives d'un utilisateur
    /// </summary>
    public async Task<List<string>> GetUserPermissionsAsync(int userId)
    {
        try
        {
            var userRole = await GetUserRoleAsync(userId);
            if (userRole == "unknown")
                return new List<string>();

            // Admin a toutes les permissions
            if (userRole == "administrateur")
            {
                return await _context.Permissions
                    .Where(p => p.Actif)
                    .Select(p => p.Code)
                    .ToListAsync();
            }

            // Permissions du rôle
            var rolePermissions = await GetRolePermissionsAsync(userRole);

            // Permissions spécifiques à l'utilisateur
            var userSpecificPermissions = await _context.UserPermissions
                .Include(up => up.Permission)
                .Where(up => up.IdUser == userId && up.Permission != null && up.Permission.Actif)
                .ToListAsync();

            // Ajouter les permissions accordées
            var grantedPermissions = userSpecificPermissions
                .Where(up => up.Granted && up.Permission != null)
                .Select(up => up.Permission!.Code)
                .ToList();

            // Retirer les permissions révoquées
            var revokedPermissions = userSpecificPermissions
                .Where(up => !up.Granted && up.Permission != null)
                .Select(up => up.Permission!.Code)
                .ToHashSet();

            var effectivePermissions = rolePermissions
                .Union(grantedPermissions)
                .Where(p => !revokedPermissions.Contains(p))
                .Distinct()
                .ToList();

            return effectivePermissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des permissions pour l'utilisateur {UserId}", userId);
            return new List<string>();
        }
    }

    /// <summary>
    /// Récupère toutes les permissions d'un rôle
    /// </summary>
    public async Task<List<string>> GetRolePermissionsAsync(string role)
    {
        var cacheKey = $"role_permissions_{role}";
        
        if (_cache.TryGetValue(cacheKey, out List<string>? permissions) && permissions != null)
            return permissions;

        permissions = await _context.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => rp.Role == role && rp.Actif && rp.Permission != null && rp.Permission.Actif)
            .Select(rp => rp.Permission!.Code)
            .ToListAsync();

        _cache.Set(cacheKey, permissions, CacheDuration);
        return permissions;
    }

    /// <summary>
    /// Vérifie si un utilisateur a au moins une des permissions spécifiées
    /// </summary>
    public async Task<bool> HasAnyPermissionAsync(int userId, params string[] permissionCodes)
    {
        foreach (var code in permissionCodes)
        {
            if (await HasPermissionAsync(userId, code))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Vérifie si un utilisateur a toutes les permissions spécifiées
    /// </summary>
    public async Task<bool> HasAllPermissionsAsync(int userId, params string[] permissionCodes)
    {
        foreach (var code in permissionCodes)
        {
            if (!await HasPermissionAsync(userId, code))
                return false;
        }
        return true;
    }

    #endregion
}
