using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mediconnet_Backend.Core.Interfaces.Services;

namespace Mediconnet_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DMPController : ControllerBase
{
    private readonly IDMPService _dmpService;
    private readonly ILogger<DMPController> _logger;

    public DMPController(IDMPService dmpService, ILogger<DMPController> logger)
    {
        _dmpService = dmpService;
        _logger = logger;
    }

    /// <summary>
    /// Récupérer le DMP d'un patient
    /// </summary>
    [HttpGet("patient/{idPatient}")]
    public async Task<IActionResult> GetDMPPatient(int idPatient)
    {
        try
        {
            var dmp = await _dmpService.GetDMPPatientAsync(idPatient);
            return dmp != null ? Ok(dmp) : NotFound(new { message = "DMP non trouvé" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération DMP");
            return StatusCode(500, new { message = "Erreur lors de la récupération du DMP" });
        }
    }

    /// <summary>
    /// Créer un DMP pour un patient
    /// </summary>
    [HttpPost("patient/{idPatient}")]
    public async Task<IActionResult> CreerDMP(int idPatient, [FromBody] CreateDMPRequest request)
    {
        try
        {
            var result = await _dmpService.CreerDMPAsync(idPatient, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur création DMP");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Activer un DMP
    /// </summary>
    [HttpPost("patient/{idPatient}/activer")]
    public async Task<IActionResult> ActiverDMP(int idPatient)
    {
        try
        {
            var result = await _dmpService.ActivateDMPAsync(idPatient);
            return result ? Ok(new { message = "DMP activé" }) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur activation DMP");
            return StatusCode(500, new { message = "Erreur lors de l'activation" });
        }
    }

    /// <summary>
    /// Désactiver un DMP
    /// </summary>
    [HttpPost("patient/{idPatient}/desactiver")]
    public async Task<IActionResult> DesactiverDMP(int idPatient, [FromBody] DesactiverDMPRequest request)
    {
        try
        {
            var result = await _dmpService.DesactiverDMPAsync(idPatient, request.Motif);
            return result ? Ok(new { message = "DMP désactivé" }) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur désactivation DMP");
            return StatusCode(500, new { message = "Erreur lors de la désactivation" });
        }
    }

    /// <summary>
    /// Synchroniser avec le DMP national
    /// </summary>
    [HttpPost("patient/{idPatient}/sync")]
    public async Task<IActionResult> SynchroniserDMP(int idPatient)
    {
        try
        {
            var result = await _dmpService.SynchroniserAvecDMPNationalAsync(idPatient);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur synchronisation DMP");
            return StatusCode(500, new { message = "Erreur lors de la synchronisation" });
        }
    }

    /// <summary>
    /// Exporter vers le DMP national
    /// </summary>
    [HttpPost("patient/{idPatient}/export")]
    public async Task<IActionResult> ExporterDMP(int idPatient, [FromBody] ExportDMPRequest request)
    {
        try
        {
            var result = await _dmpService.ExporterVersDMPNationalAsync(idPatient, request.DocumentIds);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur export DMP");
            return StatusCode(500, new { message = "Erreur lors de l'export" });
        }
    }

    /// <summary>
    /// Importer depuis le DMP national
    /// </summary>
    [HttpPost("patient/{idPatient}/import")]
    public async Task<IActionResult> ImporterDMP(int idPatient)
    {
        try
        {
            var result = await _dmpService.ImporterDepuisDMPNationalAsync(idPatient);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur import DMP");
            return StatusCode(500, new { message = "Erreur lors de l'import" });
        }
    }

    /// <summary>
    /// Documents du patient
    /// </summary>
    [HttpGet("patient/{idPatient}/documents")]
    public async Task<IActionResult> GetDocuments(int idPatient, [FromQuery] string? typeDocument = null)
    {
        try
        {
            var documents = await _dmpService.GetDocumentsPatientAsync(idPatient, typeDocument);
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération documents");
            return StatusCode(500, new { message = "Erreur lors de la récupération" });
        }
    }

    /// <summary>
    /// Ajouter un document
    /// </summary>
    [HttpPost("patient/{idPatient}/documents")]
    public async Task<IActionResult> AjouterDocument(int idPatient, [FromBody] AjoutDocumentDMPRequest request)
    {
        try
        {
            var document = await _dmpService.AjouterDocumentAsync(idPatient, request);
            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur ajout document");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Télécharger un document
    /// </summary>
    [HttpGet("documents/{idDocument}/download")]
    public async Task<IActionResult> TelechargerDocument(int idDocument)
    {
        try
        {
            var content = await _dmpService.TelechargerDocumentAsync(idDocument);
            return File(content, "application/octet-stream");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur téléchargement document");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Supprimer un document
    /// </summary>
    [HttpDelete("documents/{idDocument}")]
    public async Task<IActionResult> SupprimerDocument(int idDocument, [FromQuery] string motif)
    {
        try
        {
            var result = await _dmpService.SupprimerDocumentAsync(idDocument, motif);
            return result ? Ok(new { message = "Document supprimé" }) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur suppression document");
            return StatusCode(500, new { message = "Erreur lors de la suppression" });
        }
    }

    /// <summary>
    /// Historique des accès
    /// </summary>
    [HttpGet("patient/{idPatient}/acces")]
    public async Task<IActionResult> GetHistoriqueAcces(int idPatient)
    {
        try
        {
            var acces = await _dmpService.GetHistoriqueAccesAsync(idPatient);
            return Ok(acces);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur historique accès");
            return StatusCode(500, new { message = "Erreur lors de la récupération" });
        }
    }

    /// <summary>
    /// Accorder un accès
    /// </summary>
    [HttpPost("patient/{idPatient}/autorisations")]
    public async Task<IActionResult> AccorderAcces(int idPatient, [FromBody] AccorderAccesRequest request)
    {
        try
        {
            var result = await _dmpService.AccorderAccesAsync(idPatient, request);
            return result ? Ok(new { message = "Accès accordé" }) : BadRequest(new { message = "Impossible d'accorder l'accès" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur accord accès");
            return StatusCode(500, new { message = "Erreur lors de l'accord" });
        }
    }

    /// <summary>
    /// Révoquer un accès
    /// </summary>
    [HttpDelete("patient/{idPatient}/autorisations/{idProfessionnel}")]
    public async Task<IActionResult> RevoquerAcces(int idPatient, int idProfessionnel)
    {
        try
        {
            var result = await _dmpService.RevoquerAccesAsync(idPatient, idProfessionnel);
            return result ? Ok(new { message = "Accès révoqué" }) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur révocation accès");
            return StatusCode(500, new { message = "Erreur lors de la révocation" });
        }
    }

    /// <summary>
    /// Autorisations actives
    /// </summary>
    [HttpGet("patient/{idPatient}/autorisations")]
    public async Task<IActionResult> GetAutorisations(int idPatient)
    {
        try
        {
            var autorisations = await _dmpService.GetAutorisationsAsync(idPatient);
            return Ok(autorisations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération autorisations");
            return StatusCode(500, new { message = "Erreur lors de la récupération" });
        }
    }

    /// <summary>
    /// Export FHIR Patient
    /// </summary>
    [HttpGet("patient/{idPatient}/fhir")]
    public async Task<IActionResult> ExportFHIRPatient(int idPatient)
    {
        try
        {
            var fhir = await _dmpService.ExportFHIRPatientAsync(idPatient);
            return Content(fhir, "application/fhir+json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur export FHIR");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Export FHIR Document
    /// </summary>
    [HttpGet("documents/{idDocument}/fhir")]
    public async Task<IActionResult> ExportFHIRDocument(int idDocument)
    {
        try
        {
            var fhir = await _dmpService.ExportFHIRDocumentAsync(idDocument);
            return Content(fhir, "application/fhir+json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur export FHIR document");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Import FHIR Bundle
    /// </summary>
    [HttpPost("patient/{idPatient}/fhir")]
    public async Task<IActionResult> ImportFHIRBundle(int idPatient, [FromBody] string fhirBundle)
    {
        try
        {
            var result = await _dmpService.ImportFHIRBundleAsync(fhirBundle, idPatient);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur import FHIR");
            return StatusCode(500, new { message = "Erreur lors de l'import" });
        }
    }
}

public class DesactiverDMPRequest
{
    public string Motif { get; set; } = string.Empty;
}

public class ExportDMPRequest
{
    public List<string> DocumentIds { get; set; } = new();
}
