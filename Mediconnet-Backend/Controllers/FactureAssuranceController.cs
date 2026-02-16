using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mediconnet_Backend.Services;

namespace Mediconnet_Backend.Controllers;

[ApiController]
[Route("api/factures-assurance")]
[Authorize]
public class FactureAssuranceController : ControllerBase
{
    private readonly IFactureAssuranceService _factureAssuranceService;
    private readonly ILogger<FactureAssuranceController> _logger;

    public FactureAssuranceController(
        IFactureAssuranceService factureAssuranceService,
        ILogger<FactureAssuranceController> logger)
    {
        _factureAssuranceService = factureAssuranceService;
        _logger = logger;
    }

    /// <summary>
    /// Récupère la liste des factures assurance avec filtres
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetFactures([FromQuery] FactureAssuranceFilter? filter)
    {
        var factures = await _factureAssuranceService.GetFacturesAssuranceAsync(filter);
        return Ok(new { success = true, data = factures, total = factures.Count });
    }

    /// <summary>
    /// Récupère les statistiques des factures assurance
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStatistiques()
    {
        var stats = await _factureAssuranceService.GetStatistiquesAsync();
        return Ok(new { success = true, data = stats });
    }

    /// <summary>
    /// Récupère une facture assurance par ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetFacture(int id)
    {
        var facture = await _factureAssuranceService.GetFactureAssuranceAsync(id);
        if (facture == null)
            return NotFound(new { success = false, message = "Facture non trouvée" });

        return Ok(new { success = true, data = facture });
    }

    /// <summary>
    /// Envoie une facture à l'assurance par email avec PDF
    /// </summary>
    [HttpPost("{id}/envoyer")]
    [Authorize(Roles = "administrateur,caissier")]
    public async Task<IActionResult> EnvoyerFacture(int id)
    {
        var result = await _factureAssuranceService.EnvoyerFactureAssuranceAsync(id);
        
        if (result.Success)
            return Ok(new { success = true, message = result.Message, numeroFacture = result.NumeroFacture });
        
        return BadRequest(new { success = false, message = result.Message });
    }

    /// <summary>
    /// Met à jour le statut d'une facture (admin uniquement)
    /// </summary>
    [HttpPut("{id}/statut")]
    [Authorize(Roles = "administrateur")]
    public async Task<IActionResult> UpdateStatut(int id, [FromBody] UpdateStatutRequest request)
    {
        if (string.IsNullOrEmpty(request.Statut))
            return BadRequest(new { success = false, message = "Le statut est requis" });

        var validStatuts = new[] { "en_attente", "envoyee_assurance", "payee", "partiellement_payee", "rejetee", "annulee" };
        if (!validStatuts.Contains(request.Statut.ToLower()))
            return BadRequest(new { success = false, message = "Statut invalide" });

        var success = await _factureAssuranceService.UpdateStatutFactureAsync(id, request.Statut.ToLower(), request.Notes);
        
        if (success)
            return Ok(new { success = true, message = "Statut mis à jour avec succès" });
        
        return NotFound(new { success = false, message = "Facture non trouvée" });
    }

    /// <summary>
    /// Télécharge le PDF d'une facture
    /// </summary>
    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> TelechargerPdf(int id)
    {
        var pdfContent = await _factureAssuranceService.TelechargerFacturePdfAsync(id);
        
        if (pdfContent == null)
            return NotFound(new { success = false, message = "Facture non trouvée" });

        var facture = await _factureAssuranceService.GetFactureAssuranceAsync(id);
        var fileName = $"Facture_{facture?.NumeroFacture ?? id.ToString()}.pdf";

        return File(pdfContent, "application/pdf", fileName);
    }

    /// <summary>
    /// Envoie plusieurs factures en lot
    /// </summary>
    [HttpPost("envoyer-lot")]
    [Authorize(Roles = "administrateur")]
    public async Task<IActionResult> EnvoyerLot([FromBody] EnvoyerLotRequest request)
    {
        if (request.FactureIds == null || !request.FactureIds.Any())
            return BadRequest(new { success = false, message = "Aucune facture sélectionnée" });

        var resultats = new List<object>();
        var succes = 0;
        var echecs = 0;

        foreach (var id in request.FactureIds)
        {
            var result = await _factureAssuranceService.EnvoyerFactureAssuranceAsync(id);
            resultats.Add(new { idFacture = id, success = result.Success, message = result.Message });
            
            if (result.Success) succes++;
            else echecs++;
        }

        return Ok(new 
        { 
            success = true, 
            message = $"{succes} facture(s) envoyée(s), {echecs} échec(s)",
            details = resultats 
        });
    }
}

public class UpdateStatutRequest
{
    public string Statut { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class EnvoyerLotRequest
{
    public List<int> FactureIds { get; set; } = new();
}
