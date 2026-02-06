using System.Text.Json;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Services;

public interface IStandardChambreService
{
    Task<List<StandardChambreDto>> GetAllAsync();
    Task<List<StandardChambreSelectDto>> GetForSelectAsync();
    Task<StandardChambreDto?> GetByIdAsync(int id);
    Task<StandardChambreDto> CreateAsync(CreateStandardChambreRequest request);
    Task<StandardChambreDto?> UpdateAsync(int id, UpdateStandardChambreRequest request);
    Task<bool> DeleteAsync(int id);
}

public class StandardChambreService : IStandardChambreService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StandardChambreService> _logger;

    public StandardChambreService(ApplicationDbContext context, ILogger<StandardChambreService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<StandardChambreDto>> GetAllAsync()
    {
        var standards = await _context.StandardsChambres
            .Include(s => s.Chambres)
                .ThenInclude(c => c!.Lits)
            .OrderBy(s => s.Nom)
            .ToListAsync();

        return standards.Select(s => MapToDto(s)).ToList();
    }

    public async Task<List<StandardChambreSelectDto>> GetForSelectAsync()
    {
        var standards = await _context.StandardsChambres
            .Where(s => s.Actif)
            .OrderBy(s => s.PrixJournalier)
            .Select(s => new StandardChambreSelectDto
            {
                IdStandard = s.IdStandard,
                Nom = s.Nom,
                PrixJournalier = s.PrixJournalier,
                Privileges = ParsePrivileges(s.Privileges),
                Localisation = s.Localisation
            })
            .ToListAsync();

        return standards;
    }

    public async Task<StandardChambreDto?> GetByIdAsync(int id)
    {
        var standard = await _context.StandardsChambres
            .Include(s => s.Chambres)
                .ThenInclude(c => c!.Lits)
            .FirstOrDefaultAsync(s => s.IdStandard == id);

        return standard != null ? MapToDto(standard) : null;
    }

    public async Task<StandardChambreDto> CreateAsync(CreateStandardChambreRequest request)
    {
        var standard = new StandardChambre
        {
            Nom = request.Nom,
            Description = request.Description,
            PrixJournalier = request.PrixJournalier,
            Privileges = JsonSerializer.Serialize(request.Privileges),
            Localisation = request.Localisation,
            Actif = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.StandardsChambres.Add(standard);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Standard de chambre créé: {standard.Nom} (ID: {standard.IdStandard})");

        return MapToDto(standard);
    }

    public async Task<StandardChambreDto?> UpdateAsync(int id, UpdateStandardChambreRequest request)
    {
        var standard = await _context.StandardsChambres
            .Include(s => s.Chambres)
                .ThenInclude(c => c!.Lits)
            .FirstOrDefaultAsync(s => s.IdStandard == id);

        if (standard == null)
            return null;

        if (request.Nom != null)
            standard.Nom = request.Nom;
        if (request.Description != null)
            standard.Description = request.Description;
        if (request.PrixJournalier.HasValue)
            standard.PrixJournalier = request.PrixJournalier.Value;
        if (request.Privileges != null)
            standard.Privileges = JsonSerializer.Serialize(request.Privileges);
        if (request.Localisation != null)
            standard.Localisation = request.Localisation;
        if (request.Actif.HasValue)
            standard.Actif = request.Actif.Value;

        standard.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation($"Standard de chambre mis à jour: {standard.Nom} (ID: {standard.IdStandard})");

        return MapToDto(standard);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var standard = await _context.StandardsChambres
            .Include(s => s.Chambres)
            .FirstOrDefaultAsync(s => s.IdStandard == id);

        if (standard == null)
            return false;

        // Vérifier si des chambres utilisent ce standard
        if (standard.Chambres?.Any() == true)
        {
            throw new InvalidOperationException($"Impossible de supprimer ce standard car {standard.Chambres.Count} chambre(s) l'utilisent.");
        }

        _context.StandardsChambres.Remove(standard);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Standard de chambre supprimé: {standard.Nom} (ID: {id})");

        return true;
    }

    private StandardChambreDto MapToDto(StandardChambre standard)
    {
        var chambres = standard.Chambres?.ToList() ?? new List<Chambre>();
        var chambresDisponibles = chambres.Count(c => 
            c.Lits?.Any(l => l.Statut == "disponible") == true);

        return new StandardChambreDto
        {
            IdStandard = standard.IdStandard,
            Nom = standard.Nom,
            Description = standard.Description,
            PrixJournalier = standard.PrixJournalier,
            Privileges = ParsePrivileges(standard.Privileges),
            Localisation = standard.Localisation,
            Actif = standard.Actif,
            NombreChambres = chambres.Count,
            ChambresDisponibles = chambresDisponibles
        };
    }

    private static List<string> ParsePrivileges(string? privilegesJson)
    {
        if (string.IsNullOrEmpty(privilegesJson))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(privilegesJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
