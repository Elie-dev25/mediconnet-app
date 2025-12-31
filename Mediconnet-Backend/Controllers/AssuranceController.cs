using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.DTOs.Assurance;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// Contrôleur pour la gestion des assurances
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssuranceController : ControllerBase
{
    private readonly IAssuranceService _assuranceService;
    private readonly ILogger<AssuranceController> _logger;

    public AssuranceController(IAssuranceService assuranceService, ILogger<AssuranceController> logger)
    {
        _assuranceService = assuranceService;
        _logger = logger;
    }

    // ==================== ASSURANCES ====================

    /// <summary>
    /// Récupérer la liste des assurances avec filtres
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAssurances([FromQuery] AssuranceFilterDto filter)
    {
        try
        {
            var result = await _assuranceService.GetAssurancesAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des assurances");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer les assurances actives (pour listes déroulantes)
    /// </summary>
    [HttpGet("actives")]
    public async Task<IActionResult> GetAssurancesActives()
    {
        try
        {
            var result = await _assuranceService.GetAssurancesActivesAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des assurances actives");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer une assurance par son ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAssurance(int id)
    {
        try
        {
            var result = await _assuranceService.GetAssuranceByIdAsync(id);
            if (result == null)
            {
                return NotFound(new { message = "Assurance non trouvée" });
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de l'assurance {Id}", id);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Créer une nouvelle assurance
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin,administrateur")]
    public async Task<IActionResult> CreateAssurance([FromBody] CreateAssuranceDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _assuranceService.CreateAssuranceAsync(dto);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetAssurance), new { id = result.Data?.IdAssurance }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de l'assurance");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Mettre à jour une assurance
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "admin,administrateur")]
    public async Task<IActionResult> UpdateAssurance(int id, [FromBody] UpdateAssuranceDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _assuranceService.UpdateAssuranceAsync(id, dto);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la mise à jour de l'assurance {Id}", id);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Activer/Désactiver une assurance
    /// </summary>
    [HttpPatch("{id}/toggle-status")]
    [Authorize(Roles = "admin,administrateur")]
    public async Task<IActionResult> ToggleAssuranceStatus(int id)
    {
        try
        {
            var result = await _assuranceService.ToggleAssuranceStatusAsync(id);
            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du changement de statut de l'assurance {Id}", id);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Supprimer une assurance
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,administrateur")]
    public async Task<IActionResult> DeleteAssurance(int id)
    {
        try
        {
            var result = await _assuranceService.DeleteAssuranceAsync(id);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la suppression de l'assurance {Id}", id);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    // ==================== PATIENT ASSURANCE ====================

    /// <summary>
    /// Récupérer l'assurance d'un patient
    /// </summary>
    [HttpGet("patient/{idPatient}")]
    public async Task<IActionResult> GetPatientAssurance(int idPatient)
    {
        try
        {
            var result = await _assuranceService.GetPatientAssuranceAsync(idPatient);
            if (result == null)
            {
                return Ok(new PatientAssuranceInfoDto { EstAssure = false });
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de l'assurance du patient {Id}", idPatient);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Mettre à jour l'assurance d'un patient
    /// </summary>
    [HttpPut("patient/{idPatient}")]
    public async Task<IActionResult> UpdatePatientAssurance(int idPatient, [FromBody] UpdatePatientAssuranceDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _assuranceService.UpdatePatientAssuranceAsync(idPatient, dto);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la mise à jour de l'assurance patient {Id}", idPatient);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Retirer l'assurance d'un patient
    /// </summary>
    [HttpDelete("patient/{idPatient}")]
    public async Task<IActionResult> RemovePatientAssurance(int idPatient)
    {
        try
        {
            var result = await _assuranceService.RemovePatientAssuranceAsync(idPatient);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du retrait de l'assurance patient {Id}", idPatient);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }
}
