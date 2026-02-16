using Mediconnet_Backend.Controllers.Base;
using Mediconnet_Backend.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// Contrôleur pour la facturation avancée : PDF, échéanciers, remboursements assurance
/// </summary>
[Route("api/[controller]")]
public class FacturationController : BaseApiController
{
    private readonly IFactureService _factureService;
    private readonly ILogger<FacturationController> _logger;

    public FacturationController(IFactureService factureService, ILogger<FacturationController> logger)
    {
        _factureService = factureService;
        _logger = logger;
    }

    // ==================== PDF ====================

    /// <summary>
    /// Générer le PDF d'une facture
    /// </summary>
    [HttpGet("factures/{idFacture}/pdf")]
    public async Task<IActionResult> GetFacturePdf(int idFacture)
    {
        var accessCheck = CheckAuthentication();
        if (accessCheck != null) return accessCheck;

        try
        {
            var pdf = await _factureService.GenerateFacturePdfAsync(idFacture);
            return File(pdf, "text/html", $"facture_{idFacture}.html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur génération PDF facture {IdFacture}", idFacture);
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Générer le PDF d'un reçu de transaction
    /// </summary>
    [HttpGet("transactions/{idTransaction}/recu")]
    public async Task<IActionResult> GetRecuPdf(int idTransaction)
    {
        var accessCheck = CheckAuthentication();
        if (accessCheck != null) return accessCheck;

        try
        {
            var pdf = await _factureService.GenerateRecuPdfAsync(idTransaction);
            return File(pdf, "text/html", $"recu_{idTransaction}.html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur génération reçu transaction {IdTransaction}", idTransaction);
            return NotFound(new { message = ex.Message });
        }
    }

    // ==================== ÉCHÉANCIERS ====================

    /// <summary>
    /// Créer un échéancier de paiement pour une facture
    /// </summary>
    [HttpPost("echeanciers")]
    public async Task<IActionResult> CreerEcheancier([FromBody] CreateEcheancierRequest request)
    {
        var accessCheck = CheckCaissierAccess();
        if (accessCheck != null) return accessCheck;

        try
        {
            var result = await _factureService.CreerEcheancierAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur création échéancier pour facture {IdFacture}", request.IdFacture);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Obtenir l'échéancier d'une facture
    /// </summary>
    [HttpGet("factures/{idFacture}/echeancier")]
    public async Task<IActionResult> GetEcheancier(int idFacture)
    {
        var accessCheck = CheckAuthentication();
        if (accessCheck != null) return accessCheck;

        var result = await _factureService.GetEcheancierAsync(idFacture);
        if (result == null) return NotFound(new { message = "Aucun échéancier actif pour cette facture" });
        return Ok(result);
    }

    /// <summary>
    /// Obtenir les échéances en retard
    /// </summary>
    [HttpGet("echeances/retard")]
    public async Task<IActionResult> GetEcheancesEnRetard()
    {
        var accessCheck = CheckCaissierAccess();
        if (accessCheck != null) return accessCheck;

        var result = await _factureService.GetEcheancesEnRetardAsync();
        return Ok(result);
    }

    /// <summary>
    /// Marquer une échéance comme payée
    /// </summary>
    [HttpPut("echeances/{idEcheance}/payer")]
    public async Task<IActionResult> MarquerEcheancePayee(int idEcheance, [FromBody] MarquerEcheancePayeeRequest request)
    {
        var accessCheck = CheckCaissierAccess();
        if (accessCheck != null) return accessCheck;

        var success = await _factureService.MarquerEcheancePayeeAsync(idEcheance, request.IdTransaction);
        if (!success) return NotFound(new { message = "Échéance non trouvée" });
        return Ok(new { message = "Échéance marquée comme payée" });
    }

    // ==================== REMBOURSEMENTS ASSURANCE ====================

    /// <summary>
    /// Créer une demande de remboursement assurance
    /// </summary>
    [HttpPost("remboursements")]
    public async Task<IActionResult> CreerDemandeRemboursement([FromBody] CreateDemandeRemboursementRequest request)
    {
        var accessCheck = CheckCaissierAccess();
        if (accessCheck != null) return accessCheck;

        try
        {
            var result = await _factureService.CreerDemandeRemboursementAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur création demande remboursement pour facture {IdFacture}", request.IdFacture);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Obtenir les demandes de remboursement (filtres optionnels)
    /// </summary>
    [HttpGet("remboursements")]
    public async Task<IActionResult> GetDemandesRemboursement([FromQuery] int? idAssurance, [FromQuery] string? statut)
    {
        var accessCheck = CheckCaissierAccess();
        if (accessCheck != null) return accessCheck;

        var result = await _factureService.GetDemandesRemboursementAsync(idAssurance, statut);
        return Ok(result);
    }

    /// <summary>
    /// Traiter une demande de remboursement (approuver/rejeter)
    /// </summary>
    [HttpPut("remboursements/{idDemande}/traiter")]
    public async Task<IActionResult> TraiterDemandeRemboursement(int idDemande, [FromBody] TraiterDemandeRequest request)
    {
        var accessCheck = CheckCaissierAccess();
        if (accessCheck != null) return accessCheck;

        try
        {
            var result = await _factureService.TraiterDemandeRemboursementAsync(idDemande, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur traitement demande remboursement {IdDemande}", idDemande);
            return BadRequest(new { message = ex.Message });
        }
    }

    // ==================== COUVERTURE ASSURANCE ====================

    /// <summary>
    /// Calculer la couverture assurance pour un patient
    /// </summary>
    [HttpGet("couverture/{idPatient}")]
    public async Task<IActionResult> CalculerCouverture(int idPatient, [FromQuery] decimal montant, [FromQuery] string typeActe = "consultation")
    {
        var accessCheck = CheckAuthentication();
        if (accessCheck != null) return accessCheck;

        var couverture = await _factureService.CalculerCouvertureAssuranceAsync(idPatient, montant, typeActe);
        return Ok(new { montantCouvert = couverture, montantRestant = montant - couverture });
    }
}

/// <summary>
/// DTO pour marquer une échéance comme payée
/// </summary>
public class MarquerEcheancePayeeRequest
{
    public int IdTransaction { get; set; }
}
