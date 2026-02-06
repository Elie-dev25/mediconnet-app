using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Mediconnet_Backend.Core.Interfaces.Services;

namespace Mediconnet_Backend.Core.Attributes;

/// <summary>
/// Attribut pour exiger une permission spécifique sur un endpoint
/// Usage: [RequirePermission("patients.view")]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    public string PermissionCode { get; }
    
    public RequirePermissionAttribute(string permissionCode)
    {
        PermissionCode = permissionCode;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userIdClaim = user.FindFirst("userId");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var permissionService = context.HttpContext.RequestServices.GetService<IPermissionService>();
        if (permissionService == null)
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        var hasPermission = await permissionService.HasPermissionAsync(userId, PermissionCode);
        if (!hasPermission)
        {
            context.Result = new ForbidResult();
        }
    }
}

/// <summary>
/// Attribut pour exiger au moins une des permissions spécifiées
/// Usage: [RequireAnyPermission("patients.view", "patients.edit")]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireAnyPermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    public string[] PermissionCodes { get; }
    
    public RequireAnyPermissionAttribute(params string[] permissionCodes)
    {
        PermissionCodes = permissionCodes;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userIdClaim = user.FindFirst("userId");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var permissionService = context.HttpContext.RequestServices.GetService<IPermissionService>();
        if (permissionService == null)
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        var hasAnyPermission = await permissionService.HasAnyPermissionAsync(userId, PermissionCodes);
        if (!hasAnyPermission)
        {
            context.Result = new ForbidResult();
        }
    }
}
