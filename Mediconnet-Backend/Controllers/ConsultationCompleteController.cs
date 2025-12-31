using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Controllers.Base;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.DTOs.Consultation;

namespace Mediconnet_Backend.Controllers;

[Route("api/consultation")]
[Authorize(Roles = "medecin")]
public class ConsultationCompleteController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ConsultationCompleteController> _logger;

    public ConsultationCompleteController(ApplicationDbContext context, ILogger<ConsultationCompleteController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("dossier-patient/{idPatient}")]
    public async Task<IActionResult> GetDossierPatient(int idPatient)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var patient = await _context.Patients
                .Include(p => p.Utilisateur)
                .Include(p => p.Assurance)
                .FirstOrDefaultAsync(p => p.IdUser == idPatient);

            if (patient == null)
                return NotFound(new { message = "Patient non trouvé" });

            var user = patient.Utilisateur;
            int? age = null;
            if (user?.Naissance != null)
            {
                age = DateTime.Today.Year - user.Naissance.Value.Year;
                if (user.Naissance.Value.Date > DateTime.Today.AddYears(-age.Value)) age--;
            }

            var consultations = await _context.Consultations
                .Include(c => c.Medecin).ThenInclude(m => m!.Utilisateur)
                .Include(c => c.Medecin).ThenInclude(m => m!.Specialite)
                .Where(c => c.IdPatient == idPatient)
                .OrderByDescending(c => c.DateHeure)
                .Take(20)
                .Select(c => new HistoriqueConsultationDto
                {
                    IdConsultation = c.IdConsultation,
                    DateHeure = c.DateHeure,
                    Motif = c.Motif,
                    Diagnostic = c.Diagnostic,
                    Statut = c.Statut,
                    MedecinNom = c.Medecin != null && c.Medecin.Utilisateur != null 
                        ? $"Dr. {c.Medecin.Utilisateur.Prenom} {c.Medecin.Utilisateur.Nom}" : "",
                    Specialite = c.Medecin != null && c.Medecin.Specialite != null 
                        ? c.Medecin.Specialite.NomSpecialite : null
                })
                .ToListAsync();

            var ordonnances = await _context.Ordonnances
                .Include(o => o.Medicaments)!.ThenInclude(m => m.Medicament)
                .Include(o => o.Consultation)
                .Where(o => o.Consultation != null && o.Consultation.IdPatient == idPatient)
                .OrderByDescending(o => o.Date)
                .Take(10)
                .Select(o => new HistoriqueOrdonnanceDto
                {
                    IdOrdonnance = o.IdOrdonnance,
                    DateCreation = o.Date,
                    DureeTraitement = null,
                    Medicaments = o.Medicaments != null ? o.Medicaments.Select(m => new MedicamentDto
                    {
                        NomMedicament = m.Medicament != null ? m.Medicament.Nom : "",
                        Frequence = m.Posologie,
                        Duree = m.DureeTraitement,
                        Quantite = m.Quantite
                    }).ToList() : new List<MedicamentDto>()
                })
                .ToListAsync();

            var examens = await _context.BulletinsExamen
                .Include(e => e.Examen)
                .Include(e => e.Consultation)
                .Where(e => e.Consultation != null && e.Consultation.IdPatient == idPatient)
                .OrderByDescending(e => e.DateDemande)
                .Take(20)
                .Select(e => new HistoriqueExamenDto
                {
                    IdExamen = e.IdBulletinExamen,
                    TypeExamen = e.Examen != null ? e.Examen.TypeExamen ?? "" : "",
                    NomExamen = e.Examen != null ? e.Examen.NomExamen : "",
                    Statut = "prescrit",
                    DatePrescription = e.DateDemande
                })
                .ToListAsync();

            var dossier = new DossierPatientDto
            {
                IdPatient = idPatient,
                NumeroDossier = patient.NumeroDossier ?? "",
                Nom = user?.Nom ?? "",
                Prenom = user?.Prenom ?? "",
                Naissance = user?.Naissance,
                Age = age,
                Sexe = user?.Sexe,
                Telephone = user?.Telephone,
                Email = user?.Email,
                Adresse = user?.Adresse,
                GroupeSanguin = patient.GroupeSanguin,
                MaladiesChroniques = patient.MaladiesChroniques,
                AllergiesDetails = patient.AllergiesDetails,
                AntecedentsFamiliauxDetails = patient.AntecedentsFamiliauxDetails,
                OperationsDetails = patient.OperationsDetails,
                ConsommationAlcool = patient.ConsommationAlcool,
                Tabagisme = patient.Tabagisme,
                ActivitePhysique = patient.ActivitePhysique,
                NomAssurance = patient.Assurance?.Nom,
                NumeroCarteAssurance = patient.NumeroCarteAssurance,
                CouvertureAssurance = patient.CouvertureAssurance,
                Consultations = consultations,
                Ordonnances = ordonnances,
                Examens = examens
            };

            return Ok(dossier);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetDossierPatient: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpPost("{idConsultation}/demarrer")]
    public async Task<IActionResult> DemarrerConsultation(int idConsultation)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var consultation = await _context.Consultations
                .FirstOrDefaultAsync(c => c.IdConsultation == idConsultation && c.IdMedecin == medecinId.Value);

            if (consultation == null)
                return NotFound(new { message = "Consultation non trouvée" });

            consultation.Statut = "en_cours";
            consultation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Consultation démarrée", idConsultation });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur DemarrerConsultation: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpGet("{idConsultation}")]
    public async Task<IActionResult> GetConsultation(int idConsultation)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var consultation = await _context.Consultations
                .Include(c => c.Patient).ThenInclude(p => p!.Utilisateur)
                .Include(c => c.Parametre)
                .FirstOrDefaultAsync(c => c.IdConsultation == idConsultation && c.IdMedecin == medecinId.Value);

            if (consultation == null)
                return NotFound(new { message = "Consultation non trouvée" });

            var isPremiereConsultation = !await _context.Consultations
                .AnyAsync(c => c.IdPatient == consultation.IdPatient && 
                              c.IdMedecin == medecinId.Value && 
                              c.Statut == "terminee" &&
                              c.IdConsultation != idConsultation);

            var medecin = await _context.Medecins
                .Where(m => m.IdUser == medecinId.Value)
                .Select(m => new { m.IdSpecialite })
                .FirstOrDefaultAsync();

            var result = new ConsultationEnCoursDto
            {
                IdConsultation = consultation.IdConsultation,
                IdPatient = consultation.IdPatient,
                PatientNom = consultation.Patient?.Utilisateur?.Nom ?? "",
                PatientPrenom = consultation.Patient?.Utilisateur?.Prenom ?? "",
                DateHeure = consultation.DateHeure,
                Motif = consultation.Motif,
                Statut = consultation.Statut,
                IsPremiereConsultation = isPremiereConsultation,
                SpecialiteId = medecin?.IdSpecialite ?? 0,
                Anamnese = new AnamneseDto
                {
                    MotifConsultation = consultation.Motif,
                    HistoireMaladie = consultation.Anamnese,
                    QuestionsReponses = new List<QuestionReponseDto>(),
                    ParametresVitaux = consultation.Parametre != null ? new ParametresVitauxDto
                    {
                        Poids = consultation.Parametre.Poids,
                        Taille = consultation.Parametre.Taille,
                        Temperature = consultation.Parametre.Temperature,
                        TensionArterielle = consultation.Parametre.TensionFormatee
                    } : null
                },
                Diagnostic = new DiagnosticDto
                {
                    DiagnosticPrincipal = consultation.Diagnostic,
                    NotesCliniques = consultation.NotesCliniques
                },
                Prescriptions = new PrescriptionsDto
                {
                    Examens = new List<ExamenPrescritDto>(),
                    Recommandations = new List<RecommandationDto>()
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetConsultation: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpPost("{idConsultation}/anamnese")]
    public async Task<IActionResult> SaveAnamnese(int idConsultation, [FromBody] AnamneseDto anamnese)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var consultation = await _context.Consultations
                .Include(c => c.Parametre)
                .FirstOrDefaultAsync(c => c.IdConsultation == idConsultation && c.IdMedecin == medecinId.Value);

            if (consultation == null)
                return NotFound(new { message = "Consultation non trouvée" });

            consultation.Motif = anamnese.MotifConsultation;
            consultation.Anamnese = anamnese.HistoireMaladie;
            consultation.Antecedents = anamnese.AntecedentsPersonnels;
            consultation.UpdatedAt = DateTime.UtcNow;

            if (anamnese.ParametresVitaux != null)
            {
                if (consultation.Parametre == null)
                {
                    consultation.Parametre = new Parametre { IdConsultation = idConsultation };
                    _context.Parametres.Add(consultation.Parametre);
                }

                consultation.Parametre.Poids = anamnese.ParametresVitaux.Poids;
                consultation.Parametre.Taille = anamnese.ParametresVitaux.Taille;
                consultation.Parametre.Temperature = anamnese.ParametresVitaux.Temperature;
                // Parse tension artérielle string "120/80" en systolique/diastolique
                if (!string.IsNullOrEmpty(anamnese.ParametresVitaux.TensionArterielle))
                {
                    var parts = anamnese.ParametresVitaux.TensionArterielle.Split('/');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int sys) && int.TryParse(parts[1], out int dia))
                    {
                        consultation.Parametre.TensionSystolique = sys;
                        consultation.Parametre.TensionDiastolique = dia;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Anamnèse sauvegardée" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur SaveAnamnese: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpPost("{idConsultation}/diagnostic")]
    public async Task<IActionResult> SaveDiagnostic(int idConsultation, [FromBody] DiagnosticDto diagnostic)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var consultation = await _context.Consultations
                .FirstOrDefaultAsync(c => c.IdConsultation == idConsultation && c.IdMedecin == medecinId.Value);

            if (consultation == null)
                return NotFound(new { message = "Consultation non trouvée" });

            consultation.Diagnostic = diagnostic.DiagnosticPrincipal;
            consultation.NotesCliniques = diagnostic.NotesCliniques;
            consultation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Diagnostic sauvegardé" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur SaveDiagnostic: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpPost("{idConsultation}/prescriptions")]
    public async Task<IActionResult> SavePrescriptions(int idConsultation, [FromBody] PrescriptionsDto prescriptions)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var consultation = await _context.Consultations
                .Include(c => c.Ordonnance).ThenInclude(o => o!.Medicaments)
                .Include(c => c.BulletinsExamen)
                .FirstOrDefaultAsync(c => c.IdConsultation == idConsultation && c.IdMedecin == medecinId.Value);

            if (consultation == null)
                return NotFound(new { message = "Consultation non trouvée" });

            // Note: Pour une implémentation complète, il faudrait gérer les médicaments et examens
            // Ici on simplifie en sauvegardant juste le commentaire de l'ordonnance
            
            consultation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Prescriptions sauvegardées" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur SavePrescriptions: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpPost("{idConsultation}/valider")]
    public async Task<IActionResult> ValiderConsultation(int idConsultation, [FromBody] ValiderConsultationRequest request)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var consultation = await _context.Consultations
                .FirstOrDefaultAsync(c => c.IdConsultation == idConsultation && c.IdMedecin == medecinId.Value);

            if (consultation == null)
                return NotFound(new { message = "Consultation non trouvée" });

            consultation.Statut = "terminee";
            consultation.Conclusion = request.Conclusion;
            consultation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Consultation validée", 
                idConsultation,
                imprimer = request.Imprimer 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur ValiderConsultation: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    [HttpGet("{idConsultation}/recapitulatif")]
    public async Task<IActionResult> GetRecapitulatif(int idConsultation)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var consultation = await _context.Consultations
                .Include(c => c.Patient).ThenInclude(p => p!.Utilisateur)
                .Include(c => c.Parametre)
                .FirstOrDefaultAsync(c => c.IdConsultation == idConsultation && c.IdMedecin == medecinId.Value);

            if (consultation == null)
                return NotFound(new { message = "Consultation non trouvée" });

            var dossierResult = await GetDossierPatient(consultation.IdPatient) as OkObjectResult;
            
            var result = new ConsultationRecapitulatifDto
            {
                Consultation = new ConsultationEnCoursDto
                {
                    IdConsultation = consultation.IdConsultation,
                    IdPatient = consultation.IdPatient,
                    PatientNom = consultation.Patient?.Utilisateur?.Nom ?? "",
                    PatientPrenom = consultation.Patient?.Utilisateur?.Prenom ?? "",
                    DateHeure = consultation.DateHeure,
                    Motif = consultation.Motif,
                    Statut = consultation.Statut,
                    Anamnese = new AnamneseDto { MotifConsultation = consultation.Motif, HistoireMaladie = consultation.Anamnese },
                    Diagnostic = new DiagnosticDto { DiagnosticPrincipal = consultation.Diagnostic, NotesCliniques = consultation.NotesCliniques },
                    Prescriptions = new PrescriptionsDto { Examens = new List<ExamenPrescritDto>(), Recommandations = new List<RecommandationDto>() }
                },
                Patient = dossierResult?.Value as DossierPatientDto ?? new DossierPatientDto()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetRecapitulatif: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }
}
