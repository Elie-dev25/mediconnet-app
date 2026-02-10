using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Core.Entities.Documents;
using Mediconnet_Backend.Data;
using System.Security.Claims;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// Controller pour la gestion des alertes système
/// Monitoring du stockage et de la sécurité
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "administrateur")]
public class AlertesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AlertesController> _logger;

    public AlertesController(
        ApplicationDbContext context,
        ILogger<AlertesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Récupérer les alertes actives (non acquittées)
    /// </summary>
    [HttpGet("actives")]
    public async Task<IActionResult> GetActiveAlerts([FromQuery] int limit = 50)
    {
        try
        {
            var alertes = await _context.AlertesSysteme
                .Where(a => !a.Acquittee)
                .OrderByDescending(a => a.Severite == "emergency" ? 0 : 
                                        a.Severite == "critical" ? 1 : 
                                        a.Severite == "warning" ? 2 : 3)
                .ThenByDescending(a => a.CreatedAt)
                .Take(limit)
                .Select(a => new
                {
                    a.IdAlerte,
                    a.TypeAlerte,
                    a.Message,
                    a.Severite,
                    a.Source,
                    a.Details,
                    a.CreatedAt,
                    heuresDepuisCreation = (int)Math.Floor((DateTime.UtcNow - a.CreatedAt).TotalHours)
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                count = alertes.Count,
                alertes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des alertes actives");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer toutes les alertes avec pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] string? severite = null,
        [FromQuery] string? type = null,
        [FromQuery] bool? acquittee = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = _context.AlertesSysteme.AsQueryable();

            if (!string.IsNullOrEmpty(severite))
                query = query.Where(a => a.Severite == severite);

            if (!string.IsNullOrEmpty(type))
                query = query.Where(a => a.TypeAlerte == type);

            if (acquittee.HasValue)
                query = query.Where(a => a.Acquittee == acquittee.Value);

            var totalCount = await query.CountAsync();

            var alertes = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                totalCount,
                page,
                pageSize,
                alertes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des alertes");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Acquitter une alerte
    /// </summary>
    [HttpPost("{id}/acquitter")]
    public async Task<IActionResult> AcknowledgeAlert(long id)
    {
        try
        {
            var alerte = await _context.AlertesSysteme.FindAsync(id);
            if (alerte == null)
            {
                return NotFound(new { success = false, message = "Alerte non trouvée" });
            }

            var userId = GetCurrentUserId();

            alerte.Acquittee = true;
            alerte.AcquitteePar = userId;
            alerte.DateAcquittement = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Alerte {Id} acquittée par utilisateur {UserId}", id, userId);

            return Ok(new { success = true, message = "Alerte acquittée" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'acquittement de l'alerte {Id}", id);
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Créer une alerte manuellement
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAlert([FromBody] CreateAlertRequest request)
    {
        try
        {
            var alerte = new AlerteSysteme
            {
                TypeAlerte = request.TypeAlerte,
                Message = request.Message,
                Severite = request.Severite ?? "warning",
                Source = request.Source ?? "manual",
                Details = request.Details,
                CreatedAt = DateTime.UtcNow
            };

            _context.AlertesSysteme.Add(alerte);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Alerte créée: {Type} - {Message}", request.TypeAlerte, request.Message);

            return Ok(new { success = true, idAlerte = alerte.IdAlerte });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de l'alerte");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Statistiques des alertes
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var stats = new
            {
                totalActives = await _context.AlertesSysteme.CountAsync(a => !a.Acquittee),
                emergency = await _context.AlertesSysteme.CountAsync(a => !a.Acquittee && a.Severite == "emergency"),
                critical = await _context.AlertesSysteme.CountAsync(a => !a.Acquittee && a.Severite == "critical"),
                warning = await _context.AlertesSysteme.CountAsync(a => !a.Acquittee && a.Severite == "warning"),
                info = await _context.AlertesSysteme.CountAsync(a => !a.Acquittee && a.Severite == "info"),
                totalAujourdhui = await _context.AlertesSysteme.CountAsync(a => a.CreatedAt.Date == DateTime.UtcNow.Date),
                parType = await _context.AlertesSysteme
                    .Where(a => !a.Acquittee)
                    .GroupBy(a => a.TypeAlerte)
                    .Select(g => new { type = g.Key, count = g.Count() })
                    .ToListAsync()
            };

            return Ok(new { success = true, stats });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des statistiques d'alertes");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

public class CreateAlertRequest
{
    public string TypeAlerte { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Severite { get; set; }
    public string? Source { get; set; }
    public string? Details { get; set; }
}
