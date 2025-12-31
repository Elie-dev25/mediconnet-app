using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mediconnet_Backend.Services;
using Mediconnet_Backend.DTOs.Admin;

namespace Mediconnet_Backend.Controllers;

[ApiController]
[Route("api/admin/settings")]
[Authorize(Roles = "administrateur")]
public class AdminSettingsController : ControllerBase
{
    private readonly IChambreService _chambreService;
    private readonly ILogger<AdminSettingsController> _logger;

    public AdminSettingsController(
        IChambreService chambreService,
        ILogger<AdminSettingsController> logger)
    {
        _chambreService = chambreService;
        _logger = logger;
    }

    // ==================== CHAMBRES ====================

    /// <summary>
    /// Récupérer toutes les chambres avec leurs lits
    /// </summary>
    [HttpGet("chambres")]
    public async Task<ActionResult<ChambresListResponse>> GetChambres()
    {
        try
        {
            var result = await _chambreService.GetAllChambresAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des chambres");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer une chambre par son ID
    /// </summary>
    [HttpGet("chambres/{id}")]
    public async Task<ActionResult<ChambreAdminDto>> GetChambre(int id)
    {
        try
        {
            var chambre = await _chambreService.GetChambreByIdAsync(id);
            if (chambre == null)
            {
                return NotFound(new { message = "Chambre non trouvée" });
            }
            return Ok(chambre);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de la chambre {Id}", id);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Créer une nouvelle chambre
    /// </summary>
    [HttpPost("chambres")]
    public async Task<ActionResult<ChambreResponse>> CreateChambre([FromBody] CreateChambreRequest request)
    {
        try
        {
            var result = await _chambreService.CreateChambreAsync(request);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de la chambre");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Mettre à jour une chambre
    /// </summary>
    [HttpPut("chambres/{id}")]
    public async Task<ActionResult<ChambreResponse>> UpdateChambre(int id, [FromBody] UpdateChambreRequest request)
    {
        try
        {
            var result = await _chambreService.UpdateChambreAsync(id, request);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la mise à jour de la chambre {Id}", id);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Supprimer une chambre
    /// </summary>
    [HttpDelete("chambres/{id}")]
    public async Task<ActionResult<ChambreResponse>> DeleteChambre(int id)
    {
        try
        {
            var result = await _chambreService.DeleteChambreAsync(id);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la suppression de la chambre {Id}", id);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Statistiques des chambres et lits
    /// </summary>
    [HttpGet("chambres/stats")]
    public async Task<ActionResult<ChambresStats>> GetChambresStats()
    {
        try
        {
            var stats = await _chambreService.GetStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des statistiques");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    // ==================== LITS ====================

    /// <summary>
    /// Ajouter un lit à une chambre
    /// </summary>
    [HttpPost("chambres/{chambreId}/lits")]
    public async Task<ActionResult<LitResponse>> AddLit(int chambreId, [FromBody] CreateLitRequest request)
    {
        try
        {
            var result = await _chambreService.AddLitToChambreAsync(chambreId, request);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'ajout du lit à la chambre {ChambreId}", chambreId);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Mettre à jour un lit
    /// </summary>
    [HttpPut("lits/{id}")]
    public async Task<ActionResult<LitResponse>> UpdateLit(int id, [FromBody] UpdateLitRequest request)
    {
        try
        {
            var result = await _chambreService.UpdateLitAsync(id, request);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la mise à jour du lit {Id}", id);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Supprimer un lit
    /// </summary>
    [HttpDelete("lits/{id}")]
    public async Task<ActionResult<LitResponse>> DeleteLit(int id)
    {
        try
        {
            var result = await _chambreService.DeleteLitAsync(id);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la suppression du lit {Id}", id);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    // ==================== LABORATOIRES (Placeholder) ====================

    /// <summary>
    /// Récupérer tous les laboratoires (placeholder)
    /// </summary>
    [HttpGet("laboratoires")]
    public ActionResult<LaboratoiresListResponse> GetLaboratoires()
    {
        return Ok(new LaboratoiresListResponse
        {
            Success = true,
            Laboratoires = new List<LaboratoireDto>(),
            Total = 0
        });
    }
}
