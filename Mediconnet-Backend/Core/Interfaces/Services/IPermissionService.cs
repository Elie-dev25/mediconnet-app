namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Interface pour le service de permissions
/// </summary>
public interface IPermissionService
{
    Task<bool> HasRoleAsync(int userId, string role);
    Task<string> GetUserRoleAsync(int userId);
}
