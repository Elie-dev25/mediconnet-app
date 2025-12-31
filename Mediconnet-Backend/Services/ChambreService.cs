using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.DTOs.Admin;

namespace Mediconnet_Backend.Services;

public interface IChambreService
{
    Task<ChambresListResponse> GetAllChambresAsync();
    Task<ChambreAdminDto?> GetChambreByIdAsync(int id);
    Task<ChambreResponse> CreateChambreAsync(CreateChambreRequest request);
    Task<ChambreResponse> UpdateChambreAsync(int id, UpdateChambreRequest request);
    Task<ChambreResponse> DeleteChambreAsync(int id);
    Task<LitResponse> AddLitToChambreAsync(int chambreId, CreateLitRequest request);
    Task<LitResponse> UpdateLitAsync(int litId, UpdateLitRequest request);
    Task<LitResponse> DeleteLitAsync(int litId);
    Task<ChambresStats> GetStatsAsync();
}

public class ChambreService : IChambreService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ChambreService> _logger;

    public ChambreService(ApplicationDbContext context, ILogger<ChambreService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ChambresListResponse> GetAllChambresAsync()
    {
        var chambres = await _context.Chambres
            .Include(c => c.Lits)
            .OrderBy(c => c.Numero)
            .ToListAsync();

        var chambresDto = chambres.Select(MapToChambreAdminDto).ToList();
        var stats = CalculateStats(chambres);

        return new ChambresListResponse
        {
            Success = true,
            Chambres = chambresDto,
            Total = chambresDto.Count,
            Stats = stats
        };
    }

    public async Task<ChambreAdminDto?> GetChambreByIdAsync(int id)
    {
        var chambre = await _context.Chambres
            .Include(c => c.Lits)
            .FirstOrDefaultAsync(c => c.IdChambre == id);

        return chambre != null ? MapToChambreAdminDto(chambre) : null;
    }

    public async Task<ChambreResponse> CreateChambreAsync(CreateChambreRequest request)
    {
        // Vérifier unicité du numéro
        var exists = await _context.Chambres.AnyAsync(c => c.Numero == request.Numero);
        if (exists)
        {
            return new ChambreResponse
            {
                Success = false,
                Message = $"Une chambre avec le numéro '{request.Numero}' existe déjà"
            };
        }

        var chambre = new Chambre
        {
            Numero = request.Numero,
            Capacite = request.Capacite,
            Etat = request.Etat ?? "bon",
            Statut = request.Statut ?? "actif",
            Lits = new List<Lit>()
        };

        // Créer les lits si spécifiés
        if (request.Lits != null && request.Lits.Any())
        {
            foreach (var litRequest in request.Lits)
            {
                chambre.Lits.Add(new Lit
                {
                    Numero = litRequest.Numero,
                    Statut = litRequest.Statut
                });
            }
        }

        _context.Chambres.Add(chambre);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Chambre créée: {Numero} (ID: {Id})", chambre.Numero, chambre.IdChambre);

        // Recharger avec les lits
        chambre = await _context.Chambres
            .Include(c => c.Lits)
            .FirstOrDefaultAsync(c => c.IdChambre == chambre.IdChambre);

        return new ChambreResponse
        {
            Success = true,
            Message = "Chambre créée avec succès",
            Chambre = MapToChambreAdminDto(chambre!)
        };
    }

    public async Task<ChambreResponse> UpdateChambreAsync(int id, UpdateChambreRequest request)
    {
        var chambre = await _context.Chambres
            .Include(c => c.Lits)
            .FirstOrDefaultAsync(c => c.IdChambre == id);

        if (chambre == null)
        {
            return new ChambreResponse
            {
                Success = false,
                Message = "Chambre non trouvée"
            };
        }

        // Vérifier unicité du numéro si modifié
        if (!string.IsNullOrEmpty(request.Numero) && request.Numero != chambre.Numero)
        {
            var exists = await _context.Chambres.AnyAsync(c => c.Numero == request.Numero && c.IdChambre != id);
            if (exists)
            {
                return new ChambreResponse
                {
                    Success = false,
                    Message = $"Une chambre avec le numéro '{request.Numero}' existe déjà"
                };
            }
            chambre.Numero = request.Numero;
        }

        if (request.Capacite.HasValue)
            chambre.Capacite = request.Capacite.Value;
        if (!string.IsNullOrEmpty(request.Etat))
            chambre.Etat = request.Etat;
        if (!string.IsNullOrEmpty(request.Statut))
            chambre.Statut = request.Statut;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Chambre mise à jour: {Id}", id);

        return new ChambreResponse
        {
            Success = true,
            Message = "Chambre mise à jour avec succès",
            Chambre = MapToChambreAdminDto(chambre)
        };
    }

    public async Task<ChambreResponse> DeleteChambreAsync(int id)
    {
        var chambre = await _context.Chambres
            .Include(c => c.Lits)
            .FirstOrDefaultAsync(c => c.IdChambre == id);

        if (chambre == null)
        {
            return new ChambreResponse
            {
                Success = false,
                Message = "Chambre non trouvée"
            };
        }

        // Vérifier qu'aucun lit n'est occupé
        var litsOccupes = chambre.Lits?.Any(l => l.Statut == "occupe") ?? false;
        if (litsOccupes)
        {
            return new ChambreResponse
            {
                Success = false,
                Message = "Impossible de supprimer une chambre avec des lits occupés"
            };
        }

        // Supprimer les lits d'abord
        if (chambre.Lits != null)
        {
            _context.Lits.RemoveRange(chambre.Lits);
        }

        _context.Chambres.Remove(chambre);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Chambre supprimée: {Id}", id);

        return new ChambreResponse
        {
            Success = true,
            Message = "Chambre supprimée avec succès"
        };
    }

    public async Task<LitResponse> AddLitToChambreAsync(int chambreId, CreateLitRequest request)
    {
        var chambre = await _context.Chambres
            .Include(c => c.Lits)
            .FirstOrDefaultAsync(c => c.IdChambre == chambreId);

        if (chambre == null)
        {
            return new LitResponse
            {
                Success = false,
                Message = "Chambre non trouvée"
            };
        }

        // Vérifier unicité du numéro de lit dans la chambre
        var exists = chambre.Lits?.Any(l => l.Numero == request.Numero) ?? false;
        if (exists)
        {
            return new LitResponse
            {
                Success = false,
                Message = $"Un lit avec le numéro '{request.Numero}' existe déjà dans cette chambre"
            };
        }

        var lit = new Lit
        {
            Numero = request.Numero,
            Statut = request.Statut,
            IdChambre = chambreId
        };

        _context.Lits.Add(lit);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Lit ajouté: {Numero} à chambre {ChambreId}", lit.Numero, chambreId);

        return new LitResponse
        {
            Success = true,
            Message = "Lit ajouté avec succès",
            Lit = new LitAdminDto
            {
                IdLit = lit.IdLit,
                Numero = lit.Numero ?? "",
                Statut = lit.Statut ?? "libre",
                IdChambre = chambreId,
                NumeroChambre = chambre.Numero,
                EstOccupe = lit.Statut == "occupe"
            }
        };
    }

    public async Task<LitResponse> UpdateLitAsync(int litId, UpdateLitRequest request)
    {
        var lit = await _context.Lits
            .Include(l => l.Chambre)
            .FirstOrDefaultAsync(l => l.IdLit == litId);

        if (lit == null)
        {
            return new LitResponse
            {
                Success = false,
                Message = "Lit non trouvé"
            };
        }

        // Si le lit est occupé, on ne peut pas le mettre hors service
        if (lit.Statut == "occupe" && request.Statut == "hors_service")
        {
            return new LitResponse
            {
                Success = false,
                Message = "Impossible de mettre hors service un lit occupé"
            };
        }

        if (!string.IsNullOrEmpty(request.Numero))
            lit.Numero = request.Numero;
        if (!string.IsNullOrEmpty(request.Statut))
            lit.Statut = request.Statut;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Lit mis à jour: {Id}", litId);

        return new LitResponse
        {
            Success = true,
            Message = "Lit mis à jour avec succès",
            Lit = new LitAdminDto
            {
                IdLit = lit.IdLit,
                Numero = lit.Numero ?? "",
                Statut = lit.Statut ?? "libre",
                IdChambre = lit.IdChambre,
                NumeroChambre = lit.Chambre?.Numero,
                EstOccupe = lit.Statut == "occupe"
            }
        };
    }

    public async Task<LitResponse> DeleteLitAsync(int litId)
    {
        var lit = await _context.Lits.FirstOrDefaultAsync(l => l.IdLit == litId);

        if (lit == null)
        {
            return new LitResponse
            {
                Success = false,
                Message = "Lit non trouvé"
            };
        }

        if (lit.Statut == "occupe")
        {
            return new LitResponse
            {
                Success = false,
                Message = "Impossible de supprimer un lit occupé"
            };
        }

        _context.Lits.Remove(lit);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Lit supprimé: {Id}", litId);

        return new LitResponse
        {
            Success = true,
            Message = "Lit supprimé avec succès"
        };
    }

    public async Task<ChambresStats> GetStatsAsync()
    {
        var chambres = await _context.Chambres
            .Include(c => c.Lits)
            .ToListAsync();

        return CalculateStats(chambres);
    }

    private static ChambreAdminDto MapToChambreAdminDto(Chambre chambre)
    {
        var lits = chambre.Lits?.ToList() ?? new List<Lit>();
        
        return new ChambreAdminDto
        {
            IdChambre = chambre.IdChambre,
            Numero = chambre.Numero ?? "",
            Capacite = chambre.Capacite ?? 0,
            Etat = chambre.Etat ?? "bon",
            Statut = chambre.Statut ?? "actif",
            NombreLits = lits.Count,
            LitsLibres = lits.Count(l => l.Statut == "libre"),
            LitsOccupes = lits.Count(l => l.Statut == "occupe"),
            LitsHorsService = lits.Count(l => l.Statut == "hors_service"),
            Lits = lits.Select(l => new LitAdminDto
            {
                IdLit = l.IdLit,
                Numero = l.Numero ?? "",
                Statut = l.Statut ?? "libre",
                IdChambre = l.IdChambre,
                NumeroChambre = chambre.Numero,
                EstOccupe = l.Statut == "occupe"
            }).ToList()
        };
    }

    private static ChambresStats CalculateStats(List<Chambre> chambres)
    {
        var allLits = chambres.SelectMany(c => c.Lits ?? new List<Lit>()).ToList();
        
        return new ChambresStats
        {
            TotalChambres = chambres.Count,
            ChambresActives = chambres.Count(c => c.Statut == "actif"),
            TotalLits = allLits.Count,
            LitsLibres = allLits.Count(l => l.Statut == "libre"),
            LitsOccupes = allLits.Count(l => l.Statut == "occupe"),
            LitsHorsService = allLits.Count(l => l.Statut == "hors_service")
        };
    }
}
