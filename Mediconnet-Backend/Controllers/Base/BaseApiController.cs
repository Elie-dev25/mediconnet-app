using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Mediconnet_Backend.Controllers.Base;

/// <summary>
/// Contrôleur de base pour tous les contrôleurs API
/// Fournit les méthodes communes : récupération de l'ID utilisateur, vérification des rôles
/// </summary>
[ApiController]
[Authorize]
public abstract class BaseApiController : ControllerBase
{
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
}
