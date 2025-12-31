using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mediconnet_Backend.Services;
using Mediconnet_Backend.DTOs.Hospitalisation;
using System.Security.Claims;

namespace Mediconnet_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HospitalisationController : ControllerBase
{
    private readonly IHospitalisationService _hospitalisationService;
    private readonly ILogger<HospitalisationController> _logger;

    public HospitalisationController(
        IHospitalisationService hospitalisationService,
        ILogger<HospitalisationController> logger)
    {
        _hospitalisationService = hospitalisationService;
        _logger = logger;
    }

    /// <summary>
    /// Récupérer toutes les chambres avec leurs lits
    /// </summary>
    [HttpGet("chambres")]
    public async Task<ActionResult<ChambresResponse>> GetChambres()
    {
        try
        {
            var result = await _hospitalisationService.GetChambresAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des chambres");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer les lits disponibles
    /// </summary>
    [HttpGet("lits/disponibles")]
    public async Task<ActionResult<LitsDisponiblesResponse>> GetLitsDisponibles()
    {
        try
        {
            var result = await _hospitalisationService.GetLitsDisponiblesAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des lits disponibles");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer toutes les hospitalisations avec filtres optionnels
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<HospitalisationDto>>> GetHospitalisations(
        [FromQuery] string? statut,
        [FromQuery] int? idPatient,
        [FromQuery] DateTime? dateDebut,
        [FromQuery] DateTime? dateFin)
    {
        try
        {
            var filtre = new FiltreHospitalisationRequest
            {
                Statut = statut,
                IdPatient = idPatient,
                DateDebut = dateDebut,
                DateFin = dateFin
            };

            var result = await _hospitalisationService.GetHospitalisationsAsync(filtre);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des hospitalisations");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer une hospitalisation par son ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<HospitalisationDto>> GetHospitalisation(int id)
    {
        try
        {
            var result = await _hospitalisationService.GetHospitalisationByIdAsync(id);
            if (result == null)
            {
                return NotFound(new { message = "Hospitalisation non trouvée" });
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de l'hospitalisation {Id}", id);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer l'historique des hospitalisations d'un patient
    /// </summary>
    [HttpGet("patient/{idPatient}")]
    public async Task<ActionResult<List<HospitalisationDto>>> GetHospitalisationsPatient(int idPatient)
    {
        try
        {
            var result = await _hospitalisationService.GetHospitalisationsPatientAsync(idPatient);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des hospitalisations du patient {IdPatient}", idPatient);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Créer une nouvelle hospitalisation
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<HospitalisationResponse>> CreerHospitalisation([FromBody] CreerHospitalisationRequest request)
    {
        try
        {
            var result = await _hospitalisationService.CreerHospitalisationAsync(request);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de l'hospitalisation");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Demander une hospitalisation depuis une consultation (médecin)
    /// </summary>
    [HttpPost("demande")]
    [Authorize(Roles = "medecin")]
    public async Task<ActionResult<HospitalisationResponse>> DemanderHospitalisation([FromBody] DemandeHospitalisationRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var medecinId))
            {
                return Unauthorized(new { message = "Utilisateur non authentifié" });
            }

            var result = await _hospitalisationService.DemanderHospitalisationAsync(request, medecinId);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la demande d'hospitalisation");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Terminer une hospitalisation
    /// </summary>
    [HttpPost("{id}/terminer")]
    public async Task<ActionResult<HospitalisationResponse>> TerminerHospitalisation(int id, [FromBody] TerminerHospitalisationRequest request)
    {
        try
        {
            request.IdAdmission = id;
            var result = await _hospitalisationService.TerminerHospitalisationAsync(request);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la terminaison de l'hospitalisation {Id}", id);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }
}
