using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mediconnet_Backend.Core.Interfaces.Services;
using System.Security.Claims;

namespace Mediconnet_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PrescriptionElectroniqueController : ControllerBase
{
    private readonly IPrescriptionElectroniqueService _prescriptionService;
    private readonly ILogger<PrescriptionElectroniqueController> _logger;

    public PrescriptionElectroniqueController(
        IPrescriptionElectroniqueService prescriptionService,
        ILogger<PrescriptionElectroniqueController> logger)
    {
        _prescriptionService = prescriptionService;
        _logger = logger;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Créer une ordonnance électronique
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Medecin")]
    public async Task<IActionResult> CreerOrdonnance([FromBody] CreateOrdonnanceElectroniqueRequest request)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var ordonnance = await _prescriptionService.CreerOrdonnanceElectroniqueAsync(request, medecinId.Value);
            return Ok(ordonnance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur création ordonnance électronique");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Récupérer une ordonnance par ID
    /// </summary>
    [HttpGet("{idOrdonnance}")]
    public async Task<IActionResult> GetOrdonnance(int idOrdonnance)
    {
        try
        {
            var ordonnance = await _prescriptionService.GetOrdonnanceAsync(idOrdonnance);
            return ordonnance != null ? Ok(ordonnance) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération ordonnance");
            return StatusCode(500, new { message = "Erreur lors de la récupération" });
        }
    }

    /// <summary>
    /// Récupérer une ordonnance par code unique
    /// </summary>
    [HttpGet("code/{codeUnique}")]
    public async Task<IActionResult> GetOrdonnanceByCode(string codeUnique)
    {
        try
        {
            var ordonnance = await _prescriptionService.GetOrdonnanceByCodeAsync(codeUnique);
            return ordonnance != null ? Ok(ordonnance) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération ordonnance par code");
            return StatusCode(500, new { message = "Erreur lors de la récupération" });
        }
    }

    /// <summary>
    /// Ordonnances d'un patient
    /// </summary>
    [HttpGet("patient/{idPatient}")]
    public async Task<IActionResult> GetOrdonnancesPatient(int idPatient)
    {
        try
        {
            var ordonnances = await _prescriptionService.GetOrdonnancesPatientAsync(idPatient);
            return Ok(ordonnances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération ordonnances patient");
            return StatusCode(500, new { message = "Erreur lors de la récupération" });
        }
    }

    /// <summary>
    /// Transmettre une ordonnance à une pharmacie
    /// </summary>
    [HttpPost("{idOrdonnance}/transmettre/{idPharmacie}")]
    public async Task<IActionResult> TransmettreOrdonnance(int idOrdonnance, int idPharmacie)
    {
        try
        {
            var result = await _prescriptionService.TransmettreAPharmacieAsync(idOrdonnance, idPharmacie);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur transmission ordonnance");
            return StatusCode(500, new { message = "Erreur lors de la transmission" });
        }
    }

    /// <summary>
    /// Annuler une transmission
    /// </summary>
    [HttpDelete("{idOrdonnance}/transmission")]
    public async Task<IActionResult> AnnulerTransmission(int idOrdonnance)
    {
        try
        {
            var result = await _prescriptionService.AnnulerTransmissionAsync(idOrdonnance);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur annulation transmission");
            return StatusCode(500, new { message = "Erreur lors de l'annulation" });
        }
    }

    /// <summary>
    /// Liste des pharmacies partenaires
    /// </summary>
    [HttpGet("pharmacies")]
    public async Task<IActionResult> GetPharmacies([FromQuery] string? ville = null)
    {
        try
        {
            var pharmacies = await _prescriptionService.GetPharmaciesPartenairesAsync(ville);
            return Ok(pharmacies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération pharmacies");
            return StatusCode(500, new { message = "Erreur lors de la récupération" });
        }
    }

    /// <summary>
    /// Marquer une ordonnance comme dispensée
    /// </summary>
    [HttpPost("{idOrdonnance}/dispenser")]
    public async Task<IActionResult> MarquerDispensee(int idOrdonnance, [FromBody] DispensationExterneRequest request)
    {
        try
        {
            var result = await _prescriptionService.MarquerDispenseeAsync(idOrdonnance, request);
            return result ? Ok(new { message = "Ordonnance dispensée" }) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur marquage dispensation");
            return StatusCode(500, new { message = "Erreur lors du marquage" });
        }
    }

    /// <summary>
    /// Statut de dispensation
    /// </summary>
    [HttpGet("{idOrdonnance}/statut")]
    public async Task<IActionResult> GetStatutDispensation(int idOrdonnance)
    {
        try
        {
            var statut = await _prescriptionService.GetStatutDispensationAsync(idOrdonnance);
            return Ok(statut);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération statut");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Générer QR Code
    /// </summary>
    [HttpGet("{idOrdonnance}/qrcode")]
    public async Task<IActionResult> GetQRCode(int idOrdonnance)
    {
        try
        {
            var qrData = await _prescriptionService.GenerateQRCodeAsync(idOrdonnance);
            return File(qrData, "text/plain");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur génération QR code");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Scanner une ordonnance
    /// </summary>
    [HttpGet("scan/{codeScanne}")]
    public async Task<IActionResult> ScanOrdonnance(string codeScanne)
    {
        try
        {
            var ordonnance = await _prescriptionService.ScanOrdonnanceAsync(codeScanne);
            return ordonnance != null ? Ok(ordonnance) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur scan ordonnance");
            return StatusCode(500, new { message = "Erreur lors du scan" });
        }
    }

    /// <summary>
    /// Renouveler une ordonnance
    /// </summary>
    [HttpPost("{idOrdonnance}/renouveler")]
    [Authorize(Roles = "Medecin")]
    public async Task<IActionResult> RenouvelerOrdonnance(int idOrdonnance)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var ordonnance = await _prescriptionService.RenouvelerOrdonnanceAsync(idOrdonnance, medecinId.Value);
            return Ok(ordonnance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur renouvellement ordonnance");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Ordonnances à renouveler
    /// </summary>
    [HttpGet("a-renouveler")]
    [Authorize(Roles = "Medecin")]
    public async Task<IActionResult> GetOrdonnancesARenouveler()
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var ordonnances = await _prescriptionService.GetOrdonnancesARenouvelerAsync(medecinId.Value);
            return Ok(ordonnances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération ordonnances à renouveler");
            return StatusCode(500, new { message = "Erreur lors de la récupération" });
        }
    }
}
