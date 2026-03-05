using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.DTOs.Prescription;
using System.Security.Claims;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// Contrôleur centralisé pour la gestion des prescriptions médicamenteuses
/// Unifie tous les points d'entrée de prescription :
/// - Consultation classique
/// - Hospitalisation
/// - Prescription directe (fiche patient)
/// </summary>
[ApiController]
[Route("api/prescription")]
[Authorize]
public class PrescriptionController : ControllerBase
{
    private readonly IPrescriptionService _prescriptionService;
    private readonly ILogger<PrescriptionController> _logger;

    public PrescriptionController(
        IPrescriptionService prescriptionService,
        ILogger<PrescriptionController> logger)
    {
        _prescriptionService = prescriptionService;
        _logger = logger;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : null;
    }

    // ==================== Création ====================

    /// <summary>
    /// Crée une ordonnance générique
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> CreerOrdonnance([FromBody] CreateOrdonnanceRequest request)
    {
        var medecinId = GetCurrentUserId();
        if (!medecinId.HasValue)
            return Unauthorized(new { message = "Utilisateur non authentifié" });

        var result = await _prescriptionService.CreerOrdonnanceAsync(request, medecinId.Value);

        if (!result.Success)
        {
            return BadRequest(new { 
                success = false, 
                message = result.Message, 
                erreurs = result.Erreurs 
            });
        }

        return Ok(new { 
            success = true, 
            message = result.Message,
            idOrdonnance = result.IdOrdonnance,
            ordonnance = result.Ordonnance,
            alertes = result.Alertes
        });
    }

    /// <summary>
    /// Crée une ordonnance dans le contexte d'une consultation
    /// </summary>
    [HttpPost("consultation/{idConsultation}")]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> CreerOrdonnanceConsultation(
        int idConsultation, 
        [FromBody] CreateOrdonnanceConsultationRequest request)
    {
        var medecinId = GetCurrentUserId();
        if (!medecinId.HasValue)
            return Unauthorized(new { message = "Utilisateur non authentifié" });

        var result = await _prescriptionService.CreerOrdonnanceConsultationAsync(
            idConsultation, 
            request.Medicaments, 
            request.Notes, 
            medecinId.Value);

        if (!result.Success)
        {
            return BadRequest(new { 
                success = false, 
                message = result.Message, 
                erreurs = result.Erreurs 
            });
        }

        return Ok(new { 
            success = true, 
            message = result.Message,
            idOrdonnance = result.IdOrdonnance,
            ordonnance = result.Ordonnance,
            alertes = result.Alertes
        });
    }

    /// <summary>
    /// Crée une ordonnance dans le contexte d'une hospitalisation
    /// </summary>
    [HttpPost("hospitalisation/{idHospitalisation}")]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> CreerOrdonnanceHospitalisation(
        int idHospitalisation, 
        [FromBody] CreateOrdonnanceHospitalisationRequest request)
    {
        var medecinId = GetCurrentUserId();
        if (!medecinId.HasValue)
            return Unauthorized(new { message = "Utilisateur non authentifié" });

        var result = await _prescriptionService.CreerOrdonnanceHospitalisationAsync(
            idHospitalisation, 
            request.Medicaments, 
            request.Notes, 
            medecinId.Value);

        if (!result.Success)
        {
            return BadRequest(new { 
                success = false, 
                message = result.Message, 
                erreurs = result.Erreurs 
            });
        }

        return Ok(new { 
            success = true, 
            message = result.Message,
            idOrdonnance = result.IdOrdonnance,
            ordonnance = result.Ordonnance,
            alertes = result.Alertes
        });
    }

    /// <summary>
    /// Crée une ordonnance directe (hors consultation/hospitalisation)
    /// Utile pour les renouvellements ou prescriptions depuis la fiche patient
    /// </summary>
    [HttpPost("directe")]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> CreerOrdonnanceDirecte([FromBody] CreateOrdonnanceDirecteRequest request)
    {
        var medecinId = GetCurrentUserId();
        if (!medecinId.HasValue)
            return Unauthorized(new { message = "Utilisateur non authentifié" });

        var result = await _prescriptionService.CreerOrdonnanceDirecteAsync(
            request.IdPatient, 
            request.Medicaments, 
            request.Notes, 
            medecinId.Value,
            request.DureeValiditeJours,
            request.Renouvelable,
            request.NombreRenouvellements);

        if (!result.Success)
        {
            return BadRequest(new { 
                success = false, 
                message = result.Message, 
                erreurs = result.Erreurs 
            });
        }

        return Ok(new { 
            success = true, 
            message = result.Message,
            idOrdonnance = result.IdOrdonnance,
            ordonnance = result.Ordonnance,
            alertes = result.Alertes
        });
    }

    // ==================== Lecture ====================

    /// <summary>
    /// Récupère une ordonnance par son ID
    /// </summary>
    [HttpGet("{idOrdonnance}")]
    public async Task<IActionResult> GetOrdonnance(int idOrdonnance)
    {
        var ordonnance = await _prescriptionService.GetOrdonnanceAsync(idOrdonnance);

        if (ordonnance == null)
            return NotFound(new { message = "Ordonnance non trouvée" });

        return Ok(ordonnance);
    }

    /// <summary>
    /// Récupère l'ordonnance d'une consultation
    /// </summary>
    [HttpGet("consultation/{idConsultation}")]
    public async Task<IActionResult> GetOrdonnanceByConsultation(int idConsultation)
    {
        var ordonnance = await _prescriptionService.GetOrdonnanceByConsultationAsync(idConsultation);

        if (ordonnance == null)
            return NotFound(new { message = "Aucune ordonnance pour cette consultation" });

        return Ok(ordonnance);
    }

    /// <summary>
    /// Récupère les ordonnances d'un patient
    /// </summary>
    [HttpGet("patient/{idPatient}")]
    public async Task<IActionResult> GetOrdonnancesPatient(int idPatient)
    {
        var ordonnances = await _prescriptionService.GetOrdonnancesPatientAsync(idPatient);
        return Ok(ordonnances);
    }

    /// <summary>
    /// Récupère les ordonnances d'une hospitalisation
    /// </summary>
    [HttpGet("hospitalisation/{idHospitalisation}")]
    public async Task<IActionResult> GetOrdonnancesHospitalisation(int idHospitalisation)
    {
        var ordonnances = await _prescriptionService.GetOrdonnancesHospitalisationAsync(idHospitalisation);
        return Ok(ordonnances);
    }

    /// <summary>
    /// Recherche des ordonnances avec filtres
    /// </summary>
    [HttpGet("recherche")]
    public async Task<IActionResult> RechercherOrdonnances([FromQuery] FiltreOrdonnanceRequest filtre)
    {
        var (items, total) = await _prescriptionService.RechercherOrdonnancesAsync(filtre);

        return Ok(new
        {
            items,
            totalItems = total,
            page = filtre.Page,
            pageSize = filtre.PageSize,
            totalPages = (int)Math.Ceiling((double)total / filtre.PageSize)
        });
    }

    // ==================== Modification ====================

    /// <summary>
    /// Met à jour une ordonnance existante
    /// </summary>
    [HttpPut("{idOrdonnance}")]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> MettreAJourOrdonnance(
        int idOrdonnance, 
        [FromBody] UpdateOrdonnanceRequest request)
    {
        var medecinId = GetCurrentUserId();
        if (!medecinId.HasValue)
            return Unauthorized(new { message = "Utilisateur non authentifié" });

        var result = await _prescriptionService.MettreAJourOrdonnanceAsync(
            idOrdonnance, 
            request.Medicaments, 
            request.Notes, 
            medecinId.Value);

        if (!result.Success)
        {
            return BadRequest(new { 
                success = false, 
                message = result.Message, 
                erreurs = result.Erreurs 
            });
        }

        return Ok(new { 
            success = true, 
            message = result.Message,
            ordonnance = result.Ordonnance,
            alertes = result.Alertes
        });
    }

    /// <summary>
    /// Annule une ordonnance
    /// </summary>
    [HttpPost("{idOrdonnance}/annuler")]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> AnnulerOrdonnance(
        int idOrdonnance, 
        [FromBody] AnnulerOrdonnanceRequest request)
    {
        var medecinId = GetCurrentUserId();
        if (!medecinId.HasValue)
            return Unauthorized(new { message = "Utilisateur non authentifié" });

        if (string.IsNullOrWhiteSpace(request.Motif))
            return BadRequest(new { message = "Le motif d'annulation est obligatoire" });

        var success = await _prescriptionService.AnnulerOrdonnanceAsync(
            idOrdonnance, 
            request.Motif, 
            medecinId.Value);

        if (!success)
            return NotFound(new { message = "Ordonnance non trouvée ou impossible à annuler" });

        return Ok(new { success = true, message = "Ordonnance annulée avec succès" });
    }

    // ==================== Validation ====================

    /// <summary>
    /// Valide une prescription avant création (vérifie stock, interactions, etc.)
    /// </summary>
    [HttpPost("valider")]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> ValiderPrescription([FromBody] ValiderPrescriptionRequest request)
    {
        var result = await _prescriptionService.ValiderPrescriptionAsync(
            request.IdPatient, 
            request.Medicaments);

        return Ok(new
        {
            estValide = result.EstValide,
            erreurs = result.Erreurs,
            alertes = result.Alertes
        });
    }
}

// ==================== DTOs spécifiques au contrôleur ====================

/// <summary>
/// Requête pour créer une ordonnance dans une consultation
/// </summary>
public class CreateOrdonnanceConsultationRequest
{
    public string? Notes { get; set; }
    public List<MedicamentPrescriptionRequest> Medicaments { get; set; } = new();
}

/// <summary>
/// Requête pour créer une ordonnance dans une hospitalisation
/// </summary>
public class CreateOrdonnanceHospitalisationRequest
{
    public string? Notes { get; set; }
    public List<MedicamentPrescriptionRequest> Medicaments { get; set; } = new();
}

/// <summary>
/// Requête pour créer une ordonnance directe
/// </summary>
public class CreateOrdonnanceDirecteRequest
{
    public int IdPatient { get; set; }
    public string? Notes { get; set; }
    public List<MedicamentPrescriptionRequest> Medicaments { get; set; } = new();
    public int? DureeValiditeJours { get; set; }
    public bool? Renouvelable { get; set; }
    public int? NombreRenouvellements { get; set; }
}

/// <summary>
/// Requête pour mettre à jour une ordonnance
/// </summary>
public class UpdateOrdonnanceRequest
{
    public string? Notes { get; set; }
    public List<MedicamentPrescriptionRequest> Medicaments { get; set; } = new();
}

/// <summary>
/// Requête pour valider une prescription
/// </summary>
public class ValiderPrescriptionRequest
{
    public int IdPatient { get; set; }
    public List<MedicamentPrescriptionRequest> Medicaments { get; set; } = new();
}
