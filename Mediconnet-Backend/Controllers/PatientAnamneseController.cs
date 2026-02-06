using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Mediconnet_Backend.Controllers;

[ApiController]
[Route("api/patient/anamnese")]
[Authorize]
public class PatientAnamneseController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PatientAnamneseController> _logger;

    public PatientAnamneseController(ApplicationDbContext context, ILogger<PatientAnamneseController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Récupérer les questions d'anamnèse pour une consultation
    /// </summary>
    [HttpGet("questions/{consultationId}")]
    public async Task<IActionResult> GetQuestions(int consultationId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            // Vérifier que la consultation appartient au patient
            var consultation = await _context.Consultations
                .FirstOrDefaultAsync(c => c.IdConsultation == consultationId && c.IdPatient == userId.Value);

            if (consultation == null)
                return NotFound(new { message = "Consultation introuvable" });

            return Ok(await GetQuestionsForConsultation(consultationId));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetQuestions: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des questions" });
        }
    }

    /// <summary>
    /// Récupérer les questions d'anamnèse par ID de RDV (crée la consultation si nécessaire)
    /// </summary>
    [HttpGet("questions-rdv/{rdvId}")]
    public async Task<IActionResult> GetQuestionsByRdv(int rdvId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            // Vérifier que le RDV appartient au patient
            var rdv = await _context.RendezVous
                .Include(r => r.Medecin)
                .FirstOrDefaultAsync(r => r.IdRendezVous == rdvId && r.IdPatient == userId.Value);

            if (rdv == null)
                return NotFound(new { message = "Rendez-vous introuvable" });

            // Chercher une consultation existante liée au RDV
            var consultation = await _context.Consultations
                .FirstOrDefaultAsync(c => c.IdRendezVous == rdvId);

            // Si pas de consultation, en créer une
            if (consultation == null)
            {
                consultation = new Consultation
                {
                    IdPatient = userId.Value,
                    IdMedecin = rdv.IdMedecin,
                    IdRendezVous = rdvId,
                    DateHeure = rdv.DateHeure,
                    Statut = "en_attente"
                };
                _context.Consultations.Add(consultation);
                await _context.SaveChangesAsync();
            }

            // Déterminer si c'est une première consultation (même logique que côté médecin)
            // C'est une première consultation si le patient n'a aucune consultation terminée avec ce médecin
            var isPremiereConsultation = !await _context.Consultations
                .AnyAsync(c => c.IdPatient == userId.Value && 
                              c.IdMedecin == rdv.IdMedecin && 
                              c.Statut == "terminee" &&
                              c.IdConsultation != consultation.IdConsultation);

            // Récupérer la spécialité du médecin
            var medecin = await _context.Medecins
                .Where(m => m.IdUser == rdv.IdMedecin)
                .Select(m => new { m.IdSpecialite })
                .FirstOrDefaultAsync();

            var response = await GetQuestionsForConsultation(consultation.IdConsultation);
            response.ConsultationId = consultation.IdConsultation;
            response.IsPremiereConsultation = isPremiereConsultation;
            response.SpecialiteId = medecin?.IdSpecialite ?? 1;
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetQuestionsByRdv: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des questions" });
        }
    }

    private async Task<AnamneseQuestionsResponse> GetQuestionsForConsultation(int consultationId)
    {
        // Récupérer les questions actives
        var questions = await _context.Questions
            .Where(q => q.Actif)
            .OrderBy(q => q.Ordre)
            .Select(q => new AnamneseQuestionDto
            {
                Id = q.Id,
                Texte = q.TexteQuestion,
                Type = q.TypeQuestion ?? "text",
                Obligatoire = q.Obligatoire,
                Ordre = q.Ordre
            })
            .ToListAsync();

        // Récupérer les réponses existantes
        var consultationQuestions = await _context.ConsultationQuestions
            .Include(cq => cq.Reponses)
            .Where(cq => cq.ConsultationId == consultationId)
            .ToListAsync();

        var existingReponses = consultationQuestions.ToDictionary(
            cq => cq.QuestionId,
            cq => cq.Reponses?.OrderByDescending(r => r.DateReponse).FirstOrDefault()?.ValeurReponse ?? ""
        );

        return new AnamneseQuestionsResponse
        {
            Questions = questions,
            ExistingReponses = existingReponses,
            ConsultationId = consultationId
        };
    }

    /// <summary>
    /// Enregistrer les réponses d'anamnèse du patient
    /// </summary>
    [HttpPost("reponses")]
    public async Task<IActionResult> SaveReponses([FromBody] SaveAnamneseRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            // Vérifier que la consultation appartient au patient
            var consultation = await _context.Consultations
                .FirstOrDefaultAsync(c => c.IdConsultation == request.ConsultationId && c.IdPatient == userId.Value);

            if (consultation == null)
                return NotFound(new { message = "Consultation introuvable" });

            // Construire l'anamnèse à partir des réponses
            var anamneseLines = new List<string>();

            foreach (var reponse in request.Reponses)
            {
                // Récupérer ou créer la liaison consultation-question
                var consultationQuestion = await _context.ConsultationQuestions
                    .Include(cq => cq.Question)
                    .FirstOrDefaultAsync(cq => cq.ConsultationId == request.ConsultationId && cq.QuestionId == reponse.QuestionId);

                if (consultationQuestion == null)
                {
                    consultationQuestion = new ConsultationQuestion
                    {
                        ConsultationId = request.ConsultationId,
                        QuestionId = reponse.QuestionId
                    };
                    _context.ConsultationQuestions.Add(consultationQuestion);
                    await _context.SaveChangesAsync();
                }

                // Ajouter ou mettre à jour la réponse
                var existingReponse = await _context.Reponses
                    .FirstOrDefaultAsync(r => r.ConsultationQuestionId == consultationQuestion.Id);

                if (existingReponse != null)
                {
                    existingReponse.ValeurReponse = reponse.Reponse;
                    existingReponse.DateReponse = DateTime.UtcNow;
                }
                else
                {
                    _context.Reponses.Add(new Reponse
                    {
                        ConsultationQuestionId = consultationQuestion.Id,
                        ValeurReponse = reponse.Reponse,
                        DateReponse = DateTime.UtcNow
                    });
                }

                // Ajouter à l'anamnèse textuelle
                var question = consultationQuestion.Question ?? await _context.Questions.FindAsync(reponse.QuestionId);
                if (question != null && !string.IsNullOrEmpty(reponse.Reponse))
                {
                    anamneseLines.Add($"- {question.TexteQuestion}: {reponse.Reponse}");
                }
            }

            // Mettre à jour le champ Anamnese de la consultation
            if (anamneseLines.Any())
            {
                consultation.Anamnese = string.Join("\n", anamneseLines);
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Réponses enregistrées avec succès" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur SaveReponses: {ex.Message}");
            return StatusCode(500, new { success = false, message = "Erreur lors de l'enregistrement des réponses" });
        }
    }
}

// DTOs
public class AnamneseQuestionDto
{
    public int Id { get; set; }
    public string Texte { get; set; } = string.Empty;
    public string Type { get; set; } = "text";
    public bool Obligatoire { get; set; }
    public string? Placeholder { get; set; }
    public List<string>? Options { get; set; }
    public int? ScaleMin { get; set; }
    public int? ScaleMax { get; set; }
    public int Ordre { get; set; }
}

public class AnamneseQuestionsResponse
{
    public List<AnamneseQuestionDto> Questions { get; set; } = new();
    public Dictionary<int, string>? ExistingReponses { get; set; }
    public int? ConsultationId { get; set; }
    public bool IsPremiereConsultation { get; set; }
    public int SpecialiteId { get; set; }
}

public class AnamneseReponseDto
{
    public int QuestionId { get; set; }
    public string Reponse { get; set; } = string.Empty;
}

public class SaveAnamneseRequest
{
    public int ConsultationId { get; set; }
    public List<AnamneseReponseDto> Reponses { get; set; } = new();
}
