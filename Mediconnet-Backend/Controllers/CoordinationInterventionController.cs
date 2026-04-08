using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mediconnet_Backend.Services;
using Mediconnet_Backend.DTOs.Chirurgie;
using System.Security.Claims;

namespace Mediconnet_Backend.Controllers;

[ApiController]
[Route("api/coordination-intervention")]
[Authorize]
public class CoordinationInterventionController : ControllerBase
{
    private readonly ICoordinationInterventionService _coordinationService;
    private readonly ILogger<CoordinationInterventionController> _logger;

    public CoordinationInterventionController(
        ICoordinationInterventionService coordinationService,
        ILogger<CoordinationInterventionController> logger)
    {
        _coordinationService = coordinationService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

    private string GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value 
            ?? User.FindFirst("role")?.Value 
            ?? "";
    }

    /// <summary>
    /// Récupérer la liste des anesthésistes avec leurs disponibilités
    /// </summary>
    [HttpGet("anesthesistes")]
    public async Task<IActionResult> GetAnesthesistesDisponibles(
        [FromQuery] DateTime dateDebut,
        [FromQuery] DateTime dateFin,
        [FromQuery] int dureeMinutes = 60)
    {
        try
        {
            var anesthesistes = await _coordinationService.GetAnesthesistesDisponiblesAsync(
                dateDebut, dateFin, dureeMinutes);
            return Ok(anesthesistes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des anesthésistes");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer les créneaux disponibles d'un anesthésiste
    /// </summary>
    [HttpGet("anesthesistes/{idAnesthesiste}/creneaux")]
    public async Task<IActionResult> GetCreneauxAnesthesiste(
        int idAnesthesiste,
        [FromQuery] DateTime dateDebut,
        [FromQuery] DateTime dateFin)
    {
        try
        {
            var creneaux = await _coordinationService.GetCreneauxAnesthesisteAsync(
                idAnesthesiste, dateDebut, dateFin);
            return Ok(creneaux);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des créneaux");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Proposer une coordination (chirurgien)
    /// </summary>
    [HttpPost("proposer")]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> ProposerCoordination([FromBody] ProposerCoordinationRequest request)
    {
        try
        {
            var idChirurgien = GetCurrentUserId();
            var result = await _coordinationService.ProposerCoordinationAsync(request, idChirurgien);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la proposition de coordination");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Valider une coordination (anesthésiste)
    /// </summary>
    [HttpPost("valider")]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> ValiderCoordination([FromBody] ValiderCoordinationRequest request)
    {
        try
        {
            var idAnesthesiste = GetCurrentUserId();
            var result = await _coordinationService.ValiderCoordinationAsync(request, idAnesthesiste);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la validation de coordination");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Modifier/contre-proposer une coordination (anesthésiste)
    /// </summary>
    [HttpPost("modifier")]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> ModifierCoordination([FromBody] ModifierCoordinationRequest request)
    {
        try
        {
            var idAnesthesiste = GetCurrentUserId();
            var result = await _coordinationService.ModifierCoordinationAsync(request, idAnesthesiste);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la modification de coordination");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Refuser une coordination (anesthésiste)
    /// </summary>
    [HttpPost("refuser")]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> RefuserCoordination([FromBody] RefuserCoordinationRequest request)
    {
        try
        {
            var idAnesthesiste = GetCurrentUserId();
            var result = await _coordinationService.RefuserCoordinationAsync(request, idAnesthesiste);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du refus de coordination");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Accepter une contre-proposition (chirurgien)
    /// </summary>
    [HttpPost("accepter-contre-proposition")]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> AccepterContreProposition([FromBody] AccepterContrePropositionRequest request)
    {
        try
        {
            var idChirurgien = GetCurrentUserId();
            var result = await _coordinationService.AccepterContrePropositionAsync(request, idChirurgien);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'acceptation de contre-proposition");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Refuser une contre-proposition (chirurgien)
    /// </summary>
    [HttpPost("refuser-contre-proposition")]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> RefuserContreProposition([FromBody] RefuserContrePropositionRequest request)
    {
        try
        {
            var idChirurgien = GetCurrentUserId();
            var result = await _coordinationService.RefuserContrePropositionAsync(request, idChirurgien);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du refus de contre-proposition");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Annuler une coordination
    /// </summary>
    [HttpPost("annuler")]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> AnnulerCoordination([FromBody] AnnulerCoordinationRequest request)
    {
        try
        {
            var idUser = GetCurrentUserId();
            var result = await _coordinationService.AnnulerCoordinationAsync(request, idUser);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'annulation de coordination");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer une coordination par ID
    /// </summary>
    [HttpGet("{idCoordination}")]
    public async Task<IActionResult> GetCoordination(int idCoordination)
    {
        try
        {
            var coordination = await _coordinationService.GetCoordinationAsync(idCoordination);

            if (coordination == null)
                return NotFound(new { message = "Coordination non trouvée" });

            // Vérifier que l'utilisateur est impliqué
            var userId = GetCurrentUserId();
            if (coordination.IdChirurgien != userId && coordination.IdAnesthesiste != userId)
                return Forbid();

            return Ok(coordination);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de coordination");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer les coordinations du chirurgien connecté
    /// </summary>
    [HttpGet("chirurgien/mes-coordinations")]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> GetMesCoordinationsChirurgien([FromQuery] CoordinationFilterDto? filter)
    {
        try
        {
            var idChirurgien = GetCurrentUserId();
            var coordinations = await _coordinationService.GetCoordinationsChirurgienAsync(idChirurgien, filter);
            return Ok(coordinations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des coordinations chirurgien");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer les coordinations de l'anesthésiste connecté
    /// </summary>
    [HttpGet("anesthesiste/mes-coordinations")]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> GetMesCoordinationsAnesthesiste([FromQuery] CoordinationFilterDto? filter)
    {
        try
        {
            var idAnesthesiste = GetCurrentUserId();
            var coordinations = await _coordinationService.GetCoordinationsAnesthesisteAsync(idAnesthesiste, filter);
            return Ok(coordinations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des coordinations anesthésiste");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer les demandes en attente pour l'anesthésiste
    /// </summary>
    [HttpGet("anesthesiste/demandes-en-attente")]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> GetDemandesEnAttente()
    {
        try
        {
            var idAnesthesiste = GetCurrentUserId();
            var filter = new CoordinationFilterDto { Statut = "proposee" };
            var coordinations = await _coordinationService.GetCoordinationsAnesthesisteAsync(idAnesthesiste, filter);
            return Ok(coordinations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des demandes en attente");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer l'historique d'une coordination
    /// </summary>
    [HttpGet("{idCoordination}/historique")]
    public async Task<IActionResult> GetHistorique(int idCoordination)
    {
        try
        {
            var historique = await _coordinationService.GetHistoriqueCoordinationAsync(idCoordination);
            return Ok(historique);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de l'historique");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer les statistiques de l'anesthésiste
    /// </summary>
    [HttpGet("anesthesiste/stats")]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> GetStatsAnesthesiste()
    {
        try
        {
            var idAnesthesiste = GetCurrentUserId();
            var stats = await _coordinationService.GetStatsAnesthesisteAsync(idAnesthesiste);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des statistiques");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }
}
