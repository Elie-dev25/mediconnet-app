using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;

namespace Mediconnet_Backend.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogActionAsync(string userId, string action, string resourceType, string? details = null)
    {
        try
        {
            // Pour l'instant, logger seulement en console/fichier
            // Peut être étendu pour ajouter une table d'audit si nécessaire
            
            _logger.LogInformation(
                $"[AUDIT] User: {userId}, Action: {action}, Resource: {resourceType}, Details: {details}"
            );
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error logging audit action: {ex.Message}");
        }
    }
}
