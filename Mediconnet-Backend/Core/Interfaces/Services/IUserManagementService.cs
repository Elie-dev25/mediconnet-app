using Mediconnet_Backend.DTOs.Admin;

namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Interface pour la gestion des utilisateurs
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// Obtenir tous les utilisateurs
    /// </summary>
    Task<List<UserDto>> GetAllUsersAsync();

    /// <summary>
    /// Creer un nouvel utilisateur
    /// </summary>
    Task<(bool Success, string Message, int? UserId)> CreateUserAsync(CreateUserRequest request);

    /// <summary>
    /// Supprimer un utilisateur
    /// </summary>
    Task<(bool Success, string Message)> DeleteUserAsync(int userId, int? currentUserId);

    /// <summary>
    /// Obtenir toutes les specialites
    /// </summary>
    Task<List<SpecialiteDto>> GetSpecialitesAsync();
}
