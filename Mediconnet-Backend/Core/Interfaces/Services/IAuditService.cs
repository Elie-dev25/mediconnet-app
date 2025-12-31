namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Interface pour le service d'audit
/// </summary>
public interface IAuditService
{
    Task LogActionAsync(string userId, string action, string resourceType, string? details = null);
}
