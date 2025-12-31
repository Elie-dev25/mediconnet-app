using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Services;

public class RoleService : IRoleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RoleService> _logger;

    public RoleService(ApplicationDbContext context, ILogger<RoleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> GetRoleAsync(int userId)
    {
        var utilisateur = await _context.Utilisateurs
            .FirstOrDefaultAsync(u => u.IdUser == userId);
        
        return utilisateur?.Role ?? "unknown";
    }

    public async Task<bool> IsInRoleAsync(int userId, string role)
    {
        var utilisateur = await _context.Utilisateurs
            .FirstOrDefaultAsync(u => u.IdUser == userId);
        
        return utilisateur?.Role == role;
    }
}
