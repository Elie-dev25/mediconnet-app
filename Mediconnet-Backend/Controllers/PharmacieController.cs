using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mediconnet_Backend.Controllers.Base;
using Mediconnet_Backend.Services;
using Mediconnet_Backend.DTOs.Pharmacie;

namespace Mediconnet_Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/pharmacie")]
public class PharmacieController : BaseApiController
{
    private readonly IPharmacieStockService _stockService;
    private readonly ILogger<PharmacieController> _logger;

    public PharmacieController(IPharmacieStockService stockService, ILogger<PharmacieController> logger)
    {
        _stockService = stockService;
        _logger = logger;
    }

    // ==================== KPIs & Dashboard ====================

    [HttpGet("kpis")]
    public async Task<ActionResult<PharmacieKpiDto>> GetKpis()
    {
        try
        {
            var kpis = await _stockService.GetKpisAsync();
            return Ok(kpis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération KPIs pharmacie");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpGet("alertes")]
    public async Task<ActionResult<List<AlerteStockDto>>> GetAlertes()
    {
        try
        {
            var alertes = await _stockService.GetAlertesAsync();
            return Ok(alertes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération alertes");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    // ==================== Médicaments/Stock ====================

    [HttpGet("medicaments")]
    public async Task<ActionResult<PagedResult<MedicamentStockDto>>> GetMedicaments(
        [FromQuery] string? search,
        [FromQuery] string? statut,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _stockService.GetMedicamentsAsync(search, statut, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération médicaments");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpGet("medicaments/{id}")]
    public async Task<ActionResult<MedicamentStockDto>> GetMedicament(int id)
    {
        try
        {
            var medicament = await _stockService.GetMedicamentByIdAsync(id);
            if (medicament == null)
                return NotFound(new { message = "Médicament non trouvé" });
            return Ok(medicament);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération médicament {Id}", id);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpPost("medicaments")]
    public async Task<ActionResult<MedicamentStockDto>> CreateMedicament([FromBody] CreateMedicamentRequest request)
    {
        try
        {
            var medicament = await _stockService.CreateMedicamentAsync(request);
            return CreatedAtAction(nameof(GetMedicament), new { id = medicament.IdMedicament }, medicament);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur création médicament");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpPut("medicaments/{id}")]
    public async Task<ActionResult<MedicamentStockDto>> UpdateMedicament(int id, [FromBody] UpdateMedicamentRequest request)
    {
        try
        {
            var medicament = await _stockService.UpdateMedicamentAsync(id, request);
            return Ok(medicament);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Médicament non trouvé" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur mise à jour médicament {Id}", id);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpDelete("medicaments/{id}")]
    public async Task<ActionResult> DeleteMedicament(int id)
    {
        try
        {
            var success = await _stockService.DeleteMedicamentAsync(id);
            if (!success)
                return NotFound(new { message = "Médicament non trouvé" });
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur suppression médicament {Id}", id);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpPost("stock/ajustement")]
    public async Task<ActionResult<MouvementStockDto>> AjusterStock([FromBody] AjustementStockRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();
            var mouvement = await _stockService.AjusterStockAsync(request, userId.Value);
            return Ok(mouvement);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur ajustement stock");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    // ==================== Mouvements ====================

    [HttpGet("mouvements")]
    public async Task<ActionResult<PagedResult<MouvementStockDto>>> GetMouvements([FromQuery] MouvementStockFilter filter)
    {
        try
        {
            var result = await _stockService.GetMouvementsAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération mouvements");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    // ==================== Fournisseurs ====================

    [HttpGet("fournisseurs")]
    public async Task<ActionResult<List<FournisseurDto>>> GetFournisseurs([FromQuery] bool? actif)
    {
        try
        {
            var fournisseurs = await _stockService.GetFournisseursAsync(actif);
            return Ok(fournisseurs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération fournisseurs");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpPost("fournisseurs")]
    public async Task<ActionResult<FournisseurDto>> CreateFournisseur([FromBody] CreateFournisseurRequest request)
    {
        try
        {
            var fournisseur = await _stockService.CreateFournisseurAsync(request);
            return CreatedAtAction(nameof(GetFournisseurs), fournisseur);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur création fournisseur");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpPut("fournisseurs/{id}")]
    public async Task<ActionResult<FournisseurDto>> UpdateFournisseur(int id, [FromBody] CreateFournisseurRequest request)
    {
        try
        {
            var fournisseur = await _stockService.UpdateFournisseurAsync(id, request);
            return Ok(fournisseur);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Fournisseur non trouvé" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur mise à jour fournisseur {Id}", id);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    // ==================== Commandes ====================

    [HttpGet("commandes")]
    public async Task<ActionResult<PagedResult<CommandePharmacieDto>>> GetCommandes(
        [FromQuery] string? statut,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _stockService.GetCommandesAsync(statut, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération commandes");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpGet("commandes/{id}")]
    public async Task<ActionResult<CommandePharmacieDto>> GetCommande(int id)
    {
        try
        {
            var commande = await _stockService.GetCommandeByIdAsync(id);
            if (commande == null)
                return NotFound(new { message = "Commande non trouvée" });
            return Ok(commande);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération commande {Id}", id);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpPost("commandes")]
    public async Task<ActionResult<CommandePharmacieDto>> CreateCommande([FromBody] CreateCommandeRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();
            var commande = await _stockService.CreateCommandeAsync(request, userId.Value);
            return CreatedAtAction(nameof(GetCommande), new { id = commande.IdCommande }, commande);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur création commande");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpPost("commandes/{id}/reception")]
    public async Task<ActionResult<CommandePharmacieDto>> ReceptionnerCommande(int id, [FromBody] ReceptionCommandeRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();
            var commande = await _stockService.ReceptionnerCommandeAsync(id, request, userId.Value);
            return Ok(commande);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Commande non trouvée" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur réception commande {Id}", id);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpPost("commandes/{id}/annuler")]
    public async Task<ActionResult> AnnulerCommande(int id)
    {
        try
        {
            var success = await _stockService.AnnulerCommandeAsync(id);
            if (!success)
                return NotFound(new { message = "Commande non trouvée" });
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur annulation commande {Id}", id);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    // ==================== Ordonnances/Dispensations ====================

    [HttpGet("ordonnances")]
    public async Task<ActionResult<PagedResult<OrdonnancePharmacieDto>>> GetOrdonnances(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _stockService.GetOrdonnancesEnAttenteAsync(search, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération ordonnances");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpPost("dispensations")]
    public async Task<ActionResult<DispensationDto>> DispenserOrdonnance([FromBody] CreateDispensationRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();
            var dispensation = await _stockService.DispenserOrdonnanceAsync(request, userId.Value);
            return Ok(dispensation);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur dispensation ordonnance");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpGet("dispensations")]
    public async Task<ActionResult<PagedResult<DispensationDto>>> GetDispensations(
        [FromQuery] DateTime? dateDebut,
        [FromQuery] DateTime? dateFin,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _stockService.GetDispensationsAsync(dateDebut, dateFin, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération dispensations");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }
}
