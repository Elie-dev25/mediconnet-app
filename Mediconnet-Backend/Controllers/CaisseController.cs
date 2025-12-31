using Mediconnet_Backend.Controllers.Base;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.DTOs.Caisse;
using Microsoft.AspNetCore.Mvc;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// Contrôleur pour la gestion de la caisse
/// </summary>
[Route("api/[controller]")]
public class CaisseController : BaseApiController
{
    private readonly ICaisseService _caisseService;
    private readonly ILogger<CaisseController> _logger;

    public CaisseController(ICaisseService caisseService, ILogger<CaisseController> logger)
    {
        _caisseService = caisseService;
        _logger = logger;
    }

    // ==================== KPIs ====================

    /// <summary>
    /// Obtenir les KPIs du dashboard caissier
    /// </summary>
    [HttpGet("kpis")]
    public async Task<IActionResult> GetKpis()
    {
        try
        {
            if (!IsCaissier()) return Forbid();

            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var kpis = await _caisseService.GetKpisAsync(userId.Value);
            return Ok(kpis);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetKpis: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des KPIs" });
        }
    }

    // ==================== FACTURES ====================

    /// <summary>
    /// Obtenir les factures en attente de paiement
    /// </summary>
    [HttpGet("factures/en-attente")]
    public async Task<IActionResult> GetFacturesEnAttente()
    {
        try
        {
            if (!IsCaissier()) return Forbid();

            var factures = await _caisseService.GetFacturesEnAttenteAsync();
            return Ok(factures);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetFacturesEnAttente: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des factures" });
        }
    }

    /// <summary>
    /// Obtenir les factures d'un patient
    /// </summary>
    [HttpGet("factures/patient/{idPatient}")]
    public async Task<IActionResult> GetFacturesPatient(int idPatient)
    {
        try
        {
            if (!IsCaissier()) return Forbid();

            var factures = await _caisseService.GetFacturesPatientAsync(idPatient);
            return Ok(factures);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetFacturesPatient: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des factures" });
        }
    }

    /// <summary>
    /// Obtenir le détail d'une facture
    /// </summary>
    [HttpGet("factures/{id}")]
    public async Task<IActionResult> GetFacture(int id)
    {
        try
        {
            if (!IsCaissier()) return Forbid();

            var facture = await _caisseService.GetFactureAsync(id);
            if (facture == null)
                return NotFound(new { message = "Facture introuvable" });

            return Ok(facture);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetFacture: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération de la facture" });
        }
    }

    // ==================== TRANSACTIONS ====================

    /// <summary>
    /// Obtenir les transactions du jour
    /// </summary>
    [HttpGet("transactions/jour")]
    public async Task<IActionResult> GetTransactionsJour()
    {
        try
        {
            if (!IsCaissier()) return Forbid();

            var userId = GetCurrentUserId();
            var transactions = await _caisseService.GetTransactionsJourAsync(userId);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetTransactionsJour: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des transactions" });
        }
    }

    /// <summary>
    /// Rechercher des transactions avec filtres
    /// </summary>
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] DateTime? dateDebut,
        [FromQuery] DateTime? dateFin,
        [FromQuery] string? modePaiement)
    {
        try
        {
            if (!IsCaissier()) return Forbid();

            var transactions = await _caisseService.GetTransactionsAsync(dateDebut, dateFin, modePaiement);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetTransactions: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des transactions" });
        }
    }

    /// <summary>
    /// Créer une nouvelle transaction (paiement)
    /// </summary>
    [HttpPost("transactions")]
    public async Task<IActionResult> CreerTransaction([FromBody] CreateTransactionRequest request)
    {
        try
        {
            if (!IsCaissier()) return Forbid();

            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var (success, message, transaction) = await _caisseService.CreerTransactionAsync(request, userId.Value);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message, transaction });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur CreerTransaction: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la création de la transaction" });
        }
    }

    /// <summary>
    /// Annuler une transaction
    /// </summary>
    [HttpPost("transactions/annuler")]
    public async Task<IActionResult> AnnulerTransaction([FromBody] AnnulerTransactionRequest request)
    {
        try
        {
            if (!IsCaissier()) return Forbid();

            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var (success, message) = await _caisseService.AnnulerTransactionAsync(request, userId.Value);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur AnnulerTransaction: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de l'annulation de la transaction" });
        }
    }

    // ==================== SESSION CAISSE ====================

    /// <summary>
    /// Obtenir la session de caisse active
    /// </summary>
    [HttpGet("session/active")]
    public async Task<IActionResult> GetSessionActive()
    {
        try
        {
            if (!IsCaissier()) return Forbid();

            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var session = await _caisseService.GetSessionActiveAsync(userId.Value);
            return Ok(session); // null si pas de session active
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetSessionActive: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération de la session" });
        }
    }

    /// <summary>
    /// Obtenir l'historique des sessions
    /// </summary>
    [HttpGet("session/historique")]
    public async Task<IActionResult> GetHistoriqueSessions([FromQuery] int limite = 10)
    {
        try
        {
            if (!IsCaissier()) return Forbid();

            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var sessions = await _caisseService.GetHistoriqueSessionsAsync(userId.Value, limite);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetHistoriqueSessions: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération de l'historique" });
        }
    }

    /// <summary>
    /// Ouvrir une session de caisse
    /// </summary>
    [HttpPost("session/ouvrir")]
    public async Task<IActionResult> OuvrirCaisse([FromBody] OuvrirCaisseRequest request)
    {
        try
        {
            if (!IsCaissier()) return Forbid();

            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var (success, message, session) = await _caisseService.OuvrirCaisseAsync(request, userId.Value);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message, session });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur OuvrirCaisse: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de l'ouverture de la caisse" });
        }
    }

    /// <summary>
    /// Fermer une session de caisse
    /// </summary>
    [HttpPost("session/fermer")]
    public async Task<IActionResult> FermerCaisse([FromBody] FermerCaisseRequest request)
    {
        try
        {
            if (!IsCaissier()) return Forbid();

            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var (success, message, session) = await _caisseService.FermerCaisseAsync(request, userId.Value);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message, session });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur FermerCaisse: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la fermeture de la caisse" });
        }
    }

    // ==================== REÇU ====================

    /// <summary>
    /// Obtenir le reçu d'une transaction pour impression
    /// </summary>
    [HttpGet("transactions/{id}/recu")]
    public async Task<IActionResult> GetRecuTransaction(int id)
    {
        try
        {
            if (!IsCaissier()) return Forbid();

            var recu = await _caisseService.GetRecuTransactionAsync(id);
            if (recu == null)
                return NotFound(new { message = "Transaction introuvable" });

            return Ok(recu);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetRecuTransaction: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la génération du reçu" });
        }
    }

    // ==================== RECHERCHE PATIENT ====================

    /// <summary>
    /// Rechercher des patients
    /// </summary>
    [HttpGet("patients/recherche")]
    public async Task<IActionResult> RechercherPatients([FromQuery] string q)
    {
        try
        {
            if (!IsCaissier()) return Forbid();

            var patients = await _caisseService.RechercherPatientsAsync(q);
            return Ok(patients);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur RechercherPatients: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la recherche" });
        }
    }

    // ==================== STATISTIQUES ====================

    /// <summary>
    /// Obtenir la répartition des paiements par mode
    /// </summary>
    [HttpGet("stats/repartition")]
    public async Task<IActionResult> GetRepartitionPaiements(
        [FromQuery] DateTime? dateDebut,
        [FromQuery] DateTime? dateFin)
    {
        try
        {
            if (!IsCaissier()) return Forbid();

            var repartition = await _caisseService.GetRepartitionPaiementsAsync(dateDebut, dateFin);
            return Ok(repartition);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetRepartitionPaiements: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des statistiques" });
        }
    }

    /// <summary>
    /// Obtenir les revenus par service
    /// </summary>
    [HttpGet("stats/revenus-service")]
    public async Task<IActionResult> GetRevenusParService([FromQuery] DateTime? date)
    {
        try
        {
            if (!IsCaissier()) return Forbid();

            var revenus = await _caisseService.GetRevenusParServiceAsync(date ?? DateTime.UtcNow);
            return Ok(revenus);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetRevenusParService: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des revenus" });
        }
    }

    /// <summary>
    /// Obtenir les factures en retard
    /// </summary>
    [HttpGet("stats/factures-retard")]
    public async Task<IActionResult> GetFacturesEnRetard([FromQuery] int limite = 5)
    {
        try
        {
            if (!IsCaissier()) return Forbid();

            var factures = await _caisseService.GetFacturesEnRetardAsync(limite);
            return Ok(factures);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetFacturesEnRetard: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des factures en retard" });
        }
    }
}
