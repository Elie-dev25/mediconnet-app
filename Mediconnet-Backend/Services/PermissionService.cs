using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Services;

public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(ApplicationDbContext context, ILogger<PermissionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> HasRoleAsync(int userId, string role)
    {
        var utilisateur = await _context.Utilisateurs
            .FirstOrDefaultAsync(u => u.IdUser == userId);
        
        if (utilisateur == null)
            return false;

        return utilisateur.Role == role;
    }

    public async Task<string> GetUserRoleAsync(int userId)
    {
        var utilisateur = await _context.Utilisateurs
            .FirstOrDefaultAsync(u => u.IdUser == userId);
        
        if (utilisateur == null)
            return "unknown";

        return utilisateur.Role ?? "patient";
    }
}
