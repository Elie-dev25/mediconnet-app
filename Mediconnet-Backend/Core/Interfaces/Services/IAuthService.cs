using Mediconnet_Backend.DTOs.Auth;

namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Interface pour le service d'authentification
/// </summary>
public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<UtilisateurDto?> RegisterAsync(RegisterRequest request);
    Task<UtilisateurDto?> GetCurrentUserAsync(int userId);
    Task<bool> ValidateTokenAsync(string token);
}
