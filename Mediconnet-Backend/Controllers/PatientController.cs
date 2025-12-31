using Mediconnet_Backend.Controllers.Base;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// Controleur pour la gestion du profil patient
/// </summary>
[Route("api/[controller]")]
public class PatientController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PatientController> _logger;
    private readonly IPatientService _patientService;

    public PatientController(
        ApplicationDbContext context, 
        ILogger<PatientController> logger,
        IPatientService patientService)
    {
        _context = context;
        _logger = logger;
        _patientService = patientService;
    }

    /// <summary>
    /// Obtenir le profil complet du patient connecte
    /// </summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var utilisateur = await _context.Utilisateurs
                .Include(u => u.Patient)
                .FirstOrDefaultAsync(u => u.IdUser == userId.Value);

            if (utilisateur == null || utilisateur.Patient == null)
                return NotFound(new { message = "Patient non trouve" });

            var profile = new PatientProfileDto
            {
                IdUser = utilisateur.IdUser,
                Nom = utilisateur.Nom,
                Prenom = utilisateur.Prenom,
                Email = utilisateur.Email,
                Naissance = utilisateur.Naissance,
                Sexe = utilisateur.Sexe,
                Telephone = utilisateur.Telephone,
                SituationMatrimoniale = utilisateur.SituationMatrimoniale,
                Adresse = utilisateur.Adresse,
                Photo = utilisateur.Photo,
                NumeroDossier = utilisateur.Patient.NumeroDossier,
                Ethnie = utilisateur.Patient.Ethnie,
                GroupeSanguin = utilisateur.Patient.GroupeSanguin,
                NbEnfants = utilisateur.Patient.NbEnfants,
                PersonneContact = utilisateur.Patient.PersonneContact,
                NumeroContact = utilisateur.Patient.NumeroContact,
                Profession = utilisateur.Patient.Profession,
                CreatedAt = utilisateur.CreatedAt,
                IsProfileComplete = IsProfileComplete(utilisateur)
            };

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting patient profile: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la recuperation du profil" });
        }
    }

    /// <summary>
    /// Verifier si le profil du patient est complet
    /// </summary>
    [HttpGet("profile/status")]
    public async Task<IActionResult> GetProfileStatus()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var utilisateur = await _context.Utilisateurs
                .Include(u => u.Patient)
                .FirstOrDefaultAsync(u => u.IdUser == userId.Value);

            if (utilisateur == null)
                return NotFound();

            var missingFields = new List<string>();

            if (!utilisateur.Naissance.HasValue) missingFields.Add("Date de naissance");
            if (string.IsNullOrEmpty(utilisateur.Sexe)) missingFields.Add("Sexe");
            if (string.IsNullOrEmpty(utilisateur.Telephone)) missingFields.Add("Telephone");
            if (string.IsNullOrEmpty(utilisateur.Adresse)) missingFields.Add("Adresse");

            if (utilisateur.Patient != null)
            {
                if (string.IsNullOrEmpty(utilisateur.Patient.PersonneContact)) missingFields.Add("Personne a contacter");
                if (string.IsNullOrEmpty(utilisateur.Patient.NumeroContact)) missingFields.Add("Numero de contact d'urgence");
            }

            var status = new ProfileStatusDto
            {
                IsComplete = missingFields.Count == 0,
                MissingFields = missingFields,
                Message = missingFields.Count == 0 
                    ? "Votre profil est complet" 
                    : $"Il manque {missingFields.Count} information(s) dans votre profil"
            };

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error checking profile status: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la verification du profil" });
        }
    }

    /// <summary>
    /// Mettre a jour le profil du patient
    /// </summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdatePatientProfileRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var utilisateur = await _context.Utilisateurs
                .Include(u => u.Patient)
                .FirstOrDefaultAsync(u => u.IdUser == userId.Value);

            if (utilisateur == null)
                return NotFound(new { message = "Utilisateur non trouve" });

            // Mise a jour des informations utilisateur
            if (request.Naissance.HasValue) utilisateur.Naissance = request.Naissance;
            if (!string.IsNullOrEmpty(request.Sexe)) utilisateur.Sexe = request.Sexe;
            if (!string.IsNullOrEmpty(request.Telephone)) utilisateur.Telephone = request.Telephone;
            if (!string.IsNullOrEmpty(request.SituationMatrimoniale)) utilisateur.SituationMatrimoniale = request.SituationMatrimoniale;
            if (!string.IsNullOrEmpty(request.Adresse)) utilisateur.Adresse = request.Adresse;

            // Mise a jour des informations patient
            if (utilisateur.Patient != null)
            {
                if (!string.IsNullOrEmpty(request.Ethnie)) utilisateur.Patient.Ethnie = request.Ethnie;
                if (!string.IsNullOrEmpty(request.GroupeSanguin)) utilisateur.Patient.GroupeSanguin = request.GroupeSanguin;
                if (request.NbEnfants.HasValue) utilisateur.Patient.NbEnfants = request.NbEnfants;
                if (!string.IsNullOrEmpty(request.PersonneContact)) utilisateur.Patient.PersonneContact = request.PersonneContact;
                if (!string.IsNullOrEmpty(request.NumeroContact)) utilisateur.Patient.NumeroContact = request.NumeroContact;
                if (!string.IsNullOrEmpty(request.Profession)) utilisateur.Patient.Profession = request.Profession;
            }

            utilisateur.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Profile updated for user {userId}");

            return Ok(new { message = "Profil mis a jour avec succes", isComplete = IsProfileComplete(utilisateur) });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating patient profile: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la mise a jour du profil" });
        }
    }

    /// <summary>
    /// Obtenir les statistiques du dashboard patient
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var now = DateTime.Now;

            // Visites à venir (rendez-vous futurs confirmés ou planifiés)
            var visitesAVenir = await _context.RendezVous
                .Include(r => r.Medecin).ThenInclude(m => m!.Utilisateur)
                .Include(r => r.Medecin).ThenInclude(m => m!.Service)
                .Include(r => r.Service)
                .Where(r => r.IdPatient == userId.Value && 
                           r.DateHeure > now && 
                           (r.Statut == "planifie" || r.Statut == "confirme"))
                .OrderBy(r => r.DateHeure)
                .Take(5)
                .Select(r => new VisiteDto
                {
                    IdRendezVous = r.IdRendezVous,
                    DateHeure = r.DateHeure,
                    Duree = r.Duree,
                    Statut = r.Statut,
                    TypeRdv = r.TypeRdv,
                    Motif = r.Motif,
                    NomMedecin = r.Medecin != null && r.Medecin.Utilisateur != null 
                        ? $"Dr. {r.Medecin.Utilisateur.Prenom} {r.Medecin.Utilisateur.Nom}" 
                        : "Médecin",
                    Service = r.Service != null ? r.Service.NomService : 
                             (r.Medecin != null && r.Medecin.Service != null ? r.Medecin.Service.NomService : null)
                })
                .ToListAsync();

            // Visites passées (rendez-vous terminés)
            var visitesPassees = await _context.RendezVous
                .Include(r => r.Medecin).ThenInclude(m => m!.Utilisateur)
                .Include(r => r.Service)
                .Where(r => r.IdPatient == userId.Value && 
                           (r.Statut == "termine" || (r.DateHeure < now && r.Statut != "annule")))
                .OrderByDescending(r => r.DateHeure)
                .Take(5)
                .Select(r => new VisiteDto
                {
                    IdRendezVous = r.IdRendezVous,
                    DateHeure = r.DateHeure,
                    Duree = r.Duree,
                    Statut = r.Statut,
                    TypeRdv = r.TypeRdv,
                    Motif = r.Motif,
                    NomMedecin = r.Medecin != null && r.Medecin.Utilisateur != null 
                        ? $"Dr. {r.Medecin.Utilisateur.Prenom} {r.Medecin.Utilisateur.Nom}" 
                        : "Médecin",
                    Service = r.Service != null ? r.Service.NomService : null
                })
                .ToListAsync();

            // Statistiques globales
            var totalRdv = await _context.RendezVous
                .CountAsync(r => r.IdPatient == userId.Value && r.Statut != "annule");
            
            var rdvAVenir = await _context.RendezVous
                .CountAsync(r => r.IdPatient == userId.Value && 
                           r.DateHeure > now && 
                           (r.Statut == "planifie" || r.Statut == "confirme"));

            var rdvPasses = await _context.RendezVous
                .CountAsync(r => r.IdPatient == userId.Value && 
                           (r.Statut == "termine" || r.DateHeure < now) && 
                           r.Statut != "annule");

            // TODO: Ajouter traitements prévus quand la table prescription sera utilisée
            var traitementsPrevus = new List<TraitementDto>();

            var dashboard = new PatientDashboardDto
            {
                VisitesAVenir = visitesAVenir,
                VisitesPassees = visitesPassees,
                TraitementsPrevus = traitementsPrevus,
                Stats = new PatientStatsDto
                {
                    TotalRendezVous = totalRdv,
                    RendezVousAVenir = rdvAVenir,
                    RendezVousPasses = rdvPasses,
                    Ordonnances = 0, // À implémenter
                    Examens = 0,     // À implémenter
                    Factures = 0     // À implémenter
                }
            };

            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting patient dashboard: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération du dashboard" });
        }
    }

    /// <summary>
    /// Récupérer les 6 patients les plus récemment enregistrés
    /// Accessible par: infirmier, medecin, administrateur, accueil
    /// </summary>
    [HttpGet("recent")]
    [Authorize(Roles = "infirmier,medecin,administrateur,accueil")]
    public async Task<IActionResult> GetRecentPatients([FromQuery] int count = 6)
    {
        try
        {
            var response = await _patientService.GetRecentPatientsAsync(count);
            
            if (!response.Success)
            {
                return BadRequest(new { message = response.Message });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des patients récents");
            return StatusCode(500, new { message = "Erreur serveur lors de la récupération des patients" });
        }
    }

    /// <summary>
    /// Rechercher des patients par numéro de dossier, nom ou email
    /// Accessible par: infirmier, medecin, administrateur, accueil
    /// </summary>
    [HttpPost("search")]
    [Authorize(Roles = "infirmier,medecin,administrateur,accueil")]
    public async Task<IActionResult> SearchPatients([FromBody] PatientSearchRequest request)
    {
        try
        {
            var response = await _patientService.SearchPatientsAsync(request);
            
            if (!response.Success)
            {
                return BadRequest(new { message = response.Message });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la recherche de patients");
            return StatusCode(500, new { message = "Erreur serveur lors de la recherche" });
        }
    }

    /// <summary>
    /// Récupérer un patient par son ID
    /// Accessible par: infirmier, medecin, administrateur, accueil
    /// </summary>
    [HttpGet("{patientId:int}")]
    [Authorize(Roles = "infirmier,medecin,administrateur,accueil")]
    public async Task<IActionResult> GetPatientById(int patientId)
    {
        try
        {
            var patient = await _context.Patients
                .Include(p => p.Utilisateur)
                .FirstOrDefaultAsync(p => p.IdUser == patientId);

            if (patient == null || patient.Utilisateur == null)
            {
                return NotFound(new { success = false, message = "Patient non trouvé" });
            }

            var patientInfo = new
            {
                idUser = patient.IdUser,
                numeroDossier = patient.NumeroDossier,
                nom = patient.Utilisateur.Nom,
                prenom = patient.Utilisateur.Prenom,
                email = patient.Utilisateur.Email,
                telephone = patient.Utilisateur.Telephone,
                dateNaissance = patient.Utilisateur.Naissance,
                sexe = patient.Utilisateur.Sexe,
                groupeSanguin = patient.GroupeSanguin,
                createdAt = patient.Utilisateur.CreatedAt
            };

            return Ok(new { success = true, patient = patientInfo });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération du patient {PatientId}", patientId);
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer le dossier médical complet du patient connecté
    /// </summary>
    [HttpGet("dossier-medical")]
    public async Task<IActionResult> GetDossierMedical()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            // Récupérer le patient avec ses informations
            var utilisateur = await _context.Utilisateurs
                .Include(u => u.Patient)
                .FirstOrDefaultAsync(u => u.IdUser == userId.Value);

            if (utilisateur == null || utilisateur.Patient == null)
                return NotFound(new { message = "Patient non trouvé" });

            var patient = utilisateur.Patient;

            // Profil patient
            var patientProfile = new PatientProfileDto
            {
                IdUser = utilisateur.IdUser,
                Nom = utilisateur.Nom,
                Prenom = utilisateur.Prenom,
                Email = utilisateur.Email,
                Naissance = utilisateur.Naissance,
                Sexe = utilisateur.Sexe,
                Telephone = utilisateur.Telephone,
                NumeroDossier = patient.NumeroDossier,
                GroupeSanguin = patient.GroupeSanguin,
                Adresse = utilisateur.Adresse,
                IsProfileComplete = IsProfileComplete(utilisateur)
            };

            // Antécédents médicaux (extraits des champs du patient)
            var antecedents = new List<AntecedentDto>();
            
            if (!string.IsNullOrEmpty(patient.MaladiesChroniques))
            {
                foreach (var maladie in patient.MaladiesChroniques.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    antecedents.Add(new AntecedentDto
                    {
                        Type = "medical",
                        Description = maladie.Trim(),
                        Actif = true
                    });
                }
            }

            if (patient.OperationsChirurgicales == true && !string.IsNullOrEmpty(patient.OperationsDetails))
            {
                antecedents.Add(new AntecedentDto
                {
                    Type = "chirurgical",
                    Description = patient.OperationsDetails,
                    Actif = false
                });
            }

            if (patient.AntecedentsFamiliaux == true && !string.IsNullOrEmpty(patient.AntecedentsFamiliauxDetails))
            {
                foreach (var ant in patient.AntecedentsFamiliauxDetails.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    antecedents.Add(new AntecedentDto
                    {
                        Type = "familial",
                        Description = ant.Trim(),
                        Actif = true
                    });
                }
            }

            // Allergies (extraites des champs du patient)
            var allergies = new List<AllergieDto>();
            if (patient.AllergiesConnues == true && !string.IsNullOrEmpty(patient.AllergiesDetails))
            {
                foreach (var allergie in patient.AllergiesDetails.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    allergies.Add(new AllergieDto
                    {
                        Type = "non_specifie",
                        Allergene = allergie.Trim(),
                        Severite = "moderate"
                    });
                }
            }

            // Consultations du patient
            var consultations = await _context.Consultations
                .Include(c => c.Medecin).ThenInclude(m => m!.Utilisateur)
                .Include(c => c.Medecin).ThenInclude(m => m!.Specialite)
                .Where(c => c.IdPatient == userId.Value)
                .OrderByDescending(c => c.DateHeure)
                .Take(20)
                .Select(c => new ConsultationHistoryDto
                {
                    IdConsultation = c.IdConsultation,
                    DateConsultation = c.DateHeure,
                    Motif = c.Motif ?? "Non spécifié",
                    DiagnosticPrincipal = c.Diagnostic,
                    NomMedecin = c.Medecin != null && c.Medecin.Utilisateur != null
                        ? $"Dr. {c.Medecin.Utilisateur.Prenom} {c.Medecin.Utilisateur.Nom}"
                        : "Médecin",
                    Specialite = c.Medecin != null && c.Medecin.Specialite != null
                        ? c.Medecin.Specialite.NomSpecialite
                        : null,
                    Statut = c.Statut ?? "termine"
                })
                .ToListAsync();

            // Ordonnances du patient
            var ordonnances = await _context.Ordonnances
                .Include(o => o.Consultation).ThenInclude(c => c!.Medecin).ThenInclude(m => m!.Utilisateur)
                .Include(o => o.Medicaments!).ThenInclude(pm => pm.Medicament)
                .Where(o => o.Consultation != null && o.Consultation.IdPatient == userId.Value)
                .OrderByDescending(o => o.Date)
                .Take(10)
                .Select(o => new OrdonnanceDto
                {
                    IdOrdonnance = o.IdOrdonnance,
                    DateOrdonnance = o.Date,
                    NomMedecin = o.Consultation != null && o.Consultation.Medecin != null && o.Consultation.Medecin.Utilisateur != null
                        ? $"Dr. {o.Consultation.Medecin.Utilisateur.Prenom} {o.Consultation.Medecin.Utilisateur.Nom}"
                        : "Médecin",
                    Statut = o.Consultation != null && o.Consultation.DateHeure > DateTime.UtcNow.AddDays(-30) ? "active" : "terminee",
                    Medicaments = o.Medicaments != null ? o.Medicaments.Select(pm => new MedicamentPrescritDto
                    {
                        Nom = pm.Medicament != null ? pm.Medicament.Nom : "Médicament",
                        Dosage = pm.Medicament != null ? pm.Medicament.Dosage ?? "" : "",
                        Frequence = pm.Posologie ?? "",
                        Duree = pm.DureeTraitement ?? "",
                        Instructions = null
                    }).ToList() : new List<MedicamentPrescritDto>()
                })
                .ToListAsync();

            // Examens du patient
            var examens = await _context.BulletinsExamen
                .Include(be => be.Consultation).ThenInclude(c => c!.Medecin).ThenInclude(m => m!.Utilisateur)
                .Include(be => be.Examen)
                .Where(be => be.Consultation != null && be.Consultation.IdPatient == userId.Value)
                .OrderByDescending(be => be.DateDemande)
                .Take(15)
                .Select(be => new ExamenDto
                {
                    IdExamen = be.IdBulletinExamen,
                    DateExamen = be.DateDemande,
                    TypeExamen = be.Examen != null ? be.Examen.TypeExamen ?? "autre" : "autre",
                    NomExamen = be.Examen != null ? be.Examen.NomExamen : "Examen",
                    Resultat = null, // À implémenter quand les résultats seront disponibles
                    NomMedecin = be.Consultation != null && be.Consultation.Medecin != null && be.Consultation.Medecin.Utilisateur != null
                        ? $"Dr. {be.Consultation.Medecin.Utilisateur.Prenom} {be.Consultation.Medecin.Utilisateur.Nom}"
                        : "Médecin",
                    Statut = "termine",
                    Urgent = false
                })
                .ToListAsync();

            // Statistiques
            var totalConsultations = await _context.Consultations
                .CountAsync(c => c.IdPatient == userId.Value);
            
            var totalOrdonnances = await _context.Ordonnances
                .CountAsync(o => o.Consultation != null && o.Consultation.IdPatient == userId.Value);
            
            var totalExamens = await _context.BulletinsExamen
                .CountAsync(be => be.Consultation != null && be.Consultation.IdPatient == userId.Value);

            var derniereVisite = await _context.Consultations
                .Where(c => c.IdPatient == userId.Value && c.Statut == "termine")
                .OrderByDescending(c => c.DateHeure)
                .Select(c => c.DateHeure)
                .FirstOrDefaultAsync();

            var dossier = new DossierMedicalDto
            {
                Patient = patientProfile,
                Antecedents = antecedents,
                Allergies = allergies,
                Consultations = consultations,
                Ordonnances = ordonnances,
                Examens = examens,
                Stats = new DossierStatsDto
                {
                    TotalConsultations = totalConsultations,
                    TotalOrdonnances = totalOrdonnances,
                    TotalExamens = totalExamens,
                    DerniereVisite = derniereVisite != default ? derniereVisite : null
                }
            };

            return Ok(dossier);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting patient medical record: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération du dossier médical" });
        }
    }

    private bool IsProfileComplete(Core.Entities.Utilisateur utilisateur)
    {
        if (!utilisateur.Naissance.HasValue) return false;
        if (string.IsNullOrEmpty(utilisateur.Sexe)) return false;
        if (string.IsNullOrEmpty(utilisateur.Telephone)) return false;
        if (string.IsNullOrEmpty(utilisateur.Adresse)) return false;
        if (utilisateur.Patient == null) return false;
        if (string.IsNullOrEmpty(utilisateur.Patient.PersonneContact)) return false;
        if (string.IsNullOrEmpty(utilisateur.Patient.NumeroContact)) return false;
        return true;
    }
}
