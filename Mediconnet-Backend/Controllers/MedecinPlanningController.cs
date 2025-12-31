using Mediconnet_Backend.Controllers.Base;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.DTOs.Planning;
using Microsoft.AspNetCore.Mvc;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// Contrôleur pour la gestion du planning médecin
/// </summary>
[Route("api/medecin/planning")]
public class MedecinPlanningController : BaseApiController
{
    private readonly IMedecinPlanningService _planningService;
    private readonly ILogger<MedecinPlanningController> _logger;

    public MedecinPlanningController(
        IMedecinPlanningService planningService,
        ILogger<MedecinPlanningController> logger)
    {
        _planningService = planningService;
        _logger = logger;
    }

    // ==================== DASHBOARD ====================

    /// <summary>
    /// Tableau de bord du planning médecin
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var dashboard = await _planningService.GetDashboardAsync(medecinId.Value);
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetDashboard: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    // ==================== CRÉNEAUX HORAIRES ====================

    /// <summary>
    /// Récupérer la semaine type du médecin (modèle récurrent)
    /// </summary>
    [HttpGet("semaine-type")]
    public async Task<IActionResult> GetSemaineType()
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var semaine = await _planningService.GetSemaineTypeAsync(medecinId.Value);
            return Ok(semaine);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetSemaineType: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer le planning d'une semaine spécifique avec dates
    /// </summary>
    [HttpGet("semaine")]
    public async Task<IActionResult> GetSemainePlanning([FromQuery] DateTime? date)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var dateDebut = date ?? DateTime.UtcNow;
            var semaine = await _planningService.GetSemainePlanningAsync(medecinId.Value, dateDebut);
            return Ok(semaine);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetSemainePlanning: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer les créneaux d'un jour
    /// </summary>
    [HttpGet("creneaux/{jourSemaine}")]
    public async Task<IActionResult> GetCreneauxJour(int jourSemaine)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            if (jourSemaine < 1 || jourSemaine > 7)
                return BadRequest(new { message = "Jour invalide (1-7)" });

            var creneaux = await _planningService.GetCreneauxJourAsync(medecinId.Value, jourSemaine);
            return Ok(creneaux);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetCreneauxJour: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Créer un créneau horaire
    /// </summary>
    [HttpPost("creneaux")]
    public async Task<IActionResult> CreateCreneau([FromBody] CreateCreneauRequest request)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var (success, message, creneau) = await _planningService.CreateCreneauAsync(medecinId.Value, request);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message, creneau });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur CreateCreneau: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Modifier un créneau horaire
    /// </summary>
    [HttpPut("creneaux/{id}")]
    public async Task<IActionResult> UpdateCreneau(int id, [FromBody] CreateCreneauRequest request)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var (success, message) = await _planningService.UpdateCreneauAsync(medecinId.Value, id, request);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur UpdateCreneau: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Supprimer un créneau horaire
    /// </summary>
    [HttpDelete("creneaux/{id}")]
    public async Task<IActionResult> DeleteCreneau(int id)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var (success, message) = await _planningService.DeleteCreneauAsync(medecinId.Value, id);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur DeleteCreneau: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Activer/Désactiver un créneau
    /// </summary>
    [HttpPatch("creneaux/{id}/toggle")]
    public async Task<IActionResult> ToggleCreneau(int id)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var (success, message) = await _planningService.ToggleCreneauAsync(medecinId.Value, id);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur ToggleCreneau: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    // ==================== INDISPONIBILITÉS ====================

    /// <summary>
    /// Liste des indisponibilités
    /// </summary>
    [HttpGet("indisponibilites")]
    public async Task<IActionResult> GetIndisponibilites(
        [FromQuery] DateTime? dateDebut,
        [FromQuery] DateTime? dateFin)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var indispos = await _planningService.GetIndisponibilitesAsync(medecinId.Value, dateDebut, dateFin);
            return Ok(indispos);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetIndisponibilites: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Créer une indisponibilité (congé, absence)
    /// </summary>
    [HttpPost("indisponibilites")]
    public async Task<IActionResult> CreateIndisponibilite([FromBody] CreateIndisponibiliteRequest request)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var (success, message, indispo) = await _planningService.CreateIndisponibiliteAsync(medecinId.Value, request);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message, indisponibilite = indispo });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur CreateIndisponibilite: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Supprimer une indisponibilité
    /// </summary>
    [HttpDelete("indisponibilites/{id}")]
    public async Task<IActionResult> DeleteIndisponibilite(int id)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var (success, message) = await _planningService.DeleteIndisponibiliteAsync(medecinId.Value, id);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur DeleteIndisponibilite: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    // ==================== CALENDRIER ====================

    /// <summary>
    /// Calendrier de la semaine
    /// </summary>
    [HttpGet("calendrier/semaine")]
    public async Task<IActionResult> GetCalendrierSemaine([FromQuery] DateTime? date)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var dateDebut = date ?? DateTime.UtcNow;
            var calendrier = await _planningService.GetCalendrierSemaineAsync(medecinId.Value, dateDebut);
            return Ok(calendrier);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetCalendrierSemaine: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Calendrier d'un jour
    /// </summary>
    [HttpGet("calendrier/jour")]
    public async Task<IActionResult> GetCalendrierJour([FromQuery] DateTime date)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var calendrier = await _planningService.GetCalendrierJourAsync(medecinId.Value, date);
            return Ok(calendrier);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetCalendrierJour: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }
}
