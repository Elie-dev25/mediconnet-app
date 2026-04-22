using Mediconnet_Backend.DTOs;
using Mediconnet_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mediconnet_Backend.Controllers;

[ApiController]
[Route("api/standard-chambre")]
[Authorize]
public class StandardChambreController : ControllerBase
{
    private readonly IStandardChambreService _service;
    private readonly ILogger<StandardChambreController> _logger;

    public StandardChambreController(IStandardChambreService service, ILogger<StandardChambreController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Récupère tous les standards de chambre (Admin)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "administrateur")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var standards = await _service.GetAllAsync();
            return Ok(new { success = true, data = standards, count = standards.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur GetAll");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupère les standards actifs pour sélection (Médecin)
    /// </summary>
    [HttpGet("select")]
    [Authorize(Roles = "administrateur,medecin")]
    public async Task<IActionResult> GetForSelect()
    {
        try
        {
            var standards = await _service.GetForSelectAsync();
            return Ok(new { success = true, data = standards });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur GetForSelect");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupère un standard par son ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "administrateur")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var standard = await _service.GetByIdAsync(id);
            if (standard == null)
                return NotFound(new { success = false, message = "Standard non trouvé" });

            return Ok(new { success = true, data = standard });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur GetById");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Crée un nouveau standard de chambre (Admin)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "administrateur")]
    public async Task<IActionResult> Create([FromBody] CreateStandardChambreRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Nom))
                return BadRequest(new { success = false, message = "Le nom est obligatoire" });

            if (request.PrixJournalier <= 0)
                return BadRequest(new { success = false, message = "Le prix doit être supérieur à 0" });

            var standard = await _service.CreateAsync(request);
            return Ok(new { success = true, data = standard, message = "Standard créé avec succès" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur Create");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Met à jour un standard de chambre (Admin)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "administrateur")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateStandardChambreRequest request)
    {
        try
        {
            var standard = await _service.UpdateAsync(id, request);
            if (standard == null)
                return NotFound(new { success = false, message = "Standard non trouvé" });

            return Ok(new { success = true, data = standard, message = "Standard mis à jour" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur Update");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Supprime un standard de chambre (Admin)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "administrateur")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound(new { success = false, message = "Standard non trouvé" });

            return Ok(new { success = true, message = "Standard supprimé" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur Delete");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }
}
