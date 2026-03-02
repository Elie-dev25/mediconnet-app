using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Controllers.Base;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Enums;
using Mediconnet_Backend.Core.Constants;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Core.Services;
using Mediconnet_Backend.DTOs.Consultation;
using Mediconnet_Backend.DTOs.Prescription;

namespace Mediconnet_Backend.Controllers;

[Route("api/consultation")]
[Authorize(Roles = "medecin")]
public class ConsultationCompleteController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ConsultationCompleteController> _logger;
    private readonly INotificationService _notificationService;
    private readonly IConsultationAuditService _auditService;
    private readonly IPrescriptionService _prescriptionService;

    public ConsultationCompleteController(
        ApplicationDbContext context, 
        ILogger<ConsultationCompleteController> logger,
        INotificationService notificationService,
        IConsultationAuditService auditService,
        IPrescriptionService prescriptionService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
        _auditService = auditService;
        _prescriptionService = prescriptionService;
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

            // Examens du patient (consultations ET hospitalisations)
            var examens = await _context.BulletinsExamen
                .Include(e => e.Examen)
                    .ThenInclude(ex => ex!.Specialite)
                        .ThenInclude(s => s!.Categorie)
                .Include(e => e.Consultation)
                .Include(e => e.Hospitalisation)
                .Where(e => 
                    (e.Consultation != null && e.Consultation.IdPatient == idPatient) ||
                    (e.Hospitalisation != null && e.Hospitalisation.IdPatient == idPatient))
                .OrderByDescending(e => e.DateDemande)
                .Take(20)
                .Select(e => new HistoriqueExamenDto
                {
                    IdExamen = e.IdBulletinExamen,
                    Categorie = e.Examen != null && e.Examen.Specialite != null && e.Examen.Specialite.Categorie != null 
                        ? e.Examen.Specialite.Categorie.Nom : "",
                    Specialite = e.Examen != null && e.Examen.Specialite != null 
                        ? e.Examen.Specialite.Nom : "",
                    NomExamen = e.Examen != null ? e.Examen.NomExamen : "",
                    Statut = e.Statut ?? "prescrit",
                    DatePrescription = e.DateDemande,
                    Resultats = e.ResultatTexte
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
                Nationalite = user?.Nationalite,
                RegionOrigine = user?.RegionOrigine,
                SituationMatrimoniale = user?.SituationMatrimoniale,
                Profession = patient.Profession,
                Ethnie = patient.Ethnie,
                NbEnfants = patient.NbEnfants,
                // Informations médicales
                GroupeSanguin = patient.GroupeSanguin,
                MaladiesChroniques = patient.MaladiesChroniques,
                AllergiesConnues = patient.AllergiesConnues,
                AllergiesDetails = patient.AllergiesDetails,
                AntecedentsFamiliaux = patient.AntecedentsFamiliaux,
                AntecedentsFamiliauxDetails = patient.AntecedentsFamiliauxDetails,
                OperationsChirurgicales = patient.OperationsChirurgicales,
                OperationsDetails = patient.OperationsDetails,
                // Habitudes de vie
                ConsommationAlcool = patient.ConsommationAlcool,
                FrequenceAlcool = patient.FrequenceAlcool,
                Tabagisme = patient.Tabagisme,
                ActivitePhysique = patient.ActivitePhysique,
                // Contact d'urgence
                PersonneContact = patient.PersonneContact,
                NumeroContact = patient.NumeroContact,
                // Assurance
                NomAssurance = patient.Assurance?.Nom,
                NumeroCarteAssurance = patient.NumeroCarteAssurance,
                CouvertureAssurance = patient.CouvertureAssurance,
                DateDebutValidite = patient.DateDebutValidite,
                DateFinValidite = patient.DateFinValidite,
                // Dates
                DateCreation = patient.DateCreation,
                // Historique
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

            // Valider la transition de statut
            var nouveauStatut = ConsultationStatut.EnCours.ToDbString();
            if (!Core.Services.StatutTransitionValidator.IsConsultationTransitionValid(consultation.Statut, nouveauStatut))
            {
                return BadRequest(new { 
                    message = Core.Services.StatutTransitionValidator.GetTransitionErrorMessage("consultation", consultation.Statut, nouveauStatut)
                });
            }

            var ancienStatut = consultation.Statut;
            consultation.Statut = nouveauStatut;
            consultation.DateDebutEffective = DateTime.UtcNow;
            consultation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Audit trail
            await _auditService.LogStatutChangeAsync(idConsultation, medecinId.Value, ancienStatut, nouveauStatut);

            _logger.LogInformation($"Consultation {idConsultation} démarrée par médecin {medecinId}");

            return Ok(new { message = "Consultation démarrée", idConsultation, dateDebut = consultation.DateDebutEffective });
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
                .Include(c => c.Ordonnance).ThenInclude(o => o!.Medicaments)!.ThenInclude(m => m.Medicament)
                .Include(c => c.BulletinsExamen).ThenInclude(b => b.Examen)
                .Include(c => c.ConsultationQuestions)!.ThenInclude(cq => cq.Question)
                .Include(c => c.ConsultationQuestions)!.ThenInclude(cq => cq.Reponses)
                .Include(c => c.ConsultationGynecologique)
                .FirstOrDefaultAsync(c => c.IdConsultation == idConsultation && c.IdMedecin == medecinId.Value);

            if (consultation == null)
                return NotFound(new { message = "Consultation non trouvée" });

            var isPremiereConsultation = !await _context.Consultations
                .AnyAsync(c => c.IdPatient == consultation.IdPatient && 
                              c.IdMedecin == medecinId.Value && 
                              c.Statut == ConsultationStatut.Terminee.ToDbString() &&
                              c.IdConsultation != idConsultation);

            var medecin = await _context.Medecins
                .Where(m => m.IdUser == medecinId.Value)
                .Select(m => new { m.IdSpecialite })
                .FirstOrDefaultAsync();

            // Mapper les questions-réponses
            var questionsReponses = consultation.ConsultationQuestions?.SelectMany(cq => 
                cq.Reponses?.Select(r => new QuestionReponseDto
                {
                    QuestionId = cq.QuestionId.ToString(),
                    Question = cq.Question?.TexteQuestion ?? "",
                    Reponse = r.ValeurReponse ?? ""
                }) ?? Enumerable.Empty<QuestionReponseDto>()
            ).ToList() ?? new List<QuestionReponseDto>();

            // Mapper l'ordonnance
            DTOs.Consultation.OrdonnanceDto? ordonnanceDto = null;
            if (consultation.Ordonnance != null)
            {
                ordonnanceDto = new DTOs.Consultation.OrdonnanceDto
                {
                    IdOrdonnance = consultation.Ordonnance.IdOrdonnance,
                    Notes = consultation.Ordonnance.Commentaire,
                    Medicaments = consultation.Ordonnance.Medicaments?.Select(m => new MedicamentDto
                    {
                        IdPrescription = m.IdPrescriptionMed,
                        IdMedicament = m.IdMedicament,
                        NomMedicament = m.Medicament?.Nom ?? "",
                        Dosage = m.Medicament?.Dosage,
                        Posologie = m.Posologie,
                        Frequence = m.Frequence,
                        Duree = m.DureeTraitement,
                        VoieAdministration = m.VoieAdministration,
                        FormePharmaceutique = m.FormePharmaceutique,
                        Instructions = m.Instructions,
                        Quantite = m.Quantite
                    }).ToList() ?? new List<MedicamentDto>()
                };
            }

            // Mapper les examens prescrits
            var examensPrescrits = consultation.BulletinsExamen?.Select(b => new ExamenPrescritDto
            {
                IdExamen = b.IdBulletinExamen,
                Categorie = b.Examen?.Specialite?.Categorie?.Nom ?? "",
                Specialite = b.Examen?.Specialite?.Nom ?? "",
                NomExamen = b.Examen?.NomExamen ?? "",
                Description = b.Examen?.Description,
                Urgence = false,
                Notes = b.Instructions,
                Disponible = b.Examen?.Disponible ?? true
            }).ToList() ?? new List<ExamenPrescritDto>();

            // Vérifier si les paramètres ont été pris par un infirmier
            var parametresPrisParInfirmier = consultation.Parametre?.EnregistrePar != null && 
                consultation.Parametre.EnregistrePar != medecinId.Value;
            string? infirmierNom = null;
            if (parametresPrisParInfirmier && consultation.Parametre?.UtilisateurEnregistrant != null)
            {
                infirmierNom = $"{consultation.Parametre.UtilisateurEnregistrant.Prenom} {consultation.Parametre.UtilisateurEnregistrant.Nom}";
            }

            // Récupérer les données du patient pour le récapitulatif
            var patient = consultation.Patient;
            
            // Récupérer les diagnostics précédents du patient (consultations terminées)
            var diagnosticsPrecedents = patient != null 
                ? await _context.Consultations
                    .Where(c => c.IdPatient == patient.IdUser 
                        && c.IdConsultation != idConsultation 
                        && c.Statut == ConsultationStatut.Terminee.ToDbString() 
                        && !string.IsNullOrEmpty(c.Diagnostic))
                    .OrderByDescending(c => c.DateHeure)
                    .Take(10)
                    .Include(c => c.Medecin)
                        .ThenInclude(m => m!.Utilisateur)
                    .Include(c => c.Medecin)
                        .ThenInclude(m => m!.Specialite)
                    .Select(c => new DiagnosticPrecedentDto
                    {
                        Date = c.DateHeure,
                        Diagnostic = c.Diagnostic!,
                        MedecinNom = c.Medecin != null && c.Medecin.Utilisateur != null ? c.Medecin.Utilisateur.Nom : "",
                        MedecinPrenom = c.Medecin != null && c.Medecin.Utilisateur != null ? c.Medecin.Utilisateur.Prenom : null,
                        Specialite = c.Medecin != null && c.Medecin.Specialite != null ? c.Medecin.Specialite.NomSpecialite : null
                    })
                    .ToListAsync()
                : new List<DiagnosticPrecedentDto>();
            
            var recapPatient = patient != null ? new RecapitulatifPatientDto
            {
                // Informations personnelles
                RegionOrigine = patient.Utilisateur?.RegionOrigine,
                SituationMatrimoniale = patient.Utilisateur?.SituationMatrimoniale,
                Profession = patient.Profession,
                NbEnfants = patient.NbEnfants,
                Ethnie = patient.Ethnie,
                // Informations médicales
                GroupeSanguin = patient.GroupeSanguin,
                MaladiesChroniques = patient.MaladiesChroniques,
                AllergiesConnues = patient.AllergiesConnues,
                AllergiesDetails = patient.AllergiesDetails,
                AntecedentsFamiliaux = patient.AntecedentsFamiliaux,
                AntecedentsFamiliauxDetails = patient.AntecedentsFamiliauxDetails,
                OperationsChirurgicales = patient.OperationsChirurgicales,
                OperationsDetails = patient.OperationsDetails,
                // Habitudes de vie
                ConsommationAlcool = patient.ConsommationAlcool,
                FrequenceAlcool = patient.FrequenceAlcool,
                Tabagisme = patient.Tabagisme,
                ActivitePhysique = patient.ActivitePhysique,
                // Diagnostics précédents
                DiagnosticsPrecedents = diagnosticsPrecedents
            } : null;

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
                EtapeActuelle = consultation.EtapeActuelle,
                // Étape 1: Anamnèse
                // Note: HistoireMaladie ne doit pas être pré-rempli avec les réponses du questionnaire patient
                // Le champ consultation.Anamnese contient les réponses formatées du patient (format "- Question: Réponse")
                // Le médecin doit pouvoir rédiger l'histoire de la maladie lui-même
                Anamnese = new AnamneseDto
                {
                    MotifConsultation = consultation.Motif,
                    HistoireMaladie = ConsultationHelpers.IsPatientQuestionnaireFormat(consultation.Anamnese) ? null : consultation.Anamnese,
                    QuestionsReponses = questionsReponses,
                    ParametresVitaux = consultation.Parametre != null ? new ParametresVitauxDto
                    {
                        Poids = consultation.Parametre.Poids,
                        Taille = consultation.Parametre.Taille,
                        Temperature = consultation.Parametre.Temperature,
                        TensionArterielle = consultation.Parametre.TensionFormatee
                    } : null
                },
                // Étape 2: Examen Clinique
                ExamenClinique = new ExamenCliniqueDto
                {
                    ParametresVitaux = consultation.Parametre != null ? new ParametresVitauxDto
                    {
                        Poids = consultation.Parametre.Poids,
                        Taille = consultation.Parametre.Taille,
                        Temperature = consultation.Parametre.Temperature,
                        TensionArterielle = consultation.Parametre.TensionFormatee
                    } : null,
                    ParametresPrisParInfirmier = parametresPrisParInfirmier,
                    InfirmierNom = infirmierNom,
                    DatePriseParametres = consultation.Parametre?.DateEnregistrement,
                    Inspection = consultation.ExamenInspection,
                    Palpation = consultation.ExamenPalpation,
                    Auscultation = consultation.ExamenAuscultation,
                    Percussion = consultation.ExamenPercussion,
                    AutresObservations = consultation.ExamenAutres
                },
                ExamenGynecologique = MapExamenGynecologique(consultation.ConsultationGynecologique),
                // Étape 3: Diagnostic
                Diagnostic = new DiagnosticDto
                {
                    DiagnosticPrincipal = consultation.Diagnostic,
                    DiagnosticsSecondaires = consultation.DiagnosticsSecondaires,
                    HypothesesDiagnostiques = consultation.HypothesesDiagnostiques,
                    NotesCliniques = consultation.NotesCliniques,
                    RecapitulatifPatient = recapPatient
                },
                // Étape 4: Plan de Traitement
                PlanTraitement = new PlanTraitementDto
                {
                    ExplicationDiagnostic = consultation.ExplicationDiagnostic,
                    OptionsTraitement = consultation.OptionsTraitement,
                    Ordonnance = ordonnanceDto,
                    ExamensPrescrits = examensPrescrits,
                    OrientationSpecialiste = consultation.OrientationSpecialiste,
                    MotifOrientation = consultation.MotifOrientation
                },
                // Étape 5: Conclusion
                Conclusion = new ConclusionDto
                {
                    ResumeConsultation = consultation.ResumeConsultation,
                    QuestionsPatient = consultation.QuestionsPatient,
                    ConsignesPatient = consultation.ConsignesPatient,
                    Recommandations = consultation.Recommandations
                },
                // Conservé pour compatibilité
                Prescriptions = new PrescriptionsDto
                {
                    Ordonnance = ordonnanceDto,
                    Examens = examensPrescrits,
                    Orientations = new List<OrientationPreConsultationDto>()
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

    /// <summary>
    /// Récupérer les détails complets d'une consultation (pour affichage)
    /// Accessible par le médecin ou le patient concerné
    /// </summary>
    [HttpGet("{idConsultation}/details")]
    [AllowAnonymous] // Bypass la restriction de classe, vérification manuelle dans la méthode
    public async Task<IActionResult> GetConsultationDetails(int idConsultation)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var consultation = await _context.Consultations
                .Include(c => c.Patient).ThenInclude(p => p!.Utilisateur)
                .Include(c => c.Medecin).ThenInclude(m => m!.Utilisateur)
                .Include(c => c.Parametre)!.ThenInclude(p => p!.UtilisateurEnregistrant)
                .Include(c => c.Ordonnance).ThenInclude(o => o!.Medicaments)!.ThenInclude(m => m.Medicament)
                .Include(c => c.BulletinsExamen).ThenInclude(b => b.Examen)
                .Include(c => c.ConsultationQuestions)!.ThenInclude(cq => cq.Question)
                .Include(c => c.ConsultationQuestions)!.ThenInclude(cq => cq.Reponses)
                .Include(c => c.ConsultationGynecologique)
                .FirstOrDefaultAsync(c => c.IdConsultation == idConsultation);

            if (consultation == null)
                return NotFound(new { message = "Consultation non trouvée" });

            // Vérifier que l'utilisateur a accès (médecin ou patient concerné)
            var isMedecin = consultation.IdMedecin == userId.Value;
            var isPatient = consultation.IdPatient == userId.Value;
            if (!isMedecin && !isPatient)
                return Forbid();

            // Mapper les questions-réponses
            var questionsReponses = consultation.ConsultationQuestions?.SelectMany(cq => 
                cq.Reponses?.Select(r => new QuestionReponseDto
                {
                    QuestionId = cq.QuestionId.ToString(),
                    Question = cq.Question?.TexteQuestion ?? "",
                    Reponse = r.ValeurReponse ?? ""
                }) ?? Enumerable.Empty<QuestionReponseDto>()
            ).ToList() ?? new List<QuestionReponseDto>();

            // Mapper l'ordonnance
            DTOs.Consultation.OrdonnanceDto? ordonnanceDto = null;
            if (consultation.Ordonnance != null)
            {
                ordonnanceDto = new DTOs.Consultation.OrdonnanceDto
                {
                    IdOrdonnance = consultation.Ordonnance.IdOrdonnance,
                    Notes = consultation.Ordonnance.Commentaire,
                    DureeTraitement = null,
                    Medicaments = consultation.Ordonnance.Medicaments?.Select(m => new MedicamentDto
                    {
                        IdPrescription = m.IdPrescriptionMed,
                        IdMedicament = m.IdMedicament,
                        NomMedicament = m.Medicament?.Nom ?? "",
                        Dosage = m.Medicament?.Dosage,
                        Posologie = m.Posologie,
                        Frequence = m.Frequence,
                        Duree = m.DureeTraitement,
                        VoieAdministration = m.VoieAdministration,
                        FormePharmaceutique = m.FormePharmaceutique,
                        Instructions = m.Instructions,
                        Quantite = m.Quantite
                    }).ToList() ?? new List<MedicamentDto>()
                };
            }

            // Mapper les examens prescrits
            var examensPrescrits = consultation.BulletinsExamen?.Select(b => new ExamenPrescritDetailDto
            {
                IdExamen = b.IdBulletinExamen,
                NomExamen = b.Examen?.NomExamen ?? "",
                Instructions = b.Instructions,
                Statut = "prescrit"
            }).ToList() ?? new List<ExamenPrescritDetailDto>();

            var examensPlan = consultation.BulletinsExamen?.Select(b => new ExamenPrescritDto
            {
                IdExamen = b.IdBulletinExamen,
                NomExamen = b.Examen?.NomExamen ?? "",
                Description = b.Examen?.Description ?? b.Instructions,
                Notes = b.Instructions,
                Urgence = b.Urgence,
                Categorie = b.Examen?.Specialite?.Categorie?.Nom ?? "",
                Specialite = b.Examen?.Specialite?.Nom ?? ""
            }).ToList() ?? new List<ExamenPrescritDto>();

            var parametresVitauxDto = consultation.Parametre != null ? new ParametresVitauxDto
            {
                Poids = consultation.Parametre.Poids,
                Taille = consultation.Parametre.Taille,
                Temperature = consultation.Parametre.Temperature,
                TensionArterielle = consultation.Parametre.TensionFormatee
            } : null;

            var parametresPrisParInfirmier = consultation.Parametre?.EnregistrePar.HasValue == true &&
                consultation.Parametre.EnregistrePar != consultation.IdMedecin;
            string? infirmierNom = null;
            if (parametresPrisParInfirmier && consultation.Parametre?.UtilisateurEnregistrant != null)
            {
                infirmierNom = $"{consultation.Parametre.UtilisateurEnregistrant.Prenom} {consultation.Parametre.UtilisateurEnregistrant.Nom}";
            }

            var examenCliniqueDto = new ExamenCliniqueDto
            {
                ParametresVitaux = parametresVitauxDto,
                ParametresPrisParInfirmier = parametresPrisParInfirmier,
                InfirmierNom = infirmierNom,
                DatePriseParametres = consultation.Parametre?.DateEnregistrement,
                Inspection = consultation.ExamenInspection,
                Palpation = consultation.ExamenPalpation,
                Auscultation = consultation.ExamenAuscultation,
                Percussion = consultation.ExamenPercussion,
                AutresObservations = consultation.ExamenAutres
            };

            var planTraitementDto = new PlanTraitementDto
            {
                ExplicationDiagnostic = consultation.ExplicationDiagnostic,
                OptionsTraitement = consultation.OptionsTraitement,
                Ordonnance = ordonnanceDto,
                ExamensPrescrits = examensPlan,
                OrientationSpecialiste = consultation.OrientationSpecialiste,
                MotifOrientation = consultation.MotifOrientation
            };

            var conclusionDto = new ConclusionDto
            {
                ResumeConsultation = consultation.ResumeConsultation,
                QuestionsPatient = consultation.QuestionsPatient,
                ConsignesPatient = consultation.ConsignesPatient,
                Recommandations = consultation.Recommandations
            };

            var result = new ConsultationDetailDto
            {
                IdConsultation = consultation.IdConsultation,
                IdPatient = consultation.IdPatient,
                PatientNom = consultation.Patient?.Utilisateur?.Nom ?? "",
                PatientPrenom = consultation.Patient?.Utilisateur?.Prenom ?? "",
                NumeroDossier = consultation.Patient?.NumeroDossier,
                DateConsultation = consultation.DateHeure.ToString("o"),
                Duree = 30, // Durée par défaut
                Motif = consultation.Motif,
                Statut = consultation.Statut ?? "a_faire",
                Anamnese = consultation.Anamnese,
                NotesCliniques = consultation.NotesCliniques,
                Diagnostic = consultation.Diagnostic,
                Conclusion = consultation.Conclusion,
                Recommandations = consultation.Recommandations,
                Ordonnance = ordonnanceDto,
                ExamensPrescrits = examensPrescrits,
                Questionnaire = questionsReponses,
                ParametresVitaux = parametresVitauxDto,
                ExamenClinique = examenCliniqueDto,
                ExamenGynecologique = MapExamenGynecologique(consultation.ConsultationGynecologique),
                PlanTraitement = planTraitementDto,
                ConclusionDetaillee = conclusionDto
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetConsultationDetails: {ex.Message}");
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
                .Include(c => c.ConsultationQuestions)!.ThenInclude(cq => cq.Reponses)
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

            // Sauvegarder les questions-réponses
            if (anamnese.QuestionsReponses != null && anamnese.QuestionsReponses.Count > 0)
            {
                // Supprimer les anciennes réponses pour cette consultation
                var existingCQs = consultation.ConsultationQuestions?.ToList() ?? new List<ConsultationQuestion>();
                foreach (var cq in existingCQs)
                {
                    if (cq.Reponses != null)
                    {
                        _context.Reponses.RemoveRange(cq.Reponses);
                    }
                    _context.ConsultationQuestions.Remove(cq);
                }

                // Créer les nouvelles questions-réponses
                foreach (var qr in anamnese.QuestionsReponses)
                {
                    if (string.IsNullOrWhiteSpace(qr.Reponse)) continue;

                    // Chercher ou créer la question en BD
                    var question = await _context.Questions
                        .FirstOrDefaultAsync(q => q.TexteQuestion == qr.Question);

                    if (question == null)
                    {
                        // Créer la question si elle n'existe pas
                        question = new Question
                        {
                            TexteQuestion = qr.Question,
                            TypeQuestion = "texte",
                            Categorie = "consultation",
                            Actif = true
                        };
                        _context.Questions.Add(question);
                        await _context.SaveChangesAsync(); // Pour obtenir l'ID
                    }

                    // Créer la liaison consultation-question
                    var consultationQuestion = new ConsultationQuestion
                    {
                        ConsultationId = idConsultation,
                        QuestionId = question.Id
                    };
                    _context.ConsultationQuestions.Add(consultationQuestion);
                    await _context.SaveChangesAsync(); // Pour obtenir l'ID

                    // Créer la réponse
                    var reponse = new Reponse
                    {
                        ConsultationQuestionId = consultationQuestion.Id,
                        ValeurReponse = qr.Reponse,
                        DateReponse = DateTime.UtcNow
                    };
                    _context.Reponses.Add(reponse);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Anamnèse sauvegardée pour consultation {idConsultation} avec {anamnese.QuestionsReponses?.Count ?? 0} questions-réponses");
            return Ok(new { message = "Anamnèse sauvegardée" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur SaveAnamnese: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Étape 2: Sauvegarder l'examen clinique (constantes vitales + examen physique)
    /// </summary>
    [HttpPost("{idConsultation}/examen-clinique")]
    public async Task<IActionResult> SaveExamenClinique(int idConsultation, [FromBody] ExamenCliniqueDto examenClinique)
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

            // Sauvegarder les constantes vitales si fournies (et pas déjà prises par infirmier)
            if (examenClinique.ParametresVitaux != null)
            {
                var parametre = GetOrCreateParametre(consultation, medecinId.Value);
                parametre.Poids = examenClinique.ParametresVitaux.Poids;
                parametre.Taille = examenClinique.ParametresVitaux.Taille;
                parametre.Temperature = examenClinique.ParametresVitaux.Temperature;

                if (!string.IsNullOrEmpty(examenClinique.ParametresVitaux.TensionArterielle))
                {
                    var parts = examenClinique.ParametresVitaux.TensionArterielle.Split('/');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int sys) && int.TryParse(parts[1], out int dia))
                    {
                        parametre.TensionSystolique = sys;
                        parametre.TensionDiastolique = dia;
                    }
                }
            }

            // Sauvegarder l'examen physique
            consultation.ExamenInspection = examenClinique.Inspection;
            consultation.ExamenPalpation = examenClinique.Palpation;
            consultation.ExamenAuscultation = examenClinique.Auscultation;
            consultation.ExamenPercussion = examenClinique.Percussion;
            consultation.ExamenAutres = examenClinique.AutresObservations;
            consultation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Examen clinique sauvegardé pour consultation {idConsultation}");
            return Ok(new { message = "Examen clinique sauvegardé" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur SaveExamenClinique: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Étape 2 bis: Sauvegarder l'examen gynécologique (réservé aux gynécologues)
    /// </summary>
    [HttpPost("{idConsultation}/examen-gynecologique")]
    public async Task<IActionResult> SaveExamenGynecologique(int idConsultation, [FromBody] ExamenGynecologiqueDto examenGynecologique)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var medecin = await _context.Medecins
                .Select(m => new { m.IdUser, m.IdSpecialite })
                .FirstOrDefaultAsync(m => m.IdUser == medecinId.Value);

            if (medecin == null)
                return Forbid();

            if (medecin.IdSpecialite != SpecialiteIds.GynecologieObstetrique)
                return BadRequest(new { message = "Cette étape est réservée aux consultations gynécologiques." });

            var consultation = await _context.Consultations
                .Include(c => c.ConsultationGynecologique)
                .FirstOrDefaultAsync(c => c.IdConsultation == idConsultation && c.IdMedecin == medecinId.Value);

            if (consultation == null)
                return NotFound(new { message = "Consultation non trouvée" });

            if (consultation.ConsultationGynecologique == null)
            {
                consultation.ConsultationGynecologique = new ConsultationGynecologique
                {
                    IdConsultation = idConsultation,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ConsultationsGynecologiques.Add(consultation.ConsultationGynecologique);
            }

            consultation.ConsultationGynecologique.InspectionExterne = examenGynecologique.InspectionExterne;
            consultation.ConsultationGynecologique.ExamenSpeculum = examenGynecologique.ExamenSpeculum;
            consultation.ConsultationGynecologique.ToucherVaginal = examenGynecologique.ToucherVaginal;
            consultation.ConsultationGynecologique.AutresObservations = examenGynecologique.AutresObservations;
            consultation.ConsultationGynecologique.UpdatedAt = DateTime.UtcNow;
            consultation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Examen gynécologique sauvegardé pour consultation {idConsultation}");
            return Ok(new { message = "Examen gynécologique sauvegardé" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur SaveExamenGynecologique: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Étape 3: Sauvegarder le diagnostic et orientation
    /// </summary>
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
            consultation.DiagnosticsSecondaires = diagnostic.DiagnosticsSecondaires;
            consultation.HypothesesDiagnostiques = diagnostic.HypothesesDiagnostiques;
            consultation.NotesCliniques = diagnostic.NotesCliniques;
            consultation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Diagnostic sauvegardé pour consultation {idConsultation}");
            return Ok(new { message = "Diagnostic sauvegardé" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur SaveDiagnostic: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Étape 4: Sauvegarder le plan de traitement
    /// </summary>
    [HttpPost("{idConsultation}/plan-traitement")]
    public async Task<IActionResult> SavePlanTraitement(int idConsultation, [FromBody] PlanTraitementDto planTraitement)
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

            // Sauvegarder les explications et options
            consultation.ExplicationDiagnostic = planTraitement.ExplicationDiagnostic;
            consultation.OptionsTraitement = planTraitement.OptionsTraitement;
            consultation.OrientationSpecialiste = planTraitement.OrientationSpecialiste;
            consultation.MotifOrientation = planTraitement.MotifOrientation;
            consultation.UpdatedAt = DateTime.UtcNow;

            // Sauvegarder l'ordonnance via le service centralisé
            if (planTraitement.Ordonnance != null && planTraitement.Ordonnance.Medicaments.Count > 0)
            {
                // Convertir les médicaments du DTO vers le format du service centralisé
                var medicamentsRequest = planTraitement.Ordonnance.Medicaments.Select(med => new MedicamentPrescriptionRequest
                {
                    NomMedicament = med.NomMedicament,
                    Dosage = med.Dosage,
                    Quantite = med.Quantite ?? 1,
                    Posologie = med.Posologie,
                    Frequence = med.Frequence,
                    DureeTraitement = med.Duree,
                    VoieAdministration = med.VoieAdministration,
                    FormePharmaceutique = med.FormePharmaceutique,
                    Instructions = med.Instructions
                }).ToList();

                // Utiliser le service centralisé pour créer/mettre à jour l'ordonnance
                var ordonnanceResult = await _prescriptionService.CreerOrdonnanceConsultationAsync(
                    idConsultation,
                    medicamentsRequest,
                    planTraitement.Ordonnance.Notes,
                    medecinId.Value);

                if (!ordonnanceResult.Success)
                {
                    _logger.LogWarning("Erreur lors de la création de l'ordonnance: {Message}", ordonnanceResult.Message);
                    // On continue quand même pour sauvegarder le reste du plan de traitement
                }
                else if (ordonnanceResult.Alertes.Any())
                {
                    _logger.LogInformation("Alertes prescription: {Alertes}", 
                        string.Join(", ", ordonnanceResult.Alertes.Select(a => a.Message)));
                }
            }

            // Sauvegarder les examens prescrits
            if (planTraitement.ExamensPrescrits != null && planTraitement.ExamensPrescrits.Count > 0)
            {
                // Supprimer les anciens bulletins d'examen
                if (consultation.BulletinsExamen != null && consultation.BulletinsExamen.Any())
                {
                    _context.BulletinsExamen.RemoveRange(consultation.BulletinsExamen);
                }

                foreach (var exam in planTraitement.ExamensPrescrits)
                {
                    // Chercher l'examen dans le catalogue par nom
                    var examenCatalogue = await _context.ExamensCatalogue
                        .FirstOrDefaultAsync(e => e.NomExamen.Contains(exam.NomExamen) || exam.NomExamen.Contains(e.NomExamen));

                    var bulletinExamen = new BulletinExamen
                    {
                        IdConsultation = idConsultation,
                        DateDemande = DateTime.UtcNow,
                        IdExamen = examenCatalogue?.IdExamen,
                        IdLabo = exam.IdLaboratoire,
                        Instructions = exam.Notes
                    };
                    _context.BulletinsExamen.Add(bulletinExamen);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Plan de traitement sauvegardé pour consultation {idConsultation}");
            return Ok(new { message = "Plan de traitement sauvegardé" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur SavePlanTraitement: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Étape 5: Sauvegarder la conclusion
    /// </summary>
    [HttpPost("{idConsultation}/conclusion")]
    public async Task<IActionResult> SaveConclusion(int idConsultation, [FromBody] ConclusionDto conclusion)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var consultation = await _context.Consultations
                .FirstOrDefaultAsync(c => c.IdConsultation == idConsultation && c.IdMedecin == medecinId.Value);

            if (consultation == null)
                return NotFound(new { message = "Consultation non trouvée" });

            consultation.ResumeConsultation = conclusion.ResumeConsultation;
            consultation.QuestionsPatient = conclusion.QuestionsPatient;
            consultation.ConsignesPatient = conclusion.ConsignesPatient;
            consultation.Recommandations = conclusion.Recommandations;
            consultation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Conclusion sauvegardée pour consultation {idConsultation}");
            return Ok(new { message = "Conclusion sauvegardée" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur SaveConclusion: {ex.Message}");
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

            // Note: Les orientations sont maintenant gérées via les endpoints dédiés /orientations
            // Le champ Recommandations de la consultation reste pour les notes textuelles générales
            
            consultation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Prescriptions sauvegardées pour consultation {idConsultation}");
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
                .Include(c => c.Patient)
                .FirstOrDefaultAsync(c => c.IdConsultation == idConsultation && c.IdMedecin == medecinId.Value);

            if (consultation == null)
                return NotFound(new { message = "Consultation non trouvée" });

            // Validation des champs obligatoires avant clôture
            var erreurs = new List<string>();
            if (string.IsNullOrWhiteSpace(consultation.Motif))
                erreurs.Add("Le motif de consultation est obligatoire");
            if (string.IsNullOrWhiteSpace(consultation.Diagnostic))
                erreurs.Add("Le diagnostic est obligatoire pour valider la consultation");
            
            if (erreurs.Count > 0)
            {
                return BadRequest(new { 
                    message = "Impossible de valider la consultation : champs obligatoires manquants",
                    erreurs = erreurs
                });
            }

            // Valider la transition de statut
            var nouveauStatut = ConsultationStatut.Terminee.ToDbString();
            if (!Core.Services.StatutTransitionValidator.IsConsultationTransitionValid(consultation.Statut, nouveauStatut))
            {
                return BadRequest(new { 
                    message = Core.Services.StatutTransitionValidator.GetTransitionErrorMessage("consultation", consultation.Statut, nouveauStatut)
                });
            }

            // Calculer la durée réelle de la consultation
            var dateFin = DateTime.UtcNow;
            consultation.DateFin = dateFin;
            if (consultation.DateDebutEffective.HasValue)
            {
                consultation.DureeMinutes = (int)(dateFin - consultation.DateDebutEffective.Value).TotalMinutes;
            }

            var ancienStatut = consultation.Statut;
            consultation.Statut = nouveauStatut;
            consultation.Conclusion = request.Conclusion;
            consultation.UpdatedAt = DateTime.UtcNow;

            // Mettre à jour le statut du RDV associé
            if (consultation.IdRendezVous.HasValue)
            {
                var rdv = await _context.RendezVous
                    .FirstOrDefaultAsync(r => r.IdRendezVous == consultation.IdRendezVous.Value);
                if (rdv != null)
                {
                    rdv.Statut = RendezVousStatut.Termine.ToDbString();
                }
            }

            // Si le dossier était clôturé (première consultation après clôture), le réouvrir
            if (consultation.Patient != null && consultation.Patient.DossierCloture)
            {
                consultation.Patient.DossierCloture = false;
                consultation.Patient.DateClotureDossier = null;
                consultation.Patient.IdMedecinCloture = null;
                _logger.LogInformation($"Dossier réouvert pour patient {consultation.IdPatient} après première consultation");
            }

            await _context.SaveChangesAsync();

            // Envoyer une notification au patient
            try
            {
                var medecinNom = await _context.Medecins
                    .Where(m => m.IdUser == medecinId.Value)
                    .Include(m => m.Utilisateur)
                    .Select(m => m.Utilisateur != null ? $"Dr. {m.Utilisateur.Prenom} {m.Utilisateur.Nom}" : "Votre médecin")
                    .FirstOrDefaultAsync() ?? "Votre médecin";

                await _notificationService.CreateAsync(new CreateNotificationRequest
                {
                    IdUser = consultation.IdPatient,
                    Type = "consultation_terminee",
                    Titre = "Consultation terminée",
                    Message = $"Votre consultation avec {medecinNom} est terminée. Vous pouvez consulter le compte-rendu dans votre dossier médical.",
                    Lien = $"/patient/consultations/{idConsultation}",
                    Icone = "stethoscope",
                    Priorite = "normale",
                    SendRealTime = true
                });
            }
            catch (Exception notifEx)
            {
                _logger.LogWarning($"Erreur envoi notification patient: {notifEx.Message}");
            }

            // Audit trail
            await _auditService.LogAsync(new ConsultationAuditEntry
            {
                IdConsultation = idConsultation,
                IdUtilisateur = medecinId.Value,
                TypeAction = ConsultationAuditActions.Validation,
                ChampModifie = "statut",
                AncienneValeur = ancienStatut,
                NouvelleValeur = nouveauStatut,
                Description = $"Consultation validée. Durée: {consultation.DureeMinutes} minutes"
            });

            _logger.LogInformation($"Consultation {idConsultation} validée. Durée: {consultation.DureeMinutes} minutes");

            return Ok(new { 
                message = "Consultation validée", 
                idConsultation,
                dureeMinutes = consultation.DureeMinutes,
                imprimer = request.Imprimer 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur ValiderConsultation: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Annuler une consultation
    /// </summary>
    [HttpPost("{idConsultation}/annuler")]
    public async Task<IActionResult> AnnulerConsultation(int idConsultation, [FromBody] AnnulerConsultationRequest request)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var consultation = await _context.Consultations
                .Include(c => c.Patient)
                .FirstOrDefaultAsync(c => c.IdConsultation == idConsultation && c.IdMedecin == medecinId.Value);

            if (consultation == null)
                return NotFound(new { message = "Consultation non trouvée" });

            // Valider la transition de statut
            var nouveauStatut = ConsultationStatut.Annulee.ToDbString();
            if (!Core.Services.StatutTransitionValidator.IsConsultationTransitionValid(consultation.Statut, nouveauStatut))
            {
                return BadRequest(new { 
                    message = Core.Services.StatutTransitionValidator.GetTransitionErrorMessage("consultation", consultation.Statut, nouveauStatut)
                });
            }

            // Vérifier que le motif est fourni
            if (string.IsNullOrWhiteSpace(request.Motif))
            {
                return BadRequest(new { message = "Le motif d'annulation est obligatoire" });
            }

            var ancienStatut = consultation.Statut;
            consultation.Statut = nouveauStatut;
            consultation.MotifAnnulation = request.Motif;
            consultation.DateAnnulation = DateTime.UtcNow;
            consultation.UpdatedAt = DateTime.UtcNow;

            // Annuler le RDV associé si existant
            if (consultation.IdRendezVous.HasValue)
            {
                var rdv = await _context.RendezVous
                    .FirstOrDefaultAsync(r => r.IdRendezVous == consultation.IdRendezVous.Value);
                if (rdv != null)
                {
                    rdv.Statut = RendezVousStatut.Annule.ToDbString();
                }
            }

            await _context.SaveChangesAsync();

            // Audit trail
            await _auditService.LogAsync(new ConsultationAuditEntry
            {
                IdConsultation = idConsultation,
                IdUtilisateur = medecinId.Value,
                TypeAction = ConsultationAuditActions.Annulation,
                ChampModifie = "statut",
                AncienneValeur = ancienStatut,
                NouvelleValeur = nouveauStatut,
                Description = $"Consultation annulée. Motif: {request.Motif}"
            });

            _logger.LogInformation($"Consultation {idConsultation} annulée par médecin {medecinId}. Motif: {request.Motif}");

            return Ok(new { 
                message = "Consultation annulée", 
                idConsultation,
                motif = request.Motif
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur AnnulerConsultation: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Mettre une consultation en pause avec sauvegarde de l'étape actuelle
    /// </summary>
    [HttpPost("{idConsultation}/pause")]
    public async Task<IActionResult> PauseConsultation(int idConsultation, [FromBody] PauseConsultationRequest? request = null)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var consultation = await _context.Consultations
                .FirstOrDefaultAsync(c => c.IdConsultation == idConsultation && c.IdMedecin == medecinId.Value);

            if (consultation == null)
                return NotFound(new { message = "Consultation non trouvée" });

            // Seule une consultation en cours peut être mise en pause
            var ancienStatut = consultation.Statut;
            if (ancienStatut != ConsultationStatut.EnCours.ToDbString())
            {
                return BadRequest(new { message = "Seule une consultation en cours peut être mise en pause" });
            }

            var nouveauStatut = ConsultationStatut.EnPause.ToDbString();
            consultation.Statut = nouveauStatut;
            consultation.UpdatedAt = DateTime.UtcNow;
            
            // Sauvegarder l'étape actuelle si fournie
            if (!string.IsNullOrEmpty(request?.EtapeActuelle))
            {
                consultation.EtapeActuelle = request.EtapeActuelle;
            }

            await _context.SaveChangesAsync();

            // Audit trail
            await _auditService.LogStatutChangeAsync(idConsultation, medecinId.Value, ancienStatut, nouveauStatut, "Consultation mise en pause");

            _logger.LogInformation($"Consultation {idConsultation} mise en pause par médecin {medecinId}");

            return Ok(new { message = "Consultation mise en pause", idConsultation });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur PauseConsultation: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Reprendre une consultation en pause
    /// </summary>
    [HttpPost("{idConsultation}/reprendre")]
    public async Task<IActionResult> ReprendreConsultation(int idConsultation)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var consultation = await _context.Consultations
                .FirstOrDefaultAsync(c => c.IdConsultation == idConsultation && c.IdMedecin == medecinId.Value);

            if (consultation == null)
                return NotFound(new { message = "Consultation non trouvée" });

            // Seule une consultation en pause peut être reprise
            var ancienStatut = consultation.Statut;
            if (ancienStatut != ConsultationStatut.EnPause.ToDbString())
            {
                return BadRequest(new { message = "Seule une consultation en pause peut être reprise" });
            }

            var nouveauStatut = ConsultationStatut.EnCours.ToDbString();
            consultation.Statut = nouveauStatut;
            consultation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Audit trail
            await _auditService.LogStatutChangeAsync(idConsultation, medecinId.Value, ancienStatut, nouveauStatut, "Consultation reprise");

            _logger.LogInformation($"Consultation {idConsultation} reprise par médecin {medecinId}");

            return Ok(new { message = "Consultation reprise", idConsultation });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur ReprendreConsultation: {ex.Message}");
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
                    Prescriptions = new PrescriptionsDto { Examens = new List<ExamenPrescritDto>(), Orientations = new List<OrientationPreConsultationDto>() }
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

    /// <summary>
    /// Récupérer les créneaux disponibles du médecin pour une date donnée
    /// </summary>
    [HttpGet("creneaux-disponibles")]
    public async Task<IActionResult> GetCreneauxDisponibles([FromQuery] DateTime date)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            // Récupérer le jour de la semaine (1=Lundi, 7=Dimanche)
            var jourSemaine = (int)date.DayOfWeek;
            if (jourSemaine == 0) jourSemaine = 7;

            // Récupérer les créneaux configurés pour ce jour
            var creneauxConfigures = await _context.CreneauxDisponibles
                .Where(c => c.IdMedecin == medecinId.Value && c.JourSemaine == jourSemaine && c.Actif)
                .OrderBy(c => c.HeureDebut)
                .ToListAsync();

            // Récupérer les RDV existants pour cette date
            var dateDebut = date.Date;
            var dateFin = date.Date.AddDays(1);
            var rdvsExistants = await _context.RendezVous
                .Where(r => r.IdMedecin == medecinId.Value 
                    && r.DateHeure >= dateDebut 
                    && r.DateHeure < dateFin
                    && r.Statut != RendezVousStatut.Annule.ToDbString())
                .Select(r => new { r.DateHeure, r.Duree })
                .ToListAsync();

            // Générer les slots disponibles
            var slotsDisponibles = new List<object>();
            var now = DateTime.Now;

            foreach (var creneau in creneauxConfigures)
            {
                var heureDebut = date.Date.Add(creneau.HeureDebut);
                var heureFin = date.Date.Add(creneau.HeureFin);
                var dureeSlot = creneau.DureeParDefaut > 0 ? creneau.DureeParDefaut : 30;

                var current = heureDebut;
                while (current.AddMinutes(dureeSlot) <= heureFin)
                {
                    var slotFin = current.AddMinutes(dureeSlot);
                    
                    // Vérifier si le créneau est dans le passé
                    if (current <= now)
                    {
                        current = slotFin;
                        continue;
                    }

                    // Vérifier si le créneau est déjà occupé
                    var estOccupe = rdvsExistants.Any(r => 
                        r.DateHeure < slotFin && r.DateHeure.AddMinutes(r.Duree) > current);

                    if (!estOccupe)
                    {
                        slotsDisponibles.Add(new
                        {
                            heureDebut = current.ToString("HH:mm"),
                            heureFin = slotFin.ToString("HH:mm"),
                            dateHeure = current.ToString("o"),
                            duree = dureeSlot
                        });
                    }

                    current = slotFin;
                }
            }

            return Ok(new
            {
                date = date.ToString("yyyy-MM-dd"),
                jourSemaine = jourSemaine,
                creneaux = slotsDisponibles
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetCreneauxDisponibles: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Créer un rendez-vous de suivi depuis la consultation
    /// </summary>
    [HttpPost("{idConsultation}/rdv-suivi")]
    public async Task<IActionResult> CreerRdvSuivi(int idConsultation, [FromBody] CreerRdvSuiviRequest request)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            // Vérifier que la consultation existe et appartient au médecin
            var consultation = await _context.Consultations
                .Include(c => c.Patient)
                .FirstOrDefaultAsync(c => c.IdConsultation == idConsultation && c.IdMedecin == medecinId.Value);

            if (consultation == null)
                return NotFound(new { message = "Consultation non trouvée" });

            // Vérifier que le créneau est disponible
            var dateRdv = DateTime.Parse(request.DateHeure);
            var duree = request.Duree ?? 30;
            var dateFin = dateRdv.AddMinutes(duree);

            var conflitExistant = await _context.RendezVous
                .AnyAsync(r => r.IdMedecin == medecinId.Value
                    && r.Statut != RendezVousStatut.Annule.ToDbString()
                    && r.DateHeure < dateFin
                    && r.DateHeure.AddMinutes(r.Duree) > dateRdv);

            if (conflitExistant)
                return BadRequest(new { message = "Ce créneau n'est plus disponible" });

            // Créer le rendez-vous de suivi
            var rdv = new RendezVous
            {
                IdPatient = consultation.IdPatient,
                IdMedecin = medecinId.Value,
                DateHeure = dateRdv,
                Duree = duree,
                Statut = RendezVousStatut.Confirme.ToDbString(),
                TypeRdv = RendezVousTypes.Suivi,
                Motif = request.Motif ?? $"Suivi consultation du {consultation.DateHeure:dd/MM/yyyy}",
                Notes = request.Notes,
                DateCreation = DateTime.UtcNow
            };

            _context.RendezVous.Add(rdv);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"RDV de suivi créé: {rdv.IdRendezVous} pour patient {consultation.IdPatient}");

            return Ok(new
            {
                success = true,
                message = "Rendez-vous de suivi créé avec succès",
                idRendezVous = rdv.IdRendezVous,
                dateHeure = rdv.DateHeure.ToString("o"),
                patientNom = consultation.Patient?.Utilisateur?.Nom,
                patientPrenom = consultation.Patient?.Utilisateur?.Prenom
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur CreerRdvSuivi: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Clôturer le dossier d'un patient (prochaine consultation = première consultation)
    /// </summary>
    [HttpPost("{idConsultation}/cloturer-dossier")]
    public async Task<IActionResult> CloturerDossier(int idConsultation, [FromBody] CloturerDossierRequest? request)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue)
                return Unauthorized(new { message = "Non autorisé" });

            // Vérifier que la consultation existe et appartient au médecin
            var consultation = await _context.Consultations
                .Include(c => c.Patient)
                .FirstOrDefaultAsync(c => c.IdConsultation == idConsultation && c.IdMedecin == medecinId.Value);

            if (consultation == null)
                return NotFound(new { message = "Consultation non trouvée" });

            // Mettre à jour le dossier patient comme clôturé
            var patient = consultation.Patient;
            if (patient != null)
            {
                patient.DossierCloture = true;
                patient.DateClotureDossier = DateTime.Now;
                patient.IdMedecinCloture = medecinId.Value;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation($"Dossier clôturé pour patient {consultation.IdPatient} par médecin {medecinId.Value}");

            return Ok(new
            {
                success = true,
                message = "Dossier patient clôturé avec succès. La prochaine consultation sera considérée comme une première consultation."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur CloturerDossier: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer tous les créneaux d'une date avec leur statut (disponible/occupé/passé)
    /// </summary>
    [HttpGet("creneaux-avec-statut")]
    public async Task<IActionResult> GetCreneauxAvecStatut([FromQuery] DateTime date)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue)
                return Unauthorized(new { message = "Non autorisé" });

            var jourSemaine = ((int)date.DayOfWeek == 0) ? 7 : (int)date.DayOfWeek;
            var now = DateTime.Now;

            // Récupérer les créneaux configurés pour ce jour
            var creneauxConfigures = await _context.CreneauxDisponibles
                .Where(c => c.IdMedecin == medecinId.Value && c.JourSemaine == jourSemaine && c.Actif)
                .Where(c => c.DateDebutValidite == null || c.DateDebutValidite <= date)
                .Where(c => c.DateFinValidite == null || c.DateFinValidite >= date)
                .ToListAsync();

            // Récupérer les RDV existants pour cette date
            var rdvExistants = await _context.RendezVous
                .Where(r => r.IdMedecin == medecinId.Value && r.DateHeure.Date == date.Date && r.Statut != RendezVousStatut.Annule.ToDbString())
                .Select(r => new { r.DateHeure, r.Duree })
                .ToListAsync();

            var creneauxAvecStatut = new List<object>();

            foreach (var creneau in creneauxConfigures)
            {
                var heureDebut = date.Date.Add(creneau.HeureDebut);
                var heureFin = date.Date.Add(creneau.HeureFin);
                var dureeSlot = creneau.DureeParDefaut > 0 ? creneau.DureeParDefaut : 30;

                var current = heureDebut;
                while (current.AddMinutes(dureeSlot) <= heureFin)
                {
                    var slotFin = current.AddMinutes(dureeSlot);
                    string statut;

                    // Vérifier si passé
                    if (current <= now)
                    {
                        statut = "passe";
                    }
                    // Vérifier si occupé
                    else if (rdvExistants.Any(r => r.DateHeure < slotFin && r.DateHeure.AddMinutes(r.Duree) > current))
                    {
                        statut = "occupe";
                    }
                    else
                    {
                        statut = "disponible";
                    }

                    creneauxAvecStatut.Add(new
                    {
                        heureDebut = current.ToString("HH:mm"),
                        heureFin = slotFin.ToString("HH:mm"),
                        dateHeure = current.ToString("o"),
                        duree = dureeSlot,
                        statut
                    });

                    current = slotFin;
                }
            }

            return Ok(new
            {
                date = date.ToString("yyyy-MM-dd"),
                creneaux = creneauxAvecStatut.OrderBy(c => ((dynamic)c).heureDebut).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetCreneauxAvecStatut: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupère la liste des laboratoires disponibles pour les prescriptions d'examens
    /// </summary>
    [HttpGet("laboratoires")]
    public async Task<IActionResult> GetLaboratoires()
    {
        try
        {
            var laboratoires = await _context.Laboratoires
                .Where(l => l.Actif)
                .OrderBy(l => l.Type) // Internes d'abord
                .ThenBy(l => l.NomLabo)
                .Select(l => new LaboratoireDto
                {
                    IdLabo = l.IdLabo,
                    NomLabo = l.NomLabo,
                    Contact = l.Contact,
                    Adresse = l.Adresse,
                    Telephone = l.Telephone,
                    Type = l.Type
                })
                .ToListAsync();

            return Ok(laboratoires);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetLaboratoires: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des laboratoires" });
        }
    }

    // ==================== ORIENTATION PRE-CONSULTATION (UNIFIÉ) ====================

    /// <summary>
    /// Récupère la liste des spécialités disponibles
    /// </summary>
    [HttpGet("specialites")]
    public async Task<IActionResult> GetSpecialites()
    {
        try
        {
            var specialites = await _context.Specialites
                .OrderBy(s => s.NomSpecialite)
                .Select(s => new SpecialiteDto
                {
                    IdSpecialite = s.IdSpecialite,
                    NomSpecialite = s.NomSpecialite,
                    CoutConsultation = s.CoutConsultation
                })
                .ToListAsync();

            return Ok(specialites);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetSpecialites: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des spécialités" });
        }
    }

    /// <summary>
    /// Récupère les médecins d'une spécialité donnée
    /// </summary>
    [HttpGet("specialites/{idSpecialite}/medecins")]
    public async Task<IActionResult> GetMedecinsParSpecialite(int idSpecialite)
    {
        try
        {
            var medecins = await _context.Medecins
                .Include(m => m.Utilisateur)
                .Include(m => m.Specialite)
                .Where(m => m.IdSpecialite == idSpecialite && m.Utilisateur != null)
                .Select(m => new MedecinSpecialisteDto
                {
                    IdUser = m.IdUser,
                    Nom = m.Utilisateur!.Nom,
                    Prenom = m.Utilisateur.Prenom,
                    IdSpecialite = m.IdSpecialite ?? 0,
                    NomSpecialite = m.Specialite != null ? m.Specialite.NomSpecialite : null
                })
                .ToListAsync();

            return Ok(medecins);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetMedecinsParSpecialite: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des médecins" });
        }
    }

    /// <summary>
    /// Récupère les orientations d'une consultation
    /// </summary>
    [HttpGet("{idConsultation}/orientations")]
    public async Task<IActionResult> GetOrientations(int idConsultation)
    {
        try
        {
            var orientationsEntities = await _context.OrientationsPreConsultation
                .Include(o => o.Specialite)
                .Include(o => o.MedecinOriente).ThenInclude(m => m!.Utilisateur)
                .Include(o => o.MedecinPrescripteur).ThenInclude(m => m!.Utilisateur)
                .Where(o => o.IdConsultation == idConsultation)
                .OrderByDescending(o => o.Prioritaire)
                .ThenByDescending(o => o.DateOrientation)
                .ToListAsync();

            var orientations = orientationsEntities.Select(o => MapToOrientationDto(o)).ToList();
            return Ok(orientations);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetOrientations: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des orientations" });
        }
    }

    /// <summary>
    /// Crée une orientation (unifiée: médecin interne/externe, hôpital, etc.)
    /// </summary>
    [HttpPost("{idConsultation}/orientations")]
    public async Task<IActionResult> CreateOrientation(int idConsultation, [FromBody] CreateOrientationRequest request)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var consultation = await _context.Consultations
                .FirstOrDefaultAsync(c => c.IdConsultation == idConsultation && c.IdMedecin == medecinId.Value);

            if (consultation == null)
                return NotFound(new { message = "Consultation non trouvée" });

            if (string.IsNullOrWhiteSpace(request.Motif))
                return BadRequest(new { message = "Le motif est obligatoire" });

            // Valider le type d'orientation
            var validTypes = new[] { TypesOrientation.MedecinInterne, TypesOrientation.MedecinExterne, 
                                     TypesOrientation.Hopital, TypesOrientation.ServiceInterne, TypesOrientation.Laboratoire };
            if (!validTypes.Contains(request.TypeOrientation))
                return BadRequest(new { message = "Type d'orientation invalide" });

            // Résoudre les noms pour médecin interne
            string? nomMedecinOriente = null;
            string? nomSpecialite = null;
            if (request.TypeOrientation == TypesOrientation.MedecinInterne)
            {
                if (request.IdSpecialite.HasValue)
                {
                    var specialite = await _context.Specialites.FindAsync(request.IdSpecialite.Value);
                    nomSpecialite = specialite?.NomSpecialite;
                }
                if (request.IdMedecinOriente.HasValue)
                {
                    var medecinOriente = await _context.Medecins
                        .Include(m => m.Utilisateur)
                        .FirstOrDefaultAsync(m => m.IdUser == request.IdMedecinOriente.Value);
                    if (medecinOriente?.Utilisateur != null)
                        nomMedecinOriente = $"Dr. {medecinOriente.Utilisateur.Prenom} {medecinOriente.Utilisateur.Nom}";
                }
            }

            var orientation = new OrientationPreConsultation
            {
                IdConsultation = idConsultation,
                IdPatient = consultation.IdPatient,
                IdMedecinPrescripteur = medecinId.Value,
                TypeOrientation = request.TypeOrientation,
                IdSpecialite = request.IdSpecialite,
                IdMedecinOriente = request.IdMedecinOriente,
                NomDestinataire = request.NomDestinataire,
                SpecialiteTexte = request.SpecialiteTexte ?? nomSpecialite,
                AdresseDestinataire = request.AdresseDestinataire,
                TelephoneDestinataire = request.TelephoneDestinataire,
                Motif = request.Motif,
                Notes = request.Notes,
                Urgence = request.Urgence,
                Prioritaire = request.Prioritaire,
                Statut = StatutsOrientation.EnAttente,
                DateOrientation = DateTime.UtcNow,
                DateRdvPropose = request.DateRdvPropose,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.OrientationsPreConsultation.Add(orientation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Orientation créée: {Id} type={Type} pour consultation {ConsultationId}", 
                orientation.IdOrientation, orientation.TypeOrientation, idConsultation);

            // Charger les relations pour le retour
            await _context.Entry(orientation).Reference(o => o.Specialite).LoadAsync();
            await _context.Entry(orientation).Reference(o => o.MedecinOriente).LoadAsync();
            if (orientation.MedecinOriente != null)
                await _context.Entry(orientation.MedecinOriente).Reference(m => m.Utilisateur).LoadAsync();
            await _context.Entry(orientation).Reference(o => o.MedecinPrescripteur).LoadAsync();
            if (orientation.MedecinPrescripteur != null)
                await _context.Entry(orientation.MedecinPrescripteur).Reference(m => m.Utilisateur).LoadAsync();

            return Ok(MapToOrientationDto(orientation));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur CreateOrientation: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la création de l'orientation" });
        }
    }

    /// <summary>
    /// Met à jour le statut d'une orientation
    /// </summary>
    [HttpPut("orientations/{idOrientation}/statut")]
    public async Task<IActionResult> UpdateOrientationStatut(int idOrientation, [FromBody] UpdateOrientationStatutRequest request)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var orientation = await _context.OrientationsPreConsultation
                .Include(o => o.Consultation)
                .FirstOrDefaultAsync(o => o.IdOrientation == idOrientation);

            if (orientation == null)
                return NotFound(new { message = "Orientation non trouvée" });

            // Vérifier que le médecin est autorisé (prescripteur ou médecin de la consultation)
            if (orientation.IdMedecinPrescripteur != medecinId.Value && orientation.Consultation?.IdMedecin != medecinId.Value)
                return Forbid();

            orientation.Statut = request.Statut;
            orientation.DateRdvPropose = request.DateRdvPropose ?? orientation.DateRdvPropose;
            orientation.IdRdvCree = request.IdRdvCree ?? orientation.IdRdvCree;
            if (!string.IsNullOrEmpty(request.Notes))
                orientation.Notes = request.Notes;
            orientation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Statut mis à jour" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur UpdateOrientationStatut: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Supprime une orientation
    /// </summary>
    [HttpDelete("orientations/{idOrientation}")]
    public async Task<IActionResult> DeleteOrientation(int idOrientation)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var orientation = await _context.OrientationsPreConsultation
                .FirstOrDefaultAsync(o => o.IdOrientation == idOrientation && o.IdMedecinPrescripteur == medecinId.Value);

            if (orientation == null)
                return NotFound(new { message = "Orientation non trouvée" });

            _context.OrientationsPreConsultation.Remove(orientation);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Orientation supprimée avec succès" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur DeleteOrientation: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la suppression de l'orientation" });
        }
    }

    /// <summary>
    /// Créer un RDV lié à une orientation (médecin interne)
    /// </summary>
    [HttpPost("orientations/{idOrientation}/creer-rdv")]
    public async Task<IActionResult> CreerRdvPourOrientation(int idOrientation, [FromBody] CreerRdvOrientationRequest request)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var orientation = await _context.OrientationsPreConsultation
                .Include(o => o.Consultation)
                .Include(o => o.Patient).ThenInclude(p => p!.Utilisateur)
                .FirstOrDefaultAsync(o => o.IdOrientation == idOrientation);

            if (orientation == null)
                return NotFound(new { message = "Orientation non trouvée" });

            // Vérifier que c'est une orientation interne avec un médecin sélectionné
            if (orientation.TypeOrientation != TypesOrientation.MedecinInterne)
                return BadRequest(new { message = "La création de RDV n'est possible que pour les orientations internes" });

            if (!orientation.IdMedecinOriente.HasValue)
                return BadRequest(new { message = "Aucun médecin n'est sélectionné pour cette orientation" });

            // Vérifier que le créneau est disponible
            var dateHeure = DateTime.Parse(request.DateHeure);
            var existingRdv = await _context.RendezVous
                .AnyAsync(r => r.IdMedecin == orientation.IdMedecinOriente.Value 
                    && r.DateHeure == dateHeure 
                    && r.Statut != "annule");

            if (existingRdv)
                return BadRequest(new { message = "Ce créneau n'est plus disponible" });

            // Créer le RDV
            var rdv = new RendezVous
            {
                IdPatient = orientation.IdPatient,
                IdMedecin = orientation.IdMedecinOriente.Value,
                DateHeure = dateHeure,
                Duree = request.Duree ?? 30,
                Motif = request.Motif ?? orientation.Motif,
                Notes = $"RDV créé suite à orientation #{orientation.IdOrientation}. {request.Notes ?? ""}".Trim(),
                TypeRdv = "orientation",
                Statut = "planifie",
                DateCreation = DateTime.UtcNow,
                DateModification = DateTime.UtcNow
            };

            _context.RendezVous.Add(rdv);
            await _context.SaveChangesAsync();

            // Mettre à jour l'orientation
            orientation.IdRdvCree = rdv.IdRendezVous;
            orientation.DateRdvPropose = dateHeure;
            orientation.Statut = StatutsOrientation.RdvPris;
            orientation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Envoyer une notification au patient
            try
            {
                var medecinOriente = await _context.Medecins
                    .Include(m => m.Utilisateur)
                    .FirstOrDefaultAsync(m => m.IdUser == orientation.IdMedecinOriente.Value);
                var nomMedecin = medecinOriente?.Utilisateur != null 
                    ? $"Dr. {medecinOriente.Utilisateur.Prenom} {medecinOriente.Utilisateur.Nom}" 
                    : "un médecin";

                await _notificationService.CreateAsync(new CreateNotificationRequest
                {
                    IdUser = orientation.IdPatient,
                    Type = "orientation_rdv",
                    Titre = "Rendez-vous programmé suite à orientation",
                    Message = $"Un rendez-vous a été programmé pour vous avec {nomMedecin} le {dateHeure:dd/MM/yyyy à HH:mm}.",
                    Lien = $"/patient/rendez-vous",
                    Icone = "calendar-check",
                    Priorite = orientation.Urgence ? "haute" : "normale",
                    SendRealTime = true
                });
            }
            catch (Exception notifEx)
            {
                _logger.LogWarning($"Erreur envoi notification orientation RDV: {notifEx.Message}");
            }

            _logger.LogInformation("RDV {RdvId} créé pour orientation {OrientationId}", rdv.IdRendezVous, idOrientation);

            return Ok(new { 
                success = true,
                message = "Rendez-vous créé avec succès",
                idRendezVous = rdv.IdRendezVous,
                dateHeure = rdv.DateHeure
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur CreerRdvPourOrientation: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la création du rendez-vous" });
        }
    }

    /// <summary>
    /// Récupérer les créneaux disponibles d'un médecin pour une date donnée
    /// </summary>
    [HttpGet("medecins/{idMedecin}/creneaux-disponibles")]
    public async Task<IActionResult> GetCreneauxMedecinDisponibles(int idMedecin, [FromQuery] string date)
    {
        try
        {
            if (!DateTime.TryParse(date, out var dateRecherche))
                return BadRequest(new { message = "Date invalide" });

            // Récupérer les créneaux horaires du médecin pour ce jour de la semaine
            // Note: JourSemaine dans CreneauDisponible: 1=Lundi..7=Dimanche, DayOfWeek: 0=Dimanche..6=Samedi
            var dayOfWeek = (int)dateRecherche.DayOfWeek;
            var jourSemaine = dayOfWeek == 0 ? 7 : dayOfWeek; // Convertir au format 1-7
            var creneauxHoraires = await _context.CreneauxDisponibles
                .Where(c => c.IdMedecin == idMedecin && c.JourSemaine == jourSemaine && c.Actif)
                .OrderBy(c => c.HeureDebut)
                .ToListAsync();

            // Récupérer les RDV existants pour cette date
            var dateDebut = dateRecherche.Date;
            var dateFin = dateRecherche.Date.AddDays(1);
            var rdvExistants = await _context.RendezVous
                .Where(r => r.IdMedecin == idMedecin 
                    && r.DateHeure >= dateDebut 
                    && r.DateHeure < dateFin
                    && r.Statut != "annule")
                .Select(r => new { r.DateHeure, r.Duree })
                .ToListAsync();

            // Récupérer les indisponibilités
            var indisponibilites = await _context.IndisponibilitesMedecin
                .Where(i => i.IdMedecin == idMedecin 
                    && i.DateDebut <= dateFin 
                    && i.DateFin >= dateDebut)
                .ToListAsync();

            // Vérifier si le médecin est indisponible toute la journée
            var indispoJournee = indisponibilites.Any(i => 
                i.DateDebut.Date <= dateRecherche.Date && i.DateFin.Date >= dateRecherche.Date && i.JourneeComplete);

            if (indispoJournee)
            {
                return Ok(new {
                    date = dateRecherche.ToString("yyyy-MM-dd"),
                    medecinDisponible = false,
                    messageIndisponibilite = "Le médecin n'est pas disponible ce jour",
                    creneaux = new List<object>()
                });
            }

            // Générer les créneaux avec leur statut
            var creneaux = new List<object>();
            var maintenant = DateTime.UtcNow;

            foreach (var ch in creneauxHoraires)
            {
                var heureDebut = dateRecherche.Date.Add(ch.HeureDebut);
                var heureFin = dateRecherche.Date.Add(ch.HeureFin);
                var duree = ch.DureeParDefaut > 0 ? ch.DureeParDefaut : 30;

                var current = heureDebut;
                while (current.AddMinutes(duree) <= heureFin)
                {
                    var slotEnd = current.AddMinutes(duree);
                    
                    // Vérifier si le créneau est passé
                    var estPasse = current < maintenant;
                    
                    // Vérifier si le créneau est occupé
                    var estOccupe = rdvExistants.Any(r => 
                        r.DateHeure < slotEnd && r.DateHeure.AddMinutes(r.Duree) > current);

                    // Vérifier si le créneau est dans une période d'indisponibilité
                    var estIndispo = indisponibilites.Any(i => 
                        !i.JourneeComplete && i.DateDebut < slotEnd && i.DateFin > current);

                    string statut;
                    if (estPasse) statut = "passe";
                    else if (estOccupe) statut = "occupe";
                    else if (estIndispo) statut = "indisponible";
                    else statut = "disponible";

                    creneaux.Add(new {
                        dateHeure = current.ToString("yyyy-MM-ddTHH:mm:ss"),
                        heureDebut = current.ToString("HH:mm"),
                        heureFin = slotEnd.ToString("HH:mm"),
                        duree = duree,
                        statut = statut,
                        selectionnable = statut == "disponible"
                    });

                    current = current.AddMinutes(duree);
                }
            }

            return Ok(new {
                date = dateRecherche.ToString("yyyy-MM-dd"),
                medecinDisponible = creneaux.Any(c => ((dynamic)c).statut == "disponible"),
                creneaux = creneaux
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetCreneauxMedecinDisponibles: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupère les orientations d'un patient (pour le dossier médical)
    /// </summary>
    [HttpGet("patients/{idPatient}/orientations")]
    public async Task<IActionResult> GetOrientationsPatient(int idPatient)
    {
        try
        {
            var orientationsEntities = await _context.OrientationsPreConsultation
                .Include(o => o.Specialite)
                .Include(o => o.MedecinOriente).ThenInclude(m => m!.Utilisateur)
                .Include(o => o.MedecinPrescripteur).ThenInclude(m => m!.Utilisateur)
                .Include(o => o.Consultation)
                .Where(o => o.IdPatient == idPatient)
                .OrderByDescending(o => o.DateOrientation)
                .ToListAsync();

            var orientations = orientationsEntities.Select(o => MapToOrientationDto(o)).ToList();
            return Ok(orientations);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetOrientationsPatient: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Mapper une entité OrientationPreConsultation vers DTO
    /// </summary>
    private static OrientationPreConsultationDto MapToOrientationDto(OrientationPreConsultation o)
    {
        return new OrientationPreConsultationDto
        {
            IdOrientation = o.IdOrientation,
            IdConsultation = o.IdConsultation,
            IdPatient = o.IdPatient,
            TypeOrientation = o.TypeOrientation,
            IdSpecialite = o.IdSpecialite,
            NomSpecialite = o.Specialite?.NomSpecialite ?? o.SpecialiteTexte,
            IdMedecinOriente = o.IdMedecinOriente,
            NomMedecinOriente = o.MedecinOriente?.Utilisateur != null
                ? $"Dr. {o.MedecinOriente.Utilisateur.Prenom} {o.MedecinOriente.Utilisateur.Nom}"
                : null,
            NomDestinataire = o.NomDestinataire,
            SpecialiteTexte = o.SpecialiteTexte,
            AdresseDestinataire = o.AdresseDestinataire,
            TelephoneDestinataire = o.TelephoneDestinataire,
            Motif = o.Motif,
            Notes = o.Notes,
            Urgence = o.Urgence,
            Prioritaire = o.Prioritaire,
            Statut = o.Statut,
            DateOrientation = o.DateOrientation,
            DateRdvPropose = o.DateRdvPropose,
            IdRdvCree = o.IdRdvCree,
            MedecinPrescripteur = o.MedecinPrescripteur?.Utilisateur != null
                ? $"Dr. {o.MedecinPrescripteur.Utilisateur.Prenom} {o.MedecinPrescripteur.Utilisateur.Nom}"
                : null,
            CreatedAt = o.CreatedAt
        };
    }

    private Parametre GetOrCreateParametre(Consultation consultation, int medecinId)
    {
        if (consultation.Parametre != null)
        {
            return consultation.Parametre;
        }

        var parametre = new Parametre
        {
            IdConsultation = consultation.IdConsultation,
            DateEnregistrement = DateTime.UtcNow,
            EnregistrePar = medecinId
        };

        consultation.Parametre = parametre;
        _context.Parametres.Add(parametre);
        return parametre;
    }

    private static ExamenGynecologiqueDto? MapExamenGynecologique(ConsultationGynecologique? entity)
    {
        if (entity == null)
        {
            return null;
        }

        return new ExamenGynecologiqueDto
        {
            InspectionExterne = entity.InspectionExterne,
            ExamenSpeculum = entity.ExamenSpeculum,
            ToucherVaginal = entity.ToucherVaginal,
            AutresObservations = entity.AutresObservations
        };
    }
}

public class CreerRdvSuiviRequest
{
    public string DateHeure { get; set; } = string.Empty;
    public int? Duree { get; set; }
    public string? Motif { get; set; }
    public string? Notes { get; set; }
}

public class CloturerDossierRequest
{
    public string? Motif { get; set; }
}


public class PauseConsultationRequest
{
    public string? EtapeActuelle { get; set; }
}

public static class ConsultationHelpers
{
    /// <summary>
    /// Vérifie si le contenu de l'anamnèse est au format du questionnaire patient
    /// (format "- Question: Réponse" généré automatiquement)
    /// </summary>
    public static bool IsPatientQuestionnaireFormat(string? anamnese)
    {
        if (string.IsNullOrWhiteSpace(anamnese)) return false;
        // Le format du questionnaire patient commence par "- " et contient ":"
        var lines = anamnese.Split('\n');
        return lines.Length > 0 && lines.All(line => 
            string.IsNullOrWhiteSpace(line) || 
            (line.TrimStart().StartsWith("- ") && line.Contains(":")));
    }
}
