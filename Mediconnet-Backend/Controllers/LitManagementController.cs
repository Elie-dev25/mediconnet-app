using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mediconnet_Backend.Core.Interfaces.Services;

namespace Mediconnet_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LitManagementController : ControllerBase
{
    private readonly ILitManagementService _litService;
    private readonly ILogger<LitManagementController> _logger;

    public LitManagementController(ILitManagementService litService, ILogger<LitManagementController> logger)
    {
        _litService = litService;
        _logger = logger;
    }

    /// <summary>
    /// Tableau de bord d'occupation des lits
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var dashboard = await _litService.GetOccupationDashboardAsync();
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur dashboard occupation");
            return StatusCode(500, new { message = "Erreur lors de la récupération du dashboard" });
        }
    }

    /// <summary>
    /// Occupation par chambre
    /// </summary>
    [HttpGet("occupation/chambres")]
    public async Task<IActionResult> GetOccupationParChambre()
    {
        try
        {
            var occupation = await _litService.GetOccupationParChambreAsync();
            return Ok(occupation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur occupation chambres");
            return StatusCode(500, new { message = "Erreur lors de la récupération" });
        }
    }

    /// <summary>
    /// Affectation automatique d'un lit
    /// </summary>
    [HttpPost("affectation/auto")]
    public async Task<IActionResult> AffecterAutomatique([FromBody] AffectationRequest request)
    {
        try
        {
            var result = await _litService.AffecterLitAutomatiqueAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur affectation automatique");
            return StatusCode(500, new { message = "Erreur lors de l'affectation" });
        }
    }

    /// <summary>
    /// Suggestions de lits pour un patient
    /// </summary>
    [HttpGet("suggestions/{idPatient}")]
    public async Task<IActionResult> GetSuggestions(int idPatient, [FromQuery] string? criteres = null)
    {
        try
        {
            var suggestions = await _litService.GetLitsSuggeresAsync(idPatient, criteres);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur suggestions lits");
            return StatusCode(500, new { message = "Erreur lors de la récupération des suggestions" });
        }
    }

    /// <summary>
    /// Réserver un lit
    /// </summary>
    [HttpPost("reservations")]
    public async Task<IActionResult> ReserverLit([FromBody] ReservationLitRequest request)
    {
        try
        {
            var reservation = await _litService.ReserverLitAsync(request);
            return Ok(reservation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur réservation lit");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Annuler une réservation
    /// </summary>
    [HttpDelete("reservations/{idReservation}")]
    public async Task<IActionResult> AnnulerReservation(int idReservation)
    {
        try
        {
            var result = await _litService.AnnulerReservationAsync(idReservation);
            return result ? Ok(new { message = "Réservation annulée" }) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur annulation réservation");
            return StatusCode(500, new { message = "Erreur lors de l'annulation" });
        }
    }

    /// <summary>
    /// Réservations en cours
    /// </summary>
    [HttpGet("reservations")]
    public async Task<IActionResult> GetReservations()
    {
        try
        {
            var reservations = await _litService.GetReservationsEnCoursAsync();
            return Ok(reservations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération réservations");
            return StatusCode(500, new { message = "Erreur lors de la récupération" });
        }
    }

    /// <summary>
    /// Transférer un patient
    /// </summary>
    [HttpPost("transferts")]
    public async Task<IActionResult> TransfererPatient([FromBody] TransfertRequest request)
    {
        try
        {
            var result = await _litService.TransfererPatientAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur transfert patient");
            return StatusCode(500, new { message = "Erreur lors du transfert" });
        }
    }

    /// <summary>
    /// Historique des transferts
    /// </summary>
    [HttpGet("transferts")]
    public async Task<IActionResult> GetHistoriqueTransferts([FromQuery] int? idPatient = null)
    {
        try
        {
            var transferts = await _litService.GetHistoriqueTransfertsAsync(idPatient);
            return Ok(transferts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur historique transferts");
            return StatusCode(500, new { message = "Erreur lors de la récupération" });
        }
    }

    /// <summary>
    /// Mettre un lit en maintenance
    /// </summary>
    [HttpPost("maintenance/{idLit}")]
    public async Task<IActionResult> MarquerMaintenance(int idLit, [FromBody] MaintenanceRequest request)
    {
        try
        {
            var result = await _litService.MarquerLitEnMaintenanceAsync(idLit, request.Motif);
            return result ? Ok(new { message = "Lit mis en maintenance" }) : BadRequest(new { message = "Impossible de mettre le lit en maintenance" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur mise en maintenance");
            return StatusCode(500, new { message = "Erreur lors de la mise en maintenance" });
        }
    }

    /// <summary>
    /// Libérer un lit de la maintenance
    /// </summary>
    [HttpDelete("maintenance/{idLit}")]
    public async Task<IActionResult> LibererMaintenance(int idLit)
    {
        try
        {
            var result = await _litService.LibererLitMaintenanceAsync(idLit);
            return result ? Ok(new { message = "Lit libéré" }) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur libération maintenance");
            return StatusCode(500, new { message = "Erreur lors de la libération" });
        }
    }

    /// <summary>
    /// Lits en maintenance
    /// </summary>
    [HttpGet("maintenance")]
    public async Task<IActionResult> GetLitsEnMaintenance()
    {
        try
        {
            var lits = await _litService.GetLitsEnMaintenanceAsync();
            return Ok(lits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération lits maintenance");
            return StatusCode(500, new { message = "Erreur lors de la récupération" });
        }
    }

    /// <summary>
    /// Statistiques d'occupation
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStatistiques([FromQuery] DateTime dateDebut, [FromQuery] DateTime dateFin)
    {
        try
        {
            var stats = await _litService.GetStatistiquesOccupationAsync(dateDebut, dateFin);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur statistiques occupation");
            return StatusCode(500, new { message = "Erreur lors de la récupération des statistiques" });
        }
    }
}

public class MaintenanceRequest
{
    public string Motif { get; set; } = string.Empty;
}
