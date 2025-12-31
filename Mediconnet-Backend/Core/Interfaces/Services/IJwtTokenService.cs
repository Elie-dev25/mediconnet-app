namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Interface pour le service de generation de tokens JWT
/// </summary>
public interface IJwtTokenService
{
    Task<string> GenerateTokenAsync(int userId, string role);
    string? GetUserIdFromToken(string token);
}
