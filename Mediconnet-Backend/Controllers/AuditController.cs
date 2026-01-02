using Mediconnet_Backend.Controllers.Base;
using Mediconnet_Backend.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// Contrôleur pour la consultation des logs d'audit (Admin uniquement)
/// </summary>
[Route("api/admin/[controller]")]
[Authorize]
public class AuditController : BaseApiController
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditService auditService, ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Récupère les logs d'audit avec pagination et filtres
    /// </summary>
    [HttpGet("logs")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? action = null,
        [FromQuery] string? resourceType = null,
        [FromQuery] int? userId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] bool? successOnly = null)
    {
        try
        {
            // Vérifier que l'utilisateur est admin
            if (!IsAdmin())
                return Forbid();

            var result = await _auditService.GetAuditLogsPagedAsync(
                page, pageSize, action, resourceType, userId, dateFrom, dateTo, successOnly);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des logs d'audit");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupère les statistiques des logs d'audit
    /// </summary>
    [HttpGet("logs/stats")]
    public async Task<IActionResult> GetAuditStats([FromQuery] int days = 7)
    {
        try
        {
            if (!IsAdmin())
                return Forbid();

            var stats = await _auditService.GetAuditStatsAsync(days);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des statistiques d'audit");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupère les types d'actions disponibles pour le filtrage
    /// </summary>
    [HttpGet("logs/actions")]
    public async Task<IActionResult> GetAvailableActions()
    {
        try
        {
            if (!IsAdmin())
                return Forbid();

            var actions = await _auditService.GetDistinctActionsAsync();
            return Ok(actions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des actions");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupère les types de ressources disponibles pour le filtrage
    /// </summary>
    [HttpGet("logs/resources")]
    public async Task<IActionResult> GetAvailableResourceTypes()
    {
        try
        {
            if (!IsAdmin())
                return Forbid();

            var resources = await _auditService.GetDistinctResourceTypesAsync();
            return Ok(resources);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des types de ressources");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }
}
