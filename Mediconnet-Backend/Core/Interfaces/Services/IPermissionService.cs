namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Interface pour le service de permissions RBAC
/// </summary>
public interface IPermissionService
{
    // Méthodes existantes (rôles)
    Task<bool> HasRoleAsync(int userId, string role);
    Task<string> GetUserRoleAsync(int userId);
    
    // Nouvelles méthodes RBAC (permissions fines)
    
    /// <summary>
    /// Vérifie si un utilisateur a une permission spécifique
    /// Prend en compte: permissions du rôle + permissions utilisateur (override)
    /// </summary>
    Task<bool> HasPermissionAsync(int userId, string permissionCode);
    
    /// <summary>
    /// Vérifie si un utilisateur a une permission (version synchrone pour les claims)
    /// </summary>
    Task<bool> HasPermissionByRoleAsync(string role, string permissionCode);
    
    /// <summary>
    /// Récupère toutes les permissions d'un utilisateur
    /// </summary>
    Task<List<string>> GetUserPermissionsAsync(int userId);
    
    /// <summary>
    /// Récupère toutes les permissions d'un rôle
    /// </summary>
    Task<List<string>> GetRolePermissionsAsync(string role);
    
    /// <summary>
    /// Vérifie si un utilisateur a au moins une des permissions spécifiées
    /// </summary>
    Task<bool> HasAnyPermissionAsync(int userId, params string[] permissionCodes);
    
    /// <summary>
    /// Vérifie si un utilisateur a toutes les permissions spécifiées
    /// </summary>
    Task<bool> HasAllPermissionsAsync(int userId, params string[] permissionCodes);
}
