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

    [HttpGet("profile")]
    public async Task<ActionResult<PharmacieProfileDto>> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var profile = await _stockService.GetProfileAsync(userId.Value);
            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération profile pharmacie");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<PharmacieDashboardDto>> GetDashboard()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var dashboard = await _stockService.GetDashboardAsync(userId.Value);
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération dashboard pharmacie");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpPut("profile")]
    public async Task<ActionResult<PharmacieProfileDto>> UpdateProfile([FromBody] UpdatePharmacieProfileRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var updatedProfile = await _stockService.UpdateProfileAsync(userId.Value, request);
            return Ok(updatedProfile);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur mise à jour profile pharmacie");
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

    [HttpGet("medicaments/{id}/fournisseurs")]
    public async Task<ActionResult<List<FournisseurMedicamentDto>>> GetFournisseursByMedicament(int id)
    {
        try
        {
            var fournisseurs = await _stockService.GetFournisseursByMedicamentAsync(id);
            return Ok(fournisseurs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération fournisseurs du médicament {Id}", id);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpGet("medicaments/{id}/historique")]
    public async Task<ActionResult<List<HistoriqueFournisseurMedicamentDto>>> GetHistoriqueFournisseurMedicament(int id)
    {
        try
        {
            var historique = await _stockService.GetHistoriqueFournisseurMedicamentAsync(id);
            return Ok(historique);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération historique du médicament {Id}", id);
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
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Fournisseur non trouvé: {Id}", id);
            return NotFound(new { message = "Fournisseur non trouvé" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur mise à jour fournisseur {Id}", id);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpPatch("fournisseurs/{id}/toggle-statut")]
    public async Task<ActionResult<FournisseurDto>> ToggleFournisseurStatut(int id)
    {
        try
        {
            var fournisseur = await _stockService.ToggleFournisseurStatutAsync(id);
            return Ok(fournisseur);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Fournisseur non trouvé: {Id}", id);
            return NotFound(new { message = "Fournisseur non trouvé" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur changement statut fournisseur {Id}", id);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpDelete("fournisseurs/{id}")]
    public async Task<ActionResult> DeleteFournisseur(int id)
    {
        try
        {
            var success = await _stockService.DeleteFournisseurAsync(id);
            if (!success)
                return NotFound(new { message = "Fournisseur non trouvé" });
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur suppression fournisseur {Id}", id);
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

    // ==================== NOUVEAU WORKFLOW PHARMACIE ====================
    // Prescription → Validation (Facture) → Paiement → Délivrance (Stock)

    /// <summary>
    /// Récupère le détail d'une ordonnance avec son statut de paiement
    /// </summary>
    [HttpGet("ordonnances/{idOrdonnance}")]
    public async Task<ActionResult<OrdonnancePharmacieDetailDto>> GetOrdonnanceDetail(int idOrdonnance)
    {
        try
        {
            var result = await _stockService.GetOrdonnanceDetailAsync(idOrdonnance);
            if (result == null)
                return NotFound(new { message = "Ordonnance non trouvée" });
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération détail ordonnance {Id}", idOrdonnance);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Valide une ordonnance : crée la facture associée SANS impact sur le stock.
    /// Le patient peut ensuite aller payer à la caisse.
    /// </summary>
    [HttpPost("ordonnances/{idOrdonnance}/valider")]
    public async Task<ActionResult<ValidationOrdonnanceResult>> ValiderOrdonnance(int idOrdonnance)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var result = await _stockService.ValiderOrdonnanceAsync(idOrdonnance, userId.Value);
            
            if (!result.Success)
                return BadRequest(result);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur validation ordonnance {Id}", idOrdonnance);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Délivre les médicaments d'une ordonnance PAYÉE.
    /// Décrémente le stock et enregistre la dispensation.
    /// Ce bouton n'est actif que si la facture est payée.
    /// </summary>
    [HttpPost("ordonnances/{idOrdonnance}/delivrer")]
    public async Task<ActionResult<DelivranceResult>> DelivrerOrdonnance(int idOrdonnance)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var result = await _stockService.DelivrerOrdonnanceAsync(idOrdonnance, userId.Value);
            
            if (!result.Success)
                return BadRequest(result);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur délivrance ordonnance {Id}", idOrdonnance);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    // ==================== Ventes Directes ====================

    /// <summary>
    /// Crée une vente directe sans ordonnance
    /// </summary>
    [HttpPost("ventes-directes")]
    public async Task<ActionResult<VenteDirecteResult>> CreerVenteDirecte([FromBody] CreateVenteDirecteRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var result = await _stockService.CreerVenteDirecteAsync(request, userId.Value);
            
            if (!result.Success)
                return BadRequest(result);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur création vente directe");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupère la liste des ventes directes avec pagination et filtres
    /// </summary>
    [HttpGet("ventes-directes")]
    public async Task<ActionResult<PagedResult<VenteDirecteDto>>> GetVentesDirectes(
        [FromQuery] DateTime? dateDebut,
        [FromQuery] DateTime? dateFin,
        [FromQuery] string? nomClient,
        [FromQuery] string? numeroTicket,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var filter = new VenteDirecteFilter
            {
                DateDebut = dateDebut,
                DateFin = dateFin,
                NomClient = nomClient,
                NumeroTicket = numeroTicket,
                Page = page,
                PageSize = pageSize
            };

            var result = await _stockService.GetVentesDirectesAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération ventes directes");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupère le détail d'une vente directe
    /// </summary>
    [HttpGet("ventes-directes/{id}")]
    public async Task<ActionResult<VenteDirecteDto>> GetVenteDirecte(int id)
    {
        try
        {
            var result = await _stockService.GetVenteDirecteByIdAsync(id);
            
            if (result == null)
                return NotFound(new { message = "Vente directe non trouvée" });
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération vente directe {Id}", id);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Délivre une vente directe après paiement à la caisse
    /// Décrémente le stock et met à jour le statut vers "delivre"
    /// </summary>
    [HttpPost("ventes-directes/{id}/delivrer")]
    public async Task<ActionResult<VenteDirecteResult>> DelivrerVenteDirecte(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue || userId == 0)
                return Unauthorized(new { message = "Utilisateur non authentifié" });

            var result = await _stockService.DelivrerVenteDirecteAsync(id, userId.Value);
            
            if (!result.Success)
                return BadRequest(result);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur délivrance vente directe {Id}", id);
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }
}
