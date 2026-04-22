using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mediconnet_Backend.Core.Interfaces.Services;
using System.Text.Json.Serialization;

namespace Mediconnet_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicalAlertController : ControllerBase
{
    private readonly IMedicalAlertService _alertService;
    private readonly ILogger<MedicalAlertController> _logger;

    public MedicalAlertController(IMedicalAlertService alertService, ILogger<MedicalAlertController> logger)
    {
        _alertService = alertService;
        _logger = logger;
    }

    /// <summary>
    /// VÃ©rifie les interactions mÃ©dicamenteuses entre plusieurs mÃ©dicaments
    /// </summary>
    [HttpPost("interactions/check")]
    public async Task<IActionResult> CheckInteractions([FromBody] CheckInteractionsRequest request)
    {
        try
        {
            var result = await _alertService.CheckInteractionsMedicamenteusesAsync(request.IdPatient, request.MedicamentIds);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur vÃ©rification interactions");
            return StatusCode(500, new { message = "Erreur lors de la vÃ©rification des interactions" });
        }
    }

    /// <summary>
    /// VÃ©rifie les interactions avec le traitement en cours du patient
    /// </summary>
    [HttpGet("interactions/traitement/{idPatient}/{idMedicament}")]
    public async Task<IActionResult> CheckInteractionTraitement(int idPatient, int idMedicament)
    {
        try
        {
            var result = await _alertService.CheckInteractionAvecTraitementEnCoursAsync(idPatient, idMedicament);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur vÃ©rification interaction traitement");
            return StatusCode(500, new { message = "Erreur lors de la vÃ©rification" });
        }
    }

    /// <summary>
    /// VÃ©rifie les allergies du patient pour un mÃ©dicament
    /// </summary>
    [HttpGet("allergies/check/{idPatient}/{idMedicament}")]
    public async Task<IActionResult> CheckAllergies(int idPatient, int idMedicament)
    {
        try
        {
            var result = await _alertService.CheckAllergiesAsync(idPatient, idMedicament);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur vÃ©rification allergies");
            return StatusCode(500, new { message = "Erreur lors de la vÃ©rification des allergies" });
        }
    }

    /// <summary>
    /// RÃ©cupÃ¨re les allergies d'un patient
    /// </summary>
    [HttpGet("allergies/{idPatient}")]
    public async Task<IActionResult> GetAllergiesPatient(int idPatient)
    {
        try
        {
            var allergies = await _alertService.GetAllergiesPatientAsync(idPatient);
            return Ok(allergies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur rÃ©cupÃ©ration allergies");
            return StatusCode(500, new { message = "Erreur lors de la rÃ©cupÃ©ration des allergies" });
        }
    }

    /// <summary>
    /// Ajoute une allergie Ã  un patient
    /// </summary>
    [HttpPost("allergies/{idPatient}")]
    public async Task<IActionResult> AjouterAllergie(int idPatient, [FromBody] CreateAllergieRequest request)
    {
        try
        {
            var allergie = await _alertService.AjouterAllergieAsync(idPatient, request);
            return Ok(allergie);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur ajout allergie");
            return StatusCode(500, new { message = "Erreur lors de l'ajout de l'allergie" });
        }
    }

    /// <summary>
    /// Supprime une allergie
    /// </summary>
    [HttpDelete("allergies/{idAllergie}")]
    public async Task<IActionResult> SupprimerAllergie(int idAllergie)
    {
        try
        {
            var result = await _alertService.SupprimerAllergieAsync(idAllergie);
            return result ? Ok(new { message = "Allergie supprimÃ©e" }) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur suppression allergie");
            return StatusCode(500, new { message = "Erreur lors de la suppression" });
        }
    }

    /// <summary>
    /// VÃ©rifie les contre-indications pour un mÃ©dicament
    /// </summary>
    [HttpGet("contre-indications/{idPatient}/{idMedicament}")]
    public async Task<IActionResult> CheckContreIndications(int idPatient, int idMedicament)
    {
        try
        {
            var result = await _alertService.CheckContreIndicationsAsync(idPatient, idMedicament);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur vÃ©rification contre-indications");
            return StatusCode(500, new { message = "Erreur lors de la vÃ©rification" });
        }
    }

    /// <summary>
    /// Valide une prescription complÃ¨te
    /// </summary>
    [HttpPost("prescription/validate")]
    public async Task<IActionResult> ValidatePrescription([FromBody] ValidatePrescriptionRequest request)
    {
        try
        {
            var result = await _alertService.ValidatePrescriptionAsync(request.IdPatient, request.Items);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur validation prescription");
            return StatusCode(500, new { message = "Erreur lors de la validation" });
        }
    }

    /// <summary>
    /// RÃ©cupÃ¨re l'historique des alertes d'un patient
    /// </summary>
    [HttpGet("historique/{idPatient}")]
    public async Task<IActionResult> GetHistoriqueAlertes(int idPatient)
    {
        try
        {
            var alertes = await _alertService.GetHistoriqueAlertesAsync(idPatient);
            return Ok(alertes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur rÃ©cupÃ©ration historique alertes");
            return StatusCode(500, new { message = "Erreur lors de la rÃ©cupÃ©ration" });
        }
    }
}

public class CheckInteractionsRequest
{
    [JsonRequired]
    public int IdPatient { get; set; }
    public List<int> MedicamentIds { get; set; } = new();
}

public class ValidatePrescriptionRequest
{
    [JsonRequired]
    public int IdPatient { get; set; }
    public List<PrescriptionItemRequest> Items { get; set; } = new();
}
