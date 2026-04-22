using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.Core.Entities;
using System.Text.Json.Serialization;

namespace Mediconnet_Backend.Controllers;

[ApiController]
[Route("api/assurances")]
[Authorize(Roles = "administrateur")]
public class AssuranceCouvertureController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AssuranceCouvertureController> _logger;

    public AssuranceCouvertureController(ApplicationDbContext context, ILogger<AssuranceCouvertureController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ==================== GET couvertures d'une assurance ====================

    /// <summary>
    /// RÃ©cupÃ©rer toutes les couvertures d'une assurance
    /// </summary>
    [HttpGet("{idAssurance}/couvertures")]
    public async Task<IActionResult> GetCouvertures(int idAssurance)
    {
        var assurance = await _context.Assurances
            .Include(a => a.Couvertures)
            .FirstOrDefaultAsync(a => a.IdAssurance == idAssurance);

        if (assurance == null)
            return NotFound(new { success = false, message = "Assurance non trouvÃ©e" });

        var couvertures = assurance.Couvertures.Select(c => new AssuranceCouvertureDto
        {
            IdCouverture = c.IdCouverture,
            IdAssurance = c.IdAssurance,
            TypePrestation = c.TypePrestation,
            TauxCouverture = c.TauxCouverture,
            PlafondAnnuel = c.PlafondAnnuel,
            PlafondParActe = c.PlafondParActe,
            Franchise = c.Franchise,
            Actif = c.Actif,
            Notes = c.Notes
        }).ToList();

        return Ok(new { success = true, data = couvertures });
    }

    // ==================== GET toutes les assurances avec couvertures ====================

    /// <summary>
    /// RÃ©cupÃ©rer toutes les assurances avec leurs couvertures
    /// </summary>
    [HttpGet("avec-couvertures")]
    public async Task<IActionResult> GetAssurancesAvecCouvertures()
    {
        var assurances = await _context.Assurances
            .Include(a => a.Couvertures)
            .Where(a => a.IsActive)
            .OrderBy(a => a.Nom)
            .ToListAsync();

        var result = assurances.Select(a => new AssuranceAvecCouverturesDto
        {
            IdAssurance = a.IdAssurance,
            Nom = a.Nom,
            TypeAssurance = a.TypeAssurance,
            IsActive = a.IsActive,
            Couvertures = a.Couvertures.Select(c => new AssuranceCouvertureDto
            {
                IdCouverture = c.IdCouverture,
                IdAssurance = c.IdAssurance,
                TypePrestation = c.TypePrestation,
                TauxCouverture = c.TauxCouverture,
                PlafondAnnuel = c.PlafondAnnuel,
                PlafondParActe = c.PlafondParActe,
                Franchise = c.Franchise,
                Actif = c.Actif,
                Notes = c.Notes
            }).ToList()
        }).ToList();

        return Ok(new { success = true, data = result });
    }

    // ==================== CREATE/UPDATE couverture ====================

    /// <summary>
    /// CrÃ©er ou mettre Ã  jour une couverture pour une assurance
    /// </summary>
    [HttpPut("{idAssurance}/couvertures")]
    public async Task<IActionResult> UpsertCouverture(int idAssurance, [FromBody] UpsertCouvertureRequest request)
    {
        var assurance = await _context.Assurances.FindAsync(idAssurance);
        if (assurance == null)
            return NotFound(new { success = false, message = "Assurance non trouvÃ©e" });

        if (request.TauxCouverture < 0 || request.TauxCouverture > 100)
            return BadRequest(new { success = false, message = "Le taux de couverture doit Ãªtre entre 0 et 100" });

        var validTypes = new[] { "consultation", "hospitalisation", "examen", "pharmacie" };
        if (!validTypes.Contains(request.TypePrestation))
            return BadRequest(new { success = false, message = $"Type de prestation invalide. Valeurs acceptÃ©es: {string.Join(", ", validTypes)}" });

        var existante = await _context.AssuranceCouvertures
            .FirstOrDefaultAsync(c => c.IdAssurance == idAssurance && c.TypePrestation == request.TypePrestation);

        if (existante != null)
        {
            existante.TauxCouverture = request.TauxCouverture;
            existante.PlafondAnnuel = request.PlafondAnnuel;
            existante.PlafondParActe = request.PlafondParActe;
            existante.Franchise = request.Franchise;
            existante.Actif = request.Actif ?? true;
            existante.Notes = request.Notes;
            existante.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var couverture = new AssuranceCouverture
            {
                IdAssurance = idAssurance,
                TypePrestation = request.TypePrestation,
                TauxCouverture = request.TauxCouverture,
                PlafondAnnuel = request.PlafondAnnuel,
                PlafondParActe = request.PlafondParActe,
                Franchise = request.Franchise,
                Actif = request.Actif ?? true,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };
            _context.AssuranceCouvertures.Add(couverture);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Couverture {Type} mise Ã  jour pour assurance {IdAssurance}: taux={Taux}%",
            request.TypePrestation, idAssurance, request.TauxCouverture);

        return Ok(new { success = true, message = "Couverture mise Ã  jour" });
    }

    // ==================== BATCH UPDATE couvertures ====================

    /// <summary>
    /// Mettre Ã  jour toutes les couvertures d'une assurance en une seule requÃªte
    /// </summary>
    [HttpPut("{idAssurance}/couvertures/batch")]
    public async Task<IActionResult> BatchUpdateCouvertures(int idAssurance, [FromBody] List<UpsertCouvertureRequest> requests)
    {
        var assurance = await _context.Assurances.FindAsync(idAssurance);
        if (assurance == null)
            return NotFound(new { success = false, message = "Assurance non trouvÃ©e" });

        var validTypes = new[] { "consultation", "hospitalisation", "examen", "pharmacie" };

        foreach (var request in requests)
        {
            if (!validTypes.Contains(request.TypePrestation))
                return BadRequest(new { success = false, message = $"Type de prestation invalide: {request.TypePrestation}" });

            if (request.TauxCouverture < 0 || request.TauxCouverture > 100)
                return BadRequest(new { success = false, message = $"Taux invalide pour {request.TypePrestation}: {request.TauxCouverture}" });

            var existante = await _context.AssuranceCouvertures
                .FirstOrDefaultAsync(c => c.IdAssurance == idAssurance && c.TypePrestation == request.TypePrestation);

            if (existante != null)
            {
                existante.TauxCouverture = request.TauxCouverture;
                existante.PlafondAnnuel = request.PlafondAnnuel;
                existante.PlafondParActe = request.PlafondParActe;
                existante.Franchise = request.Franchise;
                existante.Actif = request.Actif ?? true;
                existante.Notes = request.Notes;
                existante.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.AssuranceCouvertures.Add(new AssuranceCouverture
                {
                    IdAssurance = idAssurance,
                    TypePrestation = request.TypePrestation,
                    TauxCouverture = request.TauxCouverture,
                    PlafondAnnuel = request.PlafondAnnuel,
                    PlafondParActe = request.PlafondParActe,
                    Franchise = request.Franchise,
                    Actif = request.Actif ?? true,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Batch update couvertures pour assurance {IdAssurance}: {Count} types", idAssurance, requests.Count);

        return Ok(new { success = true, message = $"{requests.Count} couverture(s) mise(s) Ã  jour" });
    }

    // ==================== DELETE couverture ====================

    /// <summary>
    /// Supprimer une couverture
    /// </summary>
    [HttpDelete("couvertures/{idCouverture}")]
    public async Task<IActionResult> DeleteCouverture(int idCouverture)
    {
        var couverture = await _context.AssuranceCouvertures.FindAsync(idCouverture);
        if (couverture == null)
            return NotFound(new { success = false, message = "Couverture non trouvÃ©e" });

        _context.AssuranceCouvertures.Remove(couverture);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Couverture supprimÃ©e" });
    }
}

// ==================== DTOs ====================

public class AssuranceCouvertureDto
{
    public int IdCouverture { get; set; }
    public int IdAssurance { get; set; }
    public string TypePrestation { get; set; } = "";
    public decimal TauxCouverture { get; set; }
    public decimal? PlafondAnnuel { get; set; }
    public decimal? PlafondParActe { get; set; }
    public decimal? Franchise { get; set; }
    public bool Actif { get; set; }
    public string? Notes { get; set; }
}

public class AssuranceAvecCouverturesDto
{
    public int IdAssurance { get; set; }
    public string Nom { get; set; } = "";
    public string TypeAssurance { get; set; } = "";
    public bool IsActive { get; set; }
    public List<AssuranceCouvertureDto> Couvertures { get; set; } = new();
}

public class UpsertCouvertureRequest
{
    public string TypePrestation { get; set; } = "";
    [JsonRequired]
    public decimal TauxCouverture { get; set; }
    public decimal? PlafondAnnuel { get; set; }
    public decimal? PlafondParActe { get; set; }
    public decimal? Franchise { get; set; }
    public bool? Actif { get; set; }
    public string? Notes { get; set; }
}
