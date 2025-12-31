namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Interface pour le service de gestion des roles
/// </summary>
public interface IRoleService
{
    Task<string> GetRoleAsync(int userId);
    Task<bool> IsInRoleAsync(int userId, string role);
}
