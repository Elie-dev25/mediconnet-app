using Mediconnet_Backend.Controllers.Base;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Medecin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// Contrôleur pour les consultations et patients du médecin
/// </summary>
[Route("api/medecin")]
public class MedecinDataController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MedecinDataController> _logger;

    public MedecinDataController(ApplicationDbContext context, ILogger<MedecinDataController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ==================== CONSULTATIONS ====================

    /// <summary>
    /// Obtenir les statistiques des consultations
    /// </summary>
    [HttpGet("consultations/stats")]
    public async Task<IActionResult> GetConsultationStats()
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var today = DateTime.UtcNow.Date;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + 1);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            // Compter les RDV terminés comme consultations
            var consultations = await _context.RendezVous
                .Where(r => r.IdMedecin == medecinId.Value)
                .ToListAsync();

            var stats = new ConsultationStatsDto
            {
                TotalConsultations = consultations.Count(r => r.Statut == "termine"),
                ConsultationsAujourdHui = consultations.Count(r => r.Statut == "termine" && r.DateHeure.Date == today),
                ConsultationsSemaine = consultations.Count(r => r.Statut == "termine" && r.DateHeure.Date >= startOfWeek),
                ConsultationsMois = consultations.Count(r => r.Statut == "termine" && r.DateHeure.Date >= startOfMonth),
                EnAttente = consultations.Count(r => r.Statut == "en_cours"),
                Terminees = consultations.Count(r => r.Statut == "termine")
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetConsultationStats: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Obtenir la liste des consultations
    /// </summary>
    [HttpGet("consultations")]
    public async Task<IActionResult> GetConsultations(
        [FromQuery] DateTime? dateDebut,
        [FromQuery] DateTime? dateFin,
        [FromQuery] string? statut)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            // Récupérer la spécialité du médecin
            var medecin = await _context.Medecins
                .Where(m => m.IdUser == medecinId.Value)
                .Select(m => new { m.IdSpecialite })
                .FirstOrDefaultAsync();
            var specialiteId = medecin?.IdSpecialite ?? 0;

            var query = _context.RendezVous
                .Include(r => r.Patient)
                    .ThenInclude(p => p!.Utilisateur)
                .Where(r => r.IdMedecin == medecinId.Value)
                .Where(r => r.Statut == "en_cours" || r.Statut == "termine")
                .AsQueryable();

            if (dateDebut.HasValue)
                query = query.Where(r => r.DateHeure >= dateDebut.Value);

            if (dateFin.HasValue)
                query = query.Where(r => r.DateHeure <= dateFin.Value);

            if (!string.IsNullOrEmpty(statut))
                query = query.Where(r => r.Statut == statut);

            var consultations = await query
                .OrderByDescending(r => r.DateHeure)
                .Take(100)
                .Select(r => new ConsultationDto
                {
                    IdConsultation = _context.Consultations
                        .Where(c => c.IdRendezVous == r.IdRendezVous)
                        .Select(c => c.IdConsultation)
                        .FirstOrDefault(),
                    IdRendezVous = r.IdRendezVous,
                    IdPatient = r.IdPatient,
                    PatientNom = r.Patient!.Utilisateur!.Nom,
                    PatientPrenom = r.Patient.Utilisateur.Prenom,
                    NumeroDossier = r.Patient.NumeroDossier,
                    DateConsultation = r.DateHeure,
                    Motif = r.Motif ?? "",
                    Diagnostic = r.Notes,
                    Notes = r.Notes,
                    Statut = r.Statut == "termine" ? "terminee" : "en_cours",
                    Duree = r.Duree,
                    HasOrdonnance = false,
                    HasExamens = false,
                    IsPremiereConsultation = !_context.Consultations
                        .Any(c => c.IdPatient == r.IdPatient && 
                                  c.IdMedecin == medecinId.Value && 
                                  c.Statut == "terminee"),
                    SpecialiteId = specialiteId
                })
                .ToListAsync();

            return Ok(consultations);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetConsultations: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Obtenir les consultations du jour (en cours et à faire)
    /// </summary>
    [HttpGet("consultations/jour")]
    public async Task<IActionResult> GetConsultationsJour([FromQuery] DateTime? date)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            // Récupérer la spécialité du médecin
            var medecin = await _context.Medecins
                .Where(m => m.IdUser == medecinId.Value)
                .Select(m => new { m.IdSpecialite })
                .FirstOrDefaultAsync();
            var specialiteId = medecin?.IdSpecialite ?? 0;

            var targetDate = date?.Date ?? DateTime.UtcNow.Date;

            var consultations = await _context.RendezVous
                .Include(r => r.Patient)
                    .ThenInclude(p => p!.Utilisateur)
                .Where(r => r.IdMedecin == medecinId.Value)
                .Where(r => r.DateHeure.Date == targetDate)
                .Where(r => r.Statut == "confirme" || r.Statut == "en_cours" || r.Statut == "termine")
                .OrderBy(r => r.DateHeure)
                .Select(r => new ConsultationDto
                {
                    IdConsultation = _context.Consultations
                        .Where(c => c.IdRendezVous == r.IdRendezVous)
                        .Select(c => c.IdConsultation)
                        .FirstOrDefault(),
                    IdRendezVous = r.IdRendezVous,
                    IdPatient = r.IdPatient,
                    PatientNom = r.Patient!.Utilisateur!.Nom,
                    PatientPrenom = r.Patient.Utilisateur.Prenom,
                    NumeroDossier = r.Patient.NumeroDossier,
                    DateConsultation = r.DateHeure,
                    Motif = r.Motif ?? "",
                    Diagnostic = r.Notes,
                    Notes = r.Notes,
                    Statut = r.Statut == "termine" ? "terminee" : (r.Statut == "en_cours" ? "en_cours" : "a_faire"),
                    Duree = r.Duree,
                    HasOrdonnance = false,
                    HasExamens = false,
                    IsPremiereConsultation = !_context.Consultations
                        .Any(c => c.IdPatient == r.IdPatient && 
                                  c.IdMedecin == medecinId.Value && 
                                  c.Statut == "terminee"),
                    SpecialiteId = specialiteId
                })
                .ToListAsync();

            return Ok(consultations);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetConsultationsJour: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    // ==================== PATIENTS ====================

    /// <summary>
    /// Obtenir les statistiques des patients
    /// </summary>
    [HttpGet("patients/stats")]
    public async Task<IActionResult> GetPatientStats()
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            // Patients uniques qui ont eu un RDV avec ce médecin
            var patientIds = await _context.RendezVous
                .Where(r => r.IdMedecin == medecinId.Value)
                .Select(r => r.IdPatient)
                .Distinct()
                .ToListAsync();

            // Nouveaux patients ce mois (première consultation avec ce médecin ce mois)
            var nouveauxIds = await _context.RendezVous
                .Where(r => r.IdMedecin == medecinId.Value)
                .GroupBy(r => r.IdPatient)
                .Where(g => g.Min(r => r.DateHeure) >= startOfMonth)
                .Select(g => g.Key)
                .ToListAsync();

            // Patients avec RDV planifié
            var avecRdv = await _context.RendezVous
                .Where(r => r.IdMedecin == medecinId.Value)
                .Where(r => r.DateHeure > DateTime.UtcNow)
                .Where(r => r.Statut == "planifie" || r.Statut == "confirme")
                .Select(r => r.IdPatient)
                .Distinct()
                .CountAsync();

            return Ok(new MedecinPatientStatsDto
            {
                TotalPatients = patientIds.Count,
                NouveauxCeMois = nouveauxIds.Count,
                AvecRdvPlanifie = avecRdv
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetPatientStats: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Obtenir la liste des patients du médecin
    /// </summary>
    [HttpGet("patients")]
    public async Task<IActionResult> GetPatients([FromQuery] string? recherche)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            // Récupérer tous les patients qui ont eu un RDV avec ce médecin
            var patientIdsQuery = _context.RendezVous
                .Where(r => r.IdMedecin == medecinId.Value)
                .Select(r => r.IdPatient)
                .Distinct();

            var query = _context.Patients
                .Include(p => p.Utilisateur)
                .Where(p => patientIdsQuery.Contains(p.IdUser))
                .AsQueryable();

            // Recherche
            if (!string.IsNullOrEmpty(recherche))
            {
                var term = recherche.ToLower();
                query = query.Where(p =>
                    p.Utilisateur!.Nom.ToLower().Contains(term) ||
                    p.Utilisateur.Prenom.ToLower().Contains(term) ||
                    (p.NumeroDossier != null && p.NumeroDossier.ToLower().Contains(term)) ||
                    (p.Utilisateur.Telephone != null && p.Utilisateur.Telephone.Contains(term))
                );
            }

            var patients = await query
                .OrderBy(p => p.Utilisateur!.Nom)
                .ThenBy(p => p.Utilisateur!.Prenom)
                .Take(100)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var result = new List<MedecinPatientDto>();

            foreach (var patient in patients)
            {
                // Dernière visite
                var derniereVisite = await _context.RendezVous
                    .Where(r => r.IdMedecin == medecinId.Value && r.IdPatient == patient.IdUser)
                    .Where(r => r.Statut == "termine")
                    .OrderByDescending(r => r.DateHeure)
                    .Select(r => r.DateHeure)
                    .FirstOrDefaultAsync();

                // Prochaine visite
                var prochaineVisite = await _context.RendezVous
                    .Where(r => r.IdMedecin == medecinId.Value && r.IdPatient == patient.IdUser)
                    .Where(r => r.DateHeure > now)
                    .Where(r => r.Statut == "planifie" || r.Statut == "confirme")
                    .OrderBy(r => r.DateHeure)
                    .Select(r => r.DateHeure)
                    .FirstOrDefaultAsync();

                // Nombre de consultations
                var nbConsultations = await _context.RendezVous
                    .Where(r => r.IdMedecin == medecinId.Value && r.IdPatient == patient.IdUser)
                    .Where(r => r.Statut == "termine")
                    .CountAsync();

                int? age = null;
                if (patient.Utilisateur?.Naissance.HasValue == true)
                {
                    age = now.Year - patient.Utilisateur.Naissance.Value.Year;
                    if (now.DayOfYear < patient.Utilisateur.Naissance.Value.DayOfYear) age--;
                }

                result.Add(new MedecinPatientDto
                {
                    IdPatient = patient.IdUser,
                    IdUser = patient.IdUser,
                    Nom = patient.Utilisateur?.Nom ?? "",
                    Prenom = patient.Utilisateur?.Prenom ?? "",
                    NumeroDossier = patient.NumeroDossier,
                    Telephone = patient.Utilisateur?.Telephone,
                    Email = patient.Utilisateur?.Email,
                    Sexe = patient.Utilisateur?.Sexe,
                    Age = age,
                    DerniereVisite = derniereVisite == default ? null : derniereVisite,
                    ProchaineVisite = prochaineVisite == default ? null : prochaineVisite,
                    NombreConsultations = nbConsultations,
                    GroupeSanguin = patient.GroupeSanguin
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetPatients: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Obtenir le détail d'un patient
    /// </summary>
    [HttpGet("patients/{idPatient}")]
    public async Task<IActionResult> GetPatientDetail(int idPatient)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            // Vérifier que le patient a bien eu un RDV avec ce médecin
            var hasRdv = await _context.RendezVous
                .AnyAsync(r => r.IdMedecin == medecinId.Value && r.IdPatient == idPatient);

            if (!hasRdv)
                return NotFound(new { message = "Patient non trouvé" });

            var patient = await _context.Patients
                .Include(p => p.Utilisateur)
                .FirstOrDefaultAsync(p => p.IdUser == idPatient);

            if (patient == null)
                return NotFound(new { message = "Patient non trouvé" });

            // Dernières consultations - récupérer depuis la table Consultations
            var dernieresConsultations = await _context.Consultations
                .Where(c => c.IdMedecin == medecinId.Value && c.IdPatient == idPatient)
                .OrderByDescending(c => c.DateHeure)
                .Take(10)
                .Select(c => new ConsultationHistoriqueDto
                {
                    IdConsultation = c.IdConsultation,
                    DateConsultation = c.DateHeure,
                    Motif = c.Motif ?? "",
                    Diagnostic = c.Diagnostic
                })
                .ToListAsync();

            // Si aucune consultation, fallback sur les RDV terminés
            if (dernieresConsultations.Count == 0)
            {
                dernieresConsultations = await _context.RendezVous
                    .Where(r => r.IdMedecin == medecinId.Value && r.IdPatient == idPatient)
                    .Where(r => r.Statut == "termine")
                    .OrderByDescending(r => r.DateHeure)
                    .Take(10)
                    .Select(r => new ConsultationHistoriqueDto
                    {
                        IdConsultation = r.IdRendezVous,
                        DateConsultation = r.DateHeure,
                        Motif = r.Motif ?? "",
                        Diagnostic = r.Notes
                    })
                    .ToListAsync();
            }

            // Prochains RDV
            var prochainsRdv = await _context.RendezVous
                .Where(r => r.IdMedecin == medecinId.Value && r.IdPatient == idPatient)
                .Where(r => r.DateHeure > DateTime.UtcNow)
                .Where(r => r.Statut == "planifie" || r.Statut == "confirme")
                .OrderBy(r => r.DateHeure)
                .Take(5)
                .Select(r => new RendezVousHistoriqueDto
                {
                    IdRendezVous = r.IdRendezVous,
                    DateHeure = r.DateHeure,
                    Motif = r.Motif ?? "",
                    Statut = r.Statut
                })
                .ToListAsync();

            return Ok(new MedecinPatientDetailDto
            {
                IdPatient = patient.IdUser,
                IdUser = patient.IdUser,
                Nom = patient.Utilisateur?.Nom ?? "",
                Prenom = patient.Utilisateur?.Prenom ?? "",
                NumeroDossier = patient.NumeroDossier,
                Telephone = patient.Utilisateur?.Telephone,
                Email = patient.Utilisateur?.Email,
                Sexe = patient.Utilisateur?.Sexe,
                Naissance = patient.Utilisateur?.Naissance,
                Adresse = patient.Utilisateur?.Adresse,
                GroupeSanguin = patient.GroupeSanguin,
                Ethnie = patient.Ethnie,
                PersonneContact = patient.PersonneContact,
                NumeroContact = patient.NumeroContact,
                Profession = patient.Profession,
                DernieresConsultations = dernieresConsultations,
                ProchainsRdv = prochainsRdv
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetPatientDetail: {ex.Message}");
            return StatusCode(500, new { message = "Erreur serveur" });
        }
    }
}
