using Mediconnet_Backend.Controllers.Base;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Consultation;
using Mediconnet_Backend.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Controllers;

[Route("api/consultations")]
public class ConsultationQuestionnaireController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ConsultationQuestionnaireController> _logger;

    public ConsultationQuestionnaireController(ApplicationDbContext context, ILogger<ConsultationQuestionnaireController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("{consultationId:int}/questions")]
    public async Task<IActionResult> GetQuestions(int consultationId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var consultation = await _context.Consultations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdConsultation == consultationId);

            if (consultation == null)
                return NotFound(new { message = "Consultation introuvable" });

            if (!CanAccessConsultation(consultation, userId.Value))
                return Forbid();

            await EnsurePredefinedQuestionsLoadedAsync(consultationId);

            var items = await _context.ConsultationQuestions
                .AsNoTracking()
                .Where(cq => cq.ConsultationId == consultationId)
                .Include(cq => cq.Question)
                .Include(cq => cq.Reponses)
                .OrderBy(cq => cq.Question!.Ordre)
                .Select(cq => new ConsultationQuestionDto
                {
                    QuestionId = cq.QuestionId,
                    ConsultationQuestionId = cq.Id,
                    OrdreAffichage = cq.Question!.Ordre,
                    TexteQuestion = cq.Question!.TexteQuestion,
                    TypeQuestion = cq.Question.TypeQuestion,
                    EstPredefinie = cq.Question.Actif,
                    ValeurReponse = cq.Reponses != null && cq.Reponses.Any() ? cq.Reponses.First().ValeurReponse : null,
                    DateSaisie = cq.Reponses != null && cq.Reponses.Any() ? cq.Reponses.First().DateReponse : null
                })
                .ToListAsync();

            return Ok(new { success = true, data = items });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur GetQuestions");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    [HttpPost("{consultationId:int}/reponses")]
    public async Task<IActionResult> UpsertReponses(int consultationId, [FromBody] UpsertReponsesRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var consultation = await _context.Consultations
                .FirstOrDefaultAsync(c => c.IdConsultation == consultationId);

            if (consultation == null)
                return NotFound(new { message = "Consultation introuvable" });

            if (!CanAccessConsultation(consultation, userId.Value))
                return Forbid();

            await EnsurePredefinedQuestionsLoadedAsync(consultationId);

            if (request.Reponses == null || request.Reponses.Count == 0)
                return Ok(new { success = true, message = "Aucune réponse" });

            var questionIds = request.Reponses.Select(r => r.QuestionId).Distinct().ToList();

            // Récupérer les ConsultationQuestion correspondantes
            var consultationQuestions = await _context.ConsultationQuestions
                .Where(cq => cq.ConsultationId == consultationId && questionIds.Contains(cq.QuestionId))
                .Include(cq => cq.Reponses)
                .ToListAsync();

            var cqByQuestionId = consultationQuestions.ToDictionary(cq => cq.QuestionId, cq => cq);

            var invalid = questionIds.Except(cqByQuestionId.Keys).ToList();
            if (invalid.Count > 0)
                return BadRequest(new { success = false, message = "Certaines questions ne sont pas liées à la consultation", invalidQuestionIds = invalid });

            foreach (var r in request.Reponses)
            {
                if (!cqByQuestionId.TryGetValue(r.QuestionId, out var cq))
                    continue;

                var existingReponse = cq.Reponses?.FirstOrDefault();
                if (existingReponse != null)
                {
                    existingReponse.ValeurReponse = r.ValeurReponse;
                    existingReponse.DateReponse = DateTime.UtcNow;
                }
                else
                {
                    _context.Reponses.Add(new Reponse
                    {
                        ConsultationQuestionId = cq.Id,
                        ValeurReponse = r.ValeurReponse,
                        DateReponse = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Réponses enregistrées" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur UpsertReponses");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    [HttpPost("{consultationId:int}/questions")]
    public async Task<IActionResult> AddQuestionLibre(int consultationId, [FromBody] AddQuestionLibreRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            if (!IsMedecin())
                return Forbid();

            if (string.IsNullOrWhiteSpace(request.TexteQuestion))
                return BadRequest(new { success = false, message = "Texte de question obligatoire" });

            var consultation = await _context.Consultations
                .FirstOrDefaultAsync(c => c.IdConsultation == consultationId);

            if (consultation == null)
                return NotFound(new { message = "Consultation introuvable" });

            if (consultation.IdMedecin != userId.Value)
                return Forbid();

            await EnsurePredefinedQuestionsLoadedAsync(consultationId);

            var maxOrder = await _context.Questions
                .Select(q => (int?)q.Ordre)
                .MaxAsync() ?? 0;

            var question = new Question
            {
                TexteQuestion = request.TexteQuestion.Trim(),
                TypeQuestion = string.IsNullOrWhiteSpace(request.TypeQuestion) ? "texte" : request.TypeQuestion.Trim(),
                Ordre = maxOrder + 1,
                Actif = true
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            _context.ConsultationQuestions.Add(new ConsultationQuestion
            {
                ConsultationId = consultationId,
                QuestionId = question.Id
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                data = new ConsultationQuestionDto
                {
                    QuestionId = question.Id,
                    ConsultationQuestionId = 0,
                    OrdreAffichage = question.Ordre,
                    TexteQuestion = question.TexteQuestion,
                    TypeQuestion = question.TypeQuestion,
                    EstPredefinie = false
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur AddQuestionLibre");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    [HttpPost("{consultationId:int}/questionnaire")]
    public async Task<IActionResult> SaveQuestionnaireComplet(int consultationId, [FromBody] SaveQuestionnaireRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var consultation = await _context.Consultations
                .FirstOrDefaultAsync(c => c.IdConsultation == consultationId);

            if (consultation == null)
                return NotFound(new { message = "Consultation introuvable" });

            if (!CanAccessConsultation(consultation, userId.Value))
                return Forbid();

            foreach (var item in request.Reponses ?? new List<SaveReponseAvecQuestionItem>())
            {
                if (string.IsNullOrWhiteSpace(item.TexteQuestion))
                    continue;

                // Chercher ou créer la question
                Question? question = null;
                
                if (item.QuestionIdDb.HasValue && item.QuestionIdDb.Value > 0)
                {
                    question = await _context.Questions.FindAsync(item.QuestionIdDb.Value);
                }

                if (question == null)
                {
                    // Chercher par texte exact
                    question = await _context.Questions
                        .FirstOrDefaultAsync(q => q.TexteQuestion == item.TexteQuestion);
                }

                if (question == null)
                {
                    // Créer la question
                    var maxOrder = await _context.Questions.Select(q => (int?)q.Ordre).MaxAsync() ?? 0;
                    question = new Question
                    {
                        TexteQuestion = item.TexteQuestion.Trim(),
                        TypeQuestion = item.TypeQuestion ?? "texte",
                        Ordre = maxOrder + 1,
                        Actif = true
                    };
                    _context.Questions.Add(question);
                    await _context.SaveChangesAsync();
                }

                // S'assurer que la question est liée à la consultation
                var consultationQuestion = await _context.ConsultationQuestions
                    .Include(cq => cq.Reponses)
                    .FirstOrDefaultAsync(cq => cq.ConsultationId == consultationId && cq.QuestionId == question.Id);

                if (consultationQuestion == null)
                {
                    consultationQuestion = new ConsultationQuestion
                    {
                        ConsultationId = consultationId,
                        QuestionId = question.Id
                    };
                    _context.ConsultationQuestions.Add(consultationQuestion);
                    await _context.SaveChangesAsync();
                }

                // Upsert la réponse via ConsultationQuestion
                var reponse = consultationQuestion.Reponses?.FirstOrDefault();

                if (reponse != null)
                {
                    reponse.ValeurReponse = item.ValeurReponse;
                    reponse.DateReponse = DateTime.UtcNow;
                }
                else
                {
                    _context.Reponses.Add(new Reponse
                    {
                        ConsultationQuestionId = consultationQuestion.Id,
                        ValeurReponse = item.ValeurReponse,
                        DateReponse = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Questionnaire enregistré" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur SaveQuestionnaireComplet");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    private bool CanAccessConsultation(Consultation consultation, int userId)
    {
        if (IsMedecin()) return consultation.IdMedecin == userId;
        if (IsPatient()) return consultation.IdPatient == userId;
        if (IsAdmin()) return true;
        return false;
    }

    private async Task EnsurePredefinedQuestionsLoadedAsync(int consultationId)
    {
        // Charger les questions actives (prédéfinies)
        var predefinedIds = await _context.Questions
            .AsNoTracking()
            .Where(q => q.Actif)
            .OrderBy(q => q.Ordre)
            .Select(q => q.Id)
            .ToListAsync();

        if (predefinedIds.Count == 0)
            return;

        var existing = await _context.ConsultationQuestions
            .Where(cq => cq.ConsultationId == consultationId)
            .ToListAsync();

        var existingIds = existing.Select(e => e.QuestionId).ToHashSet();

        var missing = predefinedIds.Where(id => !existingIds.Contains(id)).ToList();
        if (missing.Count == 0)
            return;

        foreach (var qId in missing)
        {
            _context.ConsultationQuestions.Add(new ConsultationQuestion
            {
                ConsultationId = consultationId,
                QuestionId = qId
            });
        }

        await _context.SaveChangesAsync();
    }
}
