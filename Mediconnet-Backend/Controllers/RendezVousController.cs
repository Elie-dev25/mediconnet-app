using Mediconnet_Backend.Controllers.Base;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.DTOs.RendezVous;
using Microsoft.AspNetCore.Mvc;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// Contrôleur pour la gestion des rendez-vous patient
/// </summary>
[Route("api/[controller]")]
public class RendezVousController : BaseApiController
{
    private readonly IRendezVousService _rdvService;
    private readonly ILogger<RendezVousController> _logger;

    public RendezVousController(IRendezVousService rdvService, ILogger<RendezVousController> logger)
    {
        _rdvService = rdvService;
        _logger = logger;
    }

    // ==================== STATISTIQUES ====================

    /// <summary>
    /// Obtenir les statistiques des rendez-vous du patient
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var stats = await _rdvService.GetPatientStatsAsync(userId.Value);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetStats: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des statistiques" });
        }
    }

    // ==================== LISTE DES RENDEZ-VOUS ====================

    /// <summary>
    /// Obtenir les rendez-vous à venir
    /// </summary>
    [HttpGet("a-venir")]
    public async Task<IActionResult> GetUpcoming()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var rdvs = await _rdvService.GetPatientUpcomingAsync(userId.Value);
            return Ok(rdvs);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetUpcoming: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des rendez-vous" });
        }
    }

    /// <summary>
    /// Obtenir l'historique des rendez-vous
    /// </summary>
    [HttpGet("historique")]
    public async Task<IActionResult> GetHistory([FromQuery] int limite = 20)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var rdvs = await _rdvService.GetPatientHistoryAsync(userId.Value, limite);
            return Ok(rdvs);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetHistory: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération de l'historique" });
        }
    }

    /// <summary>
    /// Obtenir le détail d'un rendez-vous
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRendezVous(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var rdv = await _rdvService.GetRendezVousAsync(id, userId.Value);
            if (rdv == null)
                return NotFound(new { message = "Rendez-vous introuvable" });

            return Ok(rdv);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetRendezVous: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération du rendez-vous" });
        }
    }

    // ==================== CRÉATION / MODIFICATION ====================

    /// <summary>
    /// Créer un nouveau rendez-vous
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRendezVousRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var (success, message, rdv) = await _rdvService.CreateRendezVousAsync(request, userId.Value);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message, rendezVous = rdv });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur Create: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la création du rendez-vous" });
        }
    }

    /// <summary>
    /// Modifier un rendez-vous
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRendezVousRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var (success, message) = await _rdvService.UpdateRendezVousAsync(id, request, userId.Value);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur Update: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la modification du rendez-vous" });
        }
    }

    /// <summary>
    /// Annuler un rendez-vous
    /// </summary>
    [HttpPost("annuler")]
    public async Task<IActionResult> Annuler([FromBody] AnnulerRendezVousRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var (success, message) = await _rdvService.AnnulerRendezVousAsync(request, userId.Value);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur Annuler: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de l'annulation du rendez-vous" });
        }
    }

    // ==================== MÉDECINS ET CRÉNEAUX ====================

    /// <summary>
    /// Obtenir la liste des médecins disponibles
    /// </summary>
    [HttpGet("medecins")]
    public async Task<IActionResult> GetMedecins([FromQuery] int? serviceId)
    {
        try
        {
            var medecins = await _rdvService.GetMedecinsDisponiblesAsync(serviceId);
            return Ok(medecins);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetMedecins: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des médecins" });
        }
    }

    /// <summary>
    /// Obtenir les créneaux disponibles pour un médecin
    /// </summary>
    [HttpGet("creneaux/{medecinId}")]
    public async Task<IActionResult> GetCreneaux(
        int medecinId,
        [FromQuery] DateTime dateDebut,
        [FromQuery] DateTime dateFin)
    {
        try
        {
            if (dateFin <= dateDebut)
                return BadRequest(new { message = "La date de fin doit être après la date de début" });

            if ((dateFin - dateDebut).TotalDays > 30)
                return BadRequest(new { message = "La période ne peut pas dépasser 30 jours" });

            var creneaux = await _rdvService.GetCreneauxDisponiblesAsync(medecinId, dateDebut, dateFin);
            return Ok(creneaux);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetCreneaux: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des créneaux" });
        }
    }

    // ==================== ENDPOINTS MÉDECIN ====================

    /// <summary>
    /// Obtenir les rendez-vous du médecin connecté
    /// </summary>
    [HttpGet("medecin/list")]
    public async Task<IActionResult> GetMedecinRdvList(
        [FromQuery] DateTime? dateDebut,
        [FromQuery] DateTime? dateFin,
        [FromQuery] string? statut)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var rdvs = await _rdvService.GetMedecinRendezVousAsync(medecinId.Value, dateDebut, dateFin, statut);
            return Ok(rdvs);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetMedecinRdvList: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Obtenir les RDV du jour pour le médecin
    /// </summary>
    [HttpGet("medecin/jour")]
    public async Task<IActionResult> GetMedecinRdvJour([FromQuery] DateTime? date)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var dateRdv = date ?? DateTime.UtcNow;
            var rdvs = await _rdvService.GetMedecinRdvJourAsync(medecinId.Value, dateRdv);
            return Ok(rdvs);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetMedecinRdvJour: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Mettre à jour le statut d'un RDV
    /// </summary>
    [HttpPatch("medecin/{rdvId}/statut")]
    public async Task<IActionResult> UpdateStatutRdv(int rdvId, [FromBody] UpdateStatutRequest request)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var (success, message) = await _rdvService.UpdateStatutRdvAsync(medecinId.Value, rdvId, request.Statut);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur UpdateStatutRdv: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Obtenir les RDV en attente de validation
    /// </summary>
    [HttpGet("medecin/en-attente")]
    public async Task<IActionResult> GetRdvEnAttente()
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var rdvs = await _rdvService.GetRdvEnAttenteAsync(medecinId.Value);
            return Ok(rdvs);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetRdvEnAttente: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Valider un RDV (confirmer)
    /// </summary>
    [HttpPost("medecin/valider")]
    public async Task<IActionResult> ValiderRdv([FromBody] ValiderRdvRequest request)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var response = await _rdvService.ValiderRdvAsync(medecinId.Value, request.IdRendezVous);

            if (!response.Success)
            {
                if (response.ConflitDetecte)
                    return Conflict(new { message = response.Message, conflitDetecte = true });
                return BadRequest(new { message = response.Message });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur ValiderRdv: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Annuler un RDV par le médecin
    /// </summary>
    [HttpPost("medecin/annuler")]
    public async Task<IActionResult> AnnulerRdvMedecin([FromBody] AnnulerRdvMedecinRequest request)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var response = await _rdvService.AnnulerRdvMedecinAsync(medecinId.Value, request);

            if (!response.Success)
                return BadRequest(new { message = response.Message });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur AnnulerRdvMedecin: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Suggérer un nouveau créneau pour un RDV
    /// </summary>
    [HttpPost("medecin/suggerer-creneau")]
    public async Task<IActionResult> SuggererCreneau([FromBody] SuggererCreneauRequest request)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var response = await _rdvService.SuggererCreneauAsync(medecinId.Value, request);

            if (!response.Success)
            {
                if (response.ConflitDetecte)
                    return Conflict(new { message = response.Message, conflitDetecte = true });
                return BadRequest(new { message = response.Message });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur SuggererCreneau: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    // ==================== GESTION PROPOSITIONS PATIENT ====================

    /// <summary>
    /// Récupérer les propositions de créneaux pour le patient connecté
    /// </summary>
    [HttpGet("patient/propositions")]
    public async Task<IActionResult> GetPropositionsPatient()
    {
        try
        {
            var patientId = GetCurrentUserId();
            if (patientId == null) return Unauthorized();

            var propositions = await _rdvService.GetPropositionsPatientAsync(patientId.Value);
            return Ok(propositions);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetPropositionsPatient: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Accepter une proposition de créneau
    /// </summary>
    [HttpPost("patient/accepter-proposition")]
    public async Task<IActionResult> AccepterProposition([FromBody] AccepterPropositionRequest request)
    {
        try
        {
            var patientId = GetCurrentUserId();
            if (patientId == null) return Unauthorized();

            var response = await _rdvService.AccepterPropositionAsync(patientId.Value, request.IdRendezVous);

            if (!response.Success)
            {
                if (response.ConflitDetecte)
                    return Conflict(new { message = response.Message, conflitDetecte = true });
                return BadRequest(new { message = response.Message });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur AccepterProposition: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Refuser une proposition de créneau
    /// </summary>
    [HttpPost("patient/refuser-proposition")]
    public async Task<IActionResult> RefuserProposition([FromBody] RefuserPropositionRequest request)
    {
        try
        {
            var patientId = GetCurrentUserId();
            if (patientId == null) return Unauthorized();

            var response = await _rdvService.RefuserPropositionAsync(patientId.Value, request);

            if (!response.Success)
                return BadRequest(new { message = response.Message });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur RefuserProposition: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }
}
