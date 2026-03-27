using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.DTOs.Consultation;
using Mediconnet_Backend.Services;
using System.Security.Claims;

namespace Mediconnet_Backend.Controllers;

[ApiController]
[Route("api/programmation-intervention")]
[Authorize]
public class ProgrammationInterventionController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ProgrammationInterventionService _programmationService;
    private readonly ILogger<ProgrammationInterventionController> _logger;

    // IDs des spécialités chirurgicales autorisées
    private static readonly int[] CHIRURGIE_SPECIALITE_IDS = { 5, 6, 12, 21, 26, 31, 39, 41 };

    public ProgrammationInterventionController(
        ApplicationDbContext context,
        ProgrammationInterventionService programmationService,
        ILogger<ProgrammationInterventionController> logger)
    {
        _context = context;
        _programmationService = programmationService;
        _logger = logger;
    }

    /// <summary>
    /// Récupérer toutes les programmations du chirurgien connecté
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> GetMyProgrammations([FromQuery] string? statut = null)
    {
        var medecinId = GetCurrentUserId();
        if (!medecinId.HasValue) return Unauthorized();

        var query = _context.ProgrammationsInterventions
            .Include(p => p.Patient)
                .ThenInclude(pat => pat.Utilisateur)
            .Where(p => p.IdChirurgien == medecinId.Value);

        if (!string.IsNullOrEmpty(statut))
        {
            query = query.Where(p => p.Statut == statut);
        }

        var programmations = await query
            .OrderByDescending(p => p.DatePrevue ?? p.CreatedAt)
            .Select(p => new ProgrammationInterventionListDto
            {
                IdProgrammation = p.IdProgrammation,
                PatientNom = p.Patient.Utilisateur != null ? p.Patient.Utilisateur.Nom : null,
                PatientPrenom = p.Patient.Utilisateur != null ? p.Patient.Utilisateur.Prenom : null,
                IndicationOperatoire = p.IndicationOperatoire,
                TechniquePrevue = p.TechniquePrevue,
                DatePrevue = p.DatePrevue,
                HeureDebut = p.HeureDebut,
                DureeEstimee = p.DureeEstimee,
                Statut = p.Statut,
                TypeIntervention = p.TypeIntervention,
                ClassificationAsa = p.ClassificationAsa,
                ConsentementEclaire = p.ConsentementEclaire
            })
            .ToListAsync();

        return Ok(programmations);
    }

    /// <summary>
    /// Récupérer une programmation par ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> GetProgrammation(int id)
    {
        var medecinId = GetCurrentUserId();
        if (!medecinId.HasValue) return Unauthorized();

        var programmation = await _context.ProgrammationsInterventions
            .Include(p => p.Patient)
                .ThenInclude(pat => pat.Utilisateur)
            .Include(p => p.Chirurgien)
                .ThenInclude(m => m.Utilisateur)
            .Include(p => p.Chirurgien)
                .ThenInclude(m => m.Specialite)
            .Include(p => p.Consultation)
            .FirstOrDefaultAsync(p => p.IdProgrammation == id);

        if (programmation == null)
            return NotFound(new { message = "Programmation non trouvée" });

        // Vérifier que c'est bien le chirurgien concerné
        if (programmation.IdChirurgien != medecinId.Value)
            return Forbid();

        var dto = MapToDto(programmation);
        return Ok(dto);
    }

    /// <summary>
    /// Récupérer la programmation liée à une consultation
    /// </summary>
    [HttpGet("consultation/{idConsultation}")]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> GetByConsultation(int idConsultation)
    {
        var medecinId = GetCurrentUserId();
        if (!medecinId.HasValue) return Unauthorized();

        var programmation = await _context.ProgrammationsInterventions
            .Include(p => p.Patient)
                .ThenInclude(pat => pat.Utilisateur)
            .Include(p => p.Chirurgien)
                .ThenInclude(m => m.Utilisateur)
            .Include(p => p.Chirurgien)
                .ThenInclude(m => m.Specialite)
            .FirstOrDefaultAsync(p => p.IdConsultation == idConsultation);

        if (programmation == null)
            return Ok(new { exists = false });

        var dto = MapToDto(programmation);
        return Ok(new { exists = true, programmation = dto });
    }

    /// <summary>
    /// Créer une nouvelle programmation d'intervention
    /// Envoie des notifications email et bloque le créneau dans l'agenda
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> CreateProgrammation([FromBody] CreateProgrammationInterventionRequest request)
    {
        var medecinId = GetCurrentUserId();
        if (!medecinId.HasValue) return Unauthorized();

        var result = await _programmationService.CreateProgrammationAsync(medecinId.Value, request);

        if (!result.Success)
        {
            if (result.Message.Contains("non trouvé"))
                return NotFound(new { message = result.Message });
            if (result.Message.Contains("autorisé") || result.Message.Contains("chirurgiens"))
                return Forbid(result.Message);
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { 
            message = result.Message, 
            idProgrammation = result.IdProgrammation 
        });
    }

    /// <summary>
    /// Mettre à jour une programmation
    /// Met à jour le blocage de créneaux si la date/heure change
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> UpdateProgrammation(int id, [FromBody] UpdateProgrammationInterventionRequest request)
    {
        var medecinId = GetCurrentUserId();
        if (!medecinId.HasValue) return Unauthorized();

        var result = await _programmationService.UpdateProgrammationAsync(medecinId.Value, id, request);

        if (!result.Success)
        {
            if (result.Message.Contains("non trouvée"))
                return NotFound(new { message = result.Message });
            if (result.Message.Contains("autorisé"))
                return Forbid();
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Annuler une programmation
    /// Libère le créneau bloqué et envoie une notification au patient
    /// </summary>
    [HttpPost("{id}/annuler")]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> AnnulerProgrammation(int id, [FromBody] AnnulerProgrammationRequest request)
    {
        var medecinId = GetCurrentUserId();
        if (!medecinId.HasValue) return Unauthorized();

        var result = await _programmationService.AnnulerProgrammationAsync(medecinId.Value, id, request.Motif);

        if (!result.Success)
        {
            if (result.Message.Contains("non trouvée"))
                return NotFound(new { message = result.Message });
            if (result.Message.Contains("autorisé"))
                return Forbid();
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Valider le consentement éclairé
    /// </summary>
    [HttpPost("{id}/consentement")]
    [Authorize(Roles = "medecin")]
    public async Task<IActionResult> ValiderConsentement(int id)
    {
        var medecinId = GetCurrentUserId();
        if (!medecinId.HasValue) return Unauthorized();

        var programmation = await _context.ProgrammationsInterventions
            .FirstOrDefaultAsync(p => p.IdProgrammation == id);

        if (programmation == null)
            return NotFound(new { message = "Programmation non trouvée" });

        if (programmation.IdChirurgien != medecinId.Value)
            return Forbid();

        programmation.ConsentementEclaire = true;
        programmation.DateConsentement = DateTime.UtcNow;
        programmation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Consentement validé" });
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
            return userId;
        return null;
    }

    private static ProgrammationInterventionDto MapToDto(ProgrammationIntervention p)
    {
        return new ProgrammationInterventionDto
        {
            IdProgrammation = p.IdProgrammation,
            IdConsultation = p.IdConsultation,
            IdPatient = p.IdPatient,
            IdChirurgien = p.IdChirurgien,
            PatientNom = p.Patient?.Utilisateur?.Nom,
            PatientPrenom = p.Patient?.Utilisateur?.Prenom,
            ChirurgienNom = p.Chirurgien?.Utilisateur?.Nom,
            ChirurgienPrenom = p.Chirurgien?.Utilisateur?.Prenom,
            Specialite = p.Chirurgien?.Specialite?.NomSpecialite,
            TypeIntervention = p.TypeIntervention,
            ClassificationAsa = p.ClassificationAsa,
            RisqueOperatoire = p.RisqueOperatoire,
            ConsentementEclaire = p.ConsentementEclaire,
            DateConsentement = p.DateConsentement,
            IndicationOperatoire = p.IndicationOperatoire,
            TechniquePrevue = p.TechniquePrevue,
            DatePrevue = p.DatePrevue,
            HeureDebut = p.HeureDebut,
            DureeEstimee = p.DureeEstimee,
            NotesAnesthesie = p.NotesAnesthesie,
            BilanPreoperatoire = p.BilanPreoperatoire,
            InstructionsPatient = p.InstructionsPatient,
            Statut = p.Statut,
            MotifAnnulation = p.MotifAnnulation,
            Notes = p.Notes,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };
    }
}

public class AnnulerProgrammationRequest
{
    public string? Motif { get; set; }
}
