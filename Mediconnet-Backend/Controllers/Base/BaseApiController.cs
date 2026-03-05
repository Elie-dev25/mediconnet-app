using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Mediconnet_Backend.Core.Interfaces.Services;

namespace Mediconnet_Backend.Controllers.Base;

/// <summary>
/// Contrôleur de base pour tous les contrôleurs API
/// Fournit les méthodes communes : récupération de l'ID utilisateur, vérification des rôles et permissions
/// </summary>
[ApiController]
[Authorize]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Service de permissions (injecté via HttpContext.RequestServices)
    /// </summary>
    protected IPermissionService? PermissionService => 
        HttpContext.RequestServices.GetService<IPermissionService>();
    /// <summary>
    /// Obtient l'ID de l'utilisateur connecté depuis les claims JWT
    /// </summary>
    protected int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userId");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return null;
        return userId;
    }

    /// <summary>
    /// Vérifie si l'utilisateur courant a le rôle spécifié
    /// </summary>
    protected bool HasRole(string role)
    {
        return User.IsInRole(role) ||
               User.Claims.Any(c => (c.Type == ClaimTypes.Role || c.Type == "role") && c.Value == role);
    }

    /// <summary>
    /// Récupère le rôle de l'utilisateur courant
    /// </summary>
    protected string? GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value
            ?? User.FindFirst("role")?.Value;
    }

    /// <summary>
    /// Vérifie si l'utilisateur courant est patient
    /// </summary>
    protected bool IsPatient() => HasRole("patient");

    /// <summary>
    /// Vérifie si l'utilisateur courant est médecin
    /// </summary>
    protected bool IsMedecin() => HasRole("medecin");

    /// <summary>
    /// Vérifie si l'utilisateur courant est caissier
    /// </summary>
    protected bool IsCaissier() => HasRole("caissier") || IsAdmin();

    /// <summary>
    /// Vérifie si l'utilisateur courant est infirmier
    /// </summary>
    protected bool IsInfirmier() => HasRole("infirmier");

    /// <summary>
    /// Vérifie si l'utilisateur courant est administrateur
    /// </summary>
    protected bool IsAdmin() => HasRole("administrateur");

    /// <summary>
    /// Retourne Unauthorized si l'utilisateur n'est pas connecté
    /// </summary>
    protected IActionResult? CheckAuthentication()
    {
        return GetCurrentUserId() == null ? Unauthorized() : null;
    }

    /// <summary>
    /// Retourne Forbid si l'utilisateur n'a pas le rôle requis
    /// </summary>
    protected IActionResult? CheckRole(string role)
    {
        return HasRole(role) ? null : Forbid();
    }

    /// <summary>
    /// Retourne Forbid si l'utilisateur n'est pas admin
    /// </summary>
    protected IActionResult? CheckAdminAccess() => CheckRole("administrateur");

    /// <summary>
    /// Retourne Forbid si l'utilisateur n'est pas caissier ou admin
    /// </summary>
    protected IActionResult? CheckCaissierAccess() => IsCaissier() ? null : Forbid();

    /// <summary>
    /// Retourne Forbid si l'utilisateur n'est pas médecin
    /// </summary>
    protected IActionResult? CheckMedecinAccess() => CheckRole("medecin");

    #region Méthodes de vérification des permissions (RBAC)

    /// <summary>
    /// Vérifie si l'utilisateur courant a une permission spécifique
    /// </summary>
    protected async Task<bool> HasPermissionAsync(string permissionCode)
    {
        var userId = GetCurrentUserId();
        if (userId == null || PermissionService == null)
            return false;

        return await PermissionService.HasPermissionAsync(userId.Value, permissionCode);
    }

    /// <summary>
    /// Vérifie si l'utilisateur courant a au moins une des permissions spécifiées
    /// </summary>
    protected async Task<bool> HasAnyPermissionAsync(params string[] permissionCodes)
    {
        var userId = GetCurrentUserId();
        if (userId == null || PermissionService == null)
            return false;

        return await PermissionService.HasAnyPermissionAsync(userId.Value, permissionCodes);
    }

    /// <summary>
    /// Retourne Forbid si l'utilisateur n'a pas la permission requise
    /// </summary>
    protected async Task<IActionResult?> CheckPermissionAsync(string permissionCode)
    {
        return await HasPermissionAsync(permissionCode) ? null : Forbid();
    }

    /// <summary>
    /// Retourne Forbid si l'utilisateur n'a aucune des permissions requises
    /// </summary>
    protected async Task<IActionResult?> CheckAnyPermissionAsync(params string[] permissionCodes)
    {
        return await HasAnyPermissionAsync(permissionCodes) ? null : Forbid();
    }

    /// <summary>
    /// Récupère toutes les permissions de l'utilisateur courant
    /// </summary>
    protected async Task<List<string>> GetCurrentUserPermissionsAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null || PermissionService == null)
            return new List<string>();

        return await PermissionService.GetUserPermissionsAsync(userId.Value);
    }

    #endregion
}
