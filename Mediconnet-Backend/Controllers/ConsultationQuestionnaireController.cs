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
                .OrderBy(cq => cq.OrdreAffichage)
                .Select(cq => new ConsultationQuestionDto
                {
                    QuestionId = cq.QuestionId,
                    OrdreAffichage = cq.OrdreAffichage,
                    TexteQuestion = cq.Question!.TexteQuestion,
                    TypeQuestion = cq.Question.TypeQuestion,
                    EstPredefinie = cq.Question.EstPredefinie
                })
                .ToListAsync();

            var reponses = await _context.Reponses
                .AsNoTracking()
                .Where(r => r.ConsultationId == consultationId)
                .ToListAsync();

            var byQuestionId = reponses.ToDictionary(r => r.QuestionId, r => r);

            foreach (var item in items)
            {
                if (byQuestionId.TryGetValue(item.QuestionId, out var rep))
                {
                    item.ValeurReponse = rep.ValeurReponse;
                    item.RempliPar = rep.RempliPar;
                    item.DateSaisie = rep.DateSaisie;
                }
            }

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

            var rempliPar = IsMedecin() ? "medecin" : (IsPatient() ? "patient" : "medecin");

            await EnsurePredefinedQuestionsLoadedAsync(consultationId);

            if (request.Reponses == null || request.Reponses.Count == 0)
                return Ok(new { success = true, message = "Aucune réponse" });

            var questionIds = request.Reponses.Select(r => r.QuestionId).Distinct().ToList();

            var allowedQuestionIds = await _context.ConsultationQuestions
                .AsNoTracking()
                .Where(cq => cq.ConsultationId == consultationId)
                .Select(cq => cq.QuestionId)
                .ToListAsync();

            var invalid = questionIds.Except(allowedQuestionIds).ToList();
            if (invalid.Count > 0)
                return BadRequest(new { success = false, message = "Certaines questions ne sont pas liées à la consultation", invalidQuestionIds = invalid });

            var existing = await _context.Reponses
                .Where(r => r.ConsultationId == consultationId && questionIds.Contains(r.QuestionId))
                .ToListAsync();

            var existingByQ = existing.ToDictionary(r => r.QuestionId, r => r);

            foreach (var r in request.Reponses)
            {
                if (existingByQ.TryGetValue(r.QuestionId, out var rep))
                {
                    rep.ValeurReponse = r.ValeurReponse;
                    rep.RempliPar = rempliPar;
                    rep.DateSaisie = DateTime.UtcNow;
                }
                else
                {
                    _context.Reponses.Add(new Reponse
                    {
                        ConsultationId = consultationId,
                        QuestionId = r.QuestionId,
                        ValeurReponse = r.ValeurReponse,
                        RempliPar = rempliPar,
                        DateSaisie = DateTime.UtcNow
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

            var maxOrder = await _context.ConsultationQuestions
                .Where(cq => cq.ConsultationId == consultationId)
                .Select(cq => (int?)cq.OrdreAffichage)
                .MaxAsync() ?? 0;

            var question = new Question
            {
                TexteQuestion = request.TexteQuestion.Trim(),
                TypeQuestion = string.IsNullOrWhiteSpace(request.TypeQuestion) ? "texte" : request.TypeQuestion.Trim(),
                EstPredefinie = false,
                CreatedBy = userId.Value,
                CreatedAt = DateTime.UtcNow
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            _context.ConsultationQuestions.Add(new ConsultationQuestion
            {
                ConsultationId = consultationId,
                QuestionId = question.Id,
                OrdreAffichage = maxOrder + 1
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                data = new ConsultationQuestionDto
                {
                    QuestionId = question.Id,
                    OrdreAffichage = maxOrder + 1,
                    TexteQuestion = question.TexteQuestion,
                    TypeQuestion = question.TypeQuestion,
                    EstPredefinie = question.EstPredefinie
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

            var rempliPar = IsMedecin() ? "medecin" : (IsPatient() ? "patient" : "medecin");

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
                    question = new Question
                    {
                        TexteQuestion = item.TexteQuestion.Trim(),
                        TypeQuestion = item.TypeQuestion ?? "texte",
                        EstPredefinie = false,
                        CreatedBy = userId.Value,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Questions.Add(question);
                    await _context.SaveChangesAsync();
                }

                // S'assurer que la question est liée à la consultation
                var consultationQuestion = await _context.ConsultationQuestions
                    .FirstOrDefaultAsync(cq => cq.ConsultationId == consultationId && cq.QuestionId == question.Id);

                if (consultationQuestion == null)
                {
                    var maxOrder = await _context.ConsultationQuestions
                        .Where(cq => cq.ConsultationId == consultationId)
                        .Select(cq => (int?)cq.OrdreAffichage)
                        .MaxAsync() ?? 0;

                    _context.ConsultationQuestions.Add(new ConsultationQuestion
                    {
                        ConsultationId = consultationId,
                        QuestionId = question.Id,
                        OrdreAffichage = maxOrder + 1
                    });
                    await _context.SaveChangesAsync();
                }

                // Upsert la réponse
                var reponse = await _context.Reponses
                    .FirstOrDefaultAsync(r => r.ConsultationId == consultationId && r.QuestionId == question.Id);

                if (reponse != null)
                {
                    reponse.ValeurReponse = item.ValeurReponse;
                    reponse.RempliPar = rempliPar;
                    reponse.DateSaisie = DateTime.UtcNow;
                }
                else
                {
                    _context.Reponses.Add(new Reponse
                    {
                        ConsultationId = consultationId,
                        QuestionId = question.Id,
                        ValeurReponse = item.ValeurReponse,
                        RempliPar = rempliPar,
                        DateSaisie = DateTime.UtcNow
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
        var predefinedIds = await _context.Questions
            .AsNoTracking()
            .Where(q => q.EstPredefinie)
            .OrderBy(q => q.Id)
            .Select(q => q.Id)
            .ToListAsync();

        if (predefinedIds.Count == 0)
            return;

        var existing = await _context.ConsultationQuestions
            .Where(cq => cq.ConsultationId == consultationId)
            .ToListAsync();

        var existingIds = existing.Select(e => e.QuestionId).ToHashSet();
        var maxOrder = existing.Count == 0 ? 0 : existing.Max(e => e.OrdreAffichage);

        var missing = predefinedIds.Where(id => !existingIds.Contains(id)).ToList();
        if (missing.Count == 0)
            return;

        foreach (var qId in missing)
        {
            maxOrder++;
            _context.ConsultationQuestions.Add(new ConsultationQuestion
            {
                ConsultationId = consultationId,
                QuestionId = qId,
                OrdreAffichage = maxOrder
            });
        }

        await _context.SaveChangesAsync();
    }
}
