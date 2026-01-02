using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mediconnet_Backend.Core.Interfaces.Services;

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
    /// Vérifie les interactions médicamenteuses entre plusieurs médicaments
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
            _logger.LogError(ex, "Erreur vérification interactions");
            return StatusCode(500, new { message = "Erreur lors de la vérification des interactions" });
        }
    }

    /// <summary>
    /// Vérifie les interactions avec le traitement en cours du patient
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
            _logger.LogError(ex, "Erreur vérification interaction traitement");
            return StatusCode(500, new { message = "Erreur lors de la vérification" });
        }
    }

    /// <summary>
    /// Vérifie les allergies du patient pour un médicament
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
            _logger.LogError(ex, "Erreur vérification allergies");
            return StatusCode(500, new { message = "Erreur lors de la vérification des allergies" });
        }
    }

    /// <summary>
    /// Récupère les allergies d'un patient
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
            _logger.LogError(ex, "Erreur récupération allergies");
            return StatusCode(500, new { message = "Erreur lors de la récupération des allergies" });
        }
    }

    /// <summary>
    /// Ajoute une allergie à un patient
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
            return result ? Ok(new { message = "Allergie supprimée" }) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur suppression allergie");
            return StatusCode(500, new { message = "Erreur lors de la suppression" });
        }
    }

    /// <summary>
    /// Vérifie les contre-indications pour un médicament
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
            _logger.LogError(ex, "Erreur vérification contre-indications");
            return StatusCode(500, new { message = "Erreur lors de la vérification" });
        }
    }

    /// <summary>
    /// Valide une prescription complète
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
    /// Récupère l'historique des alertes d'un patient
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
            _logger.LogError(ex, "Erreur récupération historique alertes");
            return StatusCode(500, new { message = "Erreur lors de la récupération" });
        }
    }
}

public class CheckInteractionsRequest
{
    public int IdPatient { get; set; }
    public List<int> MedicamentIds { get; set; } = new();
}

public class ValidatePrescriptionRequest
{
    public int IdPatient { get; set; }
    public List<PrescriptionItemRequest> Items { get; set; } = new();
}
