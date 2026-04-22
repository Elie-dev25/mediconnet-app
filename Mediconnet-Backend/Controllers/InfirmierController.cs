using Mediconnet_Backend.Controllers.Base;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Hospitalisation;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Controllers;

[Route("api/infirmier")]
[Authorize(Roles = "infirmier,administrateur")]
public class InfirmierController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InfirmierController> _logger;
    private readonly IHospitalisationService _hospitalisationService;
    private readonly INotificationService _notificationService;

    public InfirmierController(
        ApplicationDbContext context, 
        ILogger<InfirmierController> logger,
        IHospitalisationService hospitalisationService,
        INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _hospitalisationService = hospitalisationService;
        _notificationService = notificationService;
    }

    /// <summary>
    /// File d'attente infirmier - Patients à voir pour prise de paramètres
    /// Inclut:
    /// - Patients arrivés à l'accueil avec facture payée
    /// - Patients avec RDV en ligne confirmé (facture payée) qui sont arrivés
    /// </summary>
    [HttpGet("file-attente")]
    public async Task<IActionResult> GetFileAttente()
    {
        try
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Patients à voir par l'infirmier:
            // - facture consultation payée
            // - consultation liée à la facture
            // - RDV du jour lié à la consultation avec statut "confirme" (payé)
            // - paramètres NON saisis (parametre absent)
            // - consultation PAS encore prêt consultation
            // - patient marqué comme arrivé (RDV.Statut = "confirme" ou "arrive")
            var items = await _context.Consultations
                .Include(c => c.Patient)
                    .ThenInclude(p => p!.Utilisateur)
                .Include(c => c.Medecin)
                    .ThenInclude(m => m!.Utilisateur)
                .Include(c => c.RendezVous)
                .Where(c => c.IdRendezVous.HasValue)
                .Where(c => !_context.Parametres.Any(p => p.IdConsultation == c.IdConsultation))
                .Where(c => c.Statut == null || c.Statut == "planifie" || c.Statut == "arrive")
                .Where(c => _context.Factures.Any(f =>
                    f.IdConsultation == c.IdConsultation &&
                    f.TypeFacture == "consultation" &&
                    f.Statut == "payee"))
                .Where(c => _context.RendezVous.Any(r =>
                    r.IdRendezVous == c.IdRendezVous &&
                    r.DateHeure >= today &&
                    r.DateHeure < tomorrow &&
                    (r.Statut == "confirme" || r.Statut == "arrive"))) // RDV confirmé (payé) ou patient arrivé
                .OrderBy(c => c.RendezVous != null ? c.RendezVous.DateHeure : c.DateHeure)
                .Select(c => new
                {
                    idConsultation = c.IdConsultation,
                    idRendezVous = c.IdRendezVous,
                    idPatient = c.IdPatient,
                    patientNom = c.Patient != null && c.Patient.Utilisateur != null ? c.Patient.Utilisateur.Nom : "",
                    patientPrenom = c.Patient != null && c.Patient.Utilisateur != null ? c.Patient.Utilisateur.Prenom : "",
                    numeroDossier = c.Patient != null ? c.Patient.NumeroDossier : null,
                    idMedecin = c.IdMedecin,
                    medecinNom = c.Medecin != null && c.Medecin.Utilisateur != null ? c.Medecin.Utilisateur.Nom : "",
                    medecinPrenom = c.Medecin != null && c.Medecin.Utilisateur != null ? c.Medecin.Utilisateur.Prenom : "",
                    dateHeure = c.RendezVous != null ? c.RendezVous.DateHeure : c.DateHeure,
                    statutConsultation = c.Statut,
                    statutRdv = c.RendezVous != null ? c.RendezVous.Statut : null,
                    sourceRdvEnLigne = c.RendezVous != null && c.RendezVous.DateCreation < today // RDV créé avant aujourd'hui = en ligne
                })
                .ToListAsync();

            return Ok(new { success = true, data = items, count = items.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur GetFileAttente infirmier");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Marquer l'arrivée d'un patient (pour les RDV pris en ligne)
    /// Le patient ne passe pas par l'accueil, il va directement à l'infirmier
    /// </summary>
    [HttpPost("marquer-arrivee/{idRendezVous}")]
    public async Task<IActionResult> MarquerArriveePatient(int idRendezVous)
    {
        try
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Trouver le RDV
            var rdv = await _context.RendezVous
                .Include(r => r.Patient)
                    .ThenInclude(p => p!.Utilisateur)
                .FirstOrDefaultAsync(r => r.IdRendezVous == idRendezVous);

            if (rdv == null)
                return NotFound(new { success = false, message = "Rendez-vous introuvable" });

            // Vérifier que c'est un RDV du jour
            if (rdv.DateHeure.Date != today)
                return BadRequest(new { success = false, message = "Ce rendez-vous n'est pas prévu pour aujourd'hui" });

            // Vérifier que le RDV est confirmé (payé)
            if (rdv.Statut != "confirme")
                return BadRequest(new { success = false, message = $"Ce rendez-vous n'est pas confirmé (statut actuel: {rdv.Statut}). Le patient doit d'abord payer." });

            // Vérifier qu'une facture payée existe
            var consultation = await _context.Consultations
                .FirstOrDefaultAsync(c => c.IdRendezVous == idRendezVous);

            if (consultation == null)
                return BadRequest(new { success = false, message = "Aucune consultation liée à ce rendez-vous" });

            var facturePaye = await _context.Factures
                .AnyAsync(f => f.IdConsultation == consultation.IdConsultation && 
                              f.TypeFacture == "consultation" && 
                              f.Statut == "payee");

            if (!facturePaye)
                return BadRequest(new { success = false, message = "La facture de consultation n'est pas payée" });

            // Marquer le patient comme arrivé
            rdv.Statut = "arrive";
            rdv.DateModification = DateTime.UtcNow;

            // Mettre à jour le statut de la consultation
            consultation.Statut = "arrive";
            consultation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var patientNom = rdv.Patient?.Utilisateur != null 
                ? $"{rdv.Patient.Utilisateur.Prenom} {rdv.Patient.Utilisateur.Nom}" 
                : "Patient";

            _logger.LogInformation("Patient {PatientNom} marqué comme arrivé pour RDV {IdRendezVous}", patientNom, idRendezVous);

            return Ok(new { 
                success = true, 
                message = $"Patient {patientNom} marqué comme arrivé",
                idConsultation = consultation.IdConsultation,
                idRendezVous = rdv.IdRendezVous
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur MarquerArriveePatient");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Obtenir les RDV confirmés du jour en attente d'arrivée (pour les patients RDV en ligne)
    /// </summary>
    [HttpGet("rdv-confirmes-jour")]
    public async Task<IActionResult> GetRdvConfirmesJour()
    {
        try
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // RDV confirmés (payés) du jour qui ne sont pas encore arrivés
            var rdvs = await _context.RendezVous
                .Include(r => r.Patient)
                    .ThenInclude(p => p!.Utilisateur)
                .Include(r => r.Medecin)
                    .ThenInclude(m => m!.Utilisateur)
                .Where(r => r.DateHeure >= today && r.DateHeure < tomorrow)
                .Where(r => r.Statut == "confirme") // Confirmé mais pas encore arrivé
                .OrderBy(r => r.DateHeure)
                .Select(r => new
                {
                    idRendezVous = r.IdRendezVous,
                    idPatient = r.IdPatient,
                    patientNom = r.Patient != null && r.Patient.Utilisateur != null ? r.Patient.Utilisateur.Nom : "",
                    patientPrenom = r.Patient != null && r.Patient.Utilisateur != null ? r.Patient.Utilisateur.Prenom : "",
                    numeroDossier = r.Patient != null ? r.Patient.NumeroDossier : null,
                    idMedecin = r.IdMedecin,
                    medecinNom = r.Medecin != null && r.Medecin.Utilisateur != null ? r.Medecin.Utilisateur.Nom : "",
                    medecinPrenom = r.Medecin != null && r.Medecin.Utilisateur != null ? r.Medecin.Utilisateur.Prenom : "",
                    dateHeure = r.DateHeure,
                    motif = r.Motif,
                    statut = r.Statut
                })
                .ToListAsync();

            return Ok(new { success = true, data = rdvs, count = rdvs.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur GetRdvConfirmesJour");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    // ==================== GESTION HOSPITALISATIONS (MAJOR) ====================

    /// <summary>
    /// Récupérer les hospitalisations en attente d'attribution de lit
    /// Accessible uniquement par les Majors - filtre par leur service
    /// Le Major est identifié via Service.IdMajor
    /// </summary>
    [HttpGet("hospitalisations/en-attente")]
    public async Task<IActionResult> GetHospitalisationsEnAttente()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Utilisateur non authentifié" });

            // Vérifier si l'utilisateur est infirmier
            var infirmier = await _context.Infirmiers
                .Include(i => i.Service)
                .FirstOrDefaultAsync(i => i.IdUser == userId.Value);

            if (infirmier == null)
                return NotFound(new { success = false, message = "Infirmier non trouvé" });

            // Vérifier si l'infirmier est Major d'un service (via Service.IdMajor)
            var serviceMajor = await _context.Services
                .FirstOrDefaultAsync(s => s.IdMajor == userId.Value);

            if (serviceMajor == null)
            {
                // Non-Major ne peut pas voir les hospitalisations en attente
                _logger.LogInformation("Infirmier {UserId} n'est pas Major d'un service", userId.Value);
                return Ok(new { success = true, data = new List<object>(), count = 0, isMajor = false });
            }

            _logger.LogInformation("Major {UserId} du service {ServiceId} ({ServiceNom}) - Chargement hospitalisations EN_ATTENTE", 
                userId.Value, serviceMajor.IdService, serviceMajor.NomService);

            var hospitalisations = await _hospitalisationService.GetHospitalisationsEnAttenteAsync(serviceMajor.IdService);

            _logger.LogInformation("Trouvé {Count} hospitalisations EN_ATTENTE pour le service {ServiceId}", 
                hospitalisations.Count, serviceMajor.IdService);

            return Ok(new { 
                success = true, 
                data = hospitalisations, 
                count = hospitalisations.Count,
                isMajor = true,
                idService = serviceMajor.IdService,
                serviceNom = serviceMajor.NomService
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur GetHospitalisationsEnAttente");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Attribuer un lit à une hospitalisation en attente (Major uniquement)
    /// </summary>
    [HttpPost("hospitalisations/attribuer-lit")]
    public async Task<IActionResult> AttribuerLit([FromBody] AttribuerLitRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Utilisateur non authentifié" });

            var role = GetCurrentUserRole() ?? "infirmier";
            var result = await _hospitalisationService.AttribuerLitAsync(request, userId.Value, role);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            _logger.LogInformation("Lit attribué: Hospitalisation {IdAdmission}, Lit {IdLit}, Major {MajorId}", 
                request.IdAdmission, request.IdLit, userId.Value);

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = result.Data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur AttribuerLit");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer les hospitalisations en cours du service du Major
    /// Le Major est identifié via Service.IdMajor
    /// </summary>
    [HttpGet("hospitalisations/service")]
    public async Task<IActionResult> GetHospitalisationsService([FromQuery] string? statut = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Utilisateur non authentifié" });

            // Vérifier si l'utilisateur est infirmier
            var infirmier = await _context.Infirmiers
                .FirstOrDefaultAsync(i => i.IdUser == userId.Value);

            if (infirmier == null)
                return NotFound(new { success = false, message = "Infirmier non trouvé" });

            // Vérifier si l'infirmier est Major d'un service (via Service.IdMajor)
            var serviceMajor = await _context.Services
                .FirstOrDefaultAsync(s => s.IdMajor == userId.Value);

            if (serviceMajor == null)
            {
                return Ok(new { success = false, message = "Vous devez être Major pour accéder à cette fonctionnalité", data = new List<object>() });
            }

            var filtre = new DTOs.Hospitalisation.FiltreHospitalisationRequest
            {
                IdService = serviceMajor.IdService,
                Statut = statut // Permet de filtrer par statut (EN_COURS, EN_ATTENTE, etc.)
            };

            var hospitalisations = await _hospitalisationService.GetHospitalisationsAsync(filtre);

            return Ok(new { 
                success = true, 
                data = hospitalisations, 
                count = hospitalisations.Count,
                idService = serviceMajor.IdService
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur GetHospitalisationsService");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer tous les patients (pour la page Patients infirmier)
    /// </summary>
    [HttpGet("patients")]
    public async Task<IActionResult> GetAllPatients([FromQuery] string? search = null)
    {
        try
        {
            var query = _context.Patients
                .Include(p => p.Utilisateur)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(p => 
                    (p.Utilisateur != null && p.Utilisateur.Nom != null && p.Utilisateur.Nom.ToLower().Contains(search)) ||
                    (p.Utilisateur != null && p.Utilisateur.Prenom != null && p.Utilisateur.Prenom.ToLower().Contains(search)) ||
                    (p.NumeroDossier != null && p.NumeroDossier.ToLower().Contains(search)));
            }

            var patients = await query
                .OrderBy(p => p.Utilisateur != null ? p.Utilisateur.Nom : "")
                .Take(100)
                .Select(p => new
                {
                    idPatient = p.IdUser,
                    nom = p.Utilisateur != null ? p.Utilisateur.Nom : "",
                    prenom = p.Utilisateur != null ? p.Utilisateur.Prenom : "",
                    email = p.Utilisateur != null ? p.Utilisateur.Email : "",
                    telephone = p.Utilisateur != null ? p.Utilisateur.Telephone : "",
                    numeroDossier = p.NumeroDossier,
                    dateNaissance = p.Utilisateur != null ? p.Utilisateur.Naissance : null,
                    sexe = p.Utilisateur != null ? p.Utilisateur.Sexe : null,
                    groupeSanguin = p.GroupeSanguin
                })
                .ToListAsync();

            return Ok(new { success = true, data = patients, count = patients.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur GetAllPatients");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer les patients hospitalisés
    /// - Major: voit EN_ATTENTE + EN_COURS de son service
    /// - Infirmier standard: voit EN_COURS de son service de rattachement
    /// Le Major est identifié via Service.IdMajor
    /// </summary>
    [HttpGet("patients/hospitalises")]
    public async Task<IActionResult> GetPatientsHospitalises([FromQuery] string? search = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Utilisateur non authentifié" });

            // Récupérer l'infirmier avec son service de rattachement
            var infirmier = await _context.Infirmiers
                .Include(i => i.Service)
                .FirstOrDefaultAsync(i => i.IdUser == userId.Value);

            if (infirmier == null)
                return NotFound(new { message = "Infirmier non trouvé" });

            // Vérifier si l'infirmier est Major d'un service (via Service.IdMajor)
            var serviceMajor = await _context.Services
                .FirstOrDefaultAsync(s => s.IdMajor == userId.Value);

            bool isMajor = serviceMajor != null;
            int serviceIdToFilter = isMajor ? serviceMajor!.IdService : infirmier.IdService;

            _logger.LogInformation("GetPatientsHospitalises - UserId: {UserId}, IsMajor: {IsMajor}, ServiceId: {ServiceId}", 
                userId.Value, isMajor, serviceIdToFilter);

            // Définir les statuts à filtrer selon le rôle
            var statutsToFilter = isMajor 
                ? new[] { "EN_ATTENTE", "EN_COURS" } 
                : new[] { "EN_COURS" };

            _logger.LogInformation("Filtrage pour service {ServiceId} - statuts: {Statuts}", 
                serviceIdToFilter, string.Join(", ", statutsToFilter));

            // Charger les hospitalisations de base sans navigation
            var rawHospitalisations = await _context.Hospitalisations
                .Where(h => h.IdService == serviceIdToFilter && statutsToFilter.Contains(h.Statut!))
                .OrderByDescending(h => h.Urgence == "critique")
                .ThenByDescending(h => h.Urgence == "urgente")
                .ThenByDescending(h => h.Statut == "EN_ATTENTE")
                .ThenBy(h => h.DateEntree)
                .ToListAsync();

            _logger.LogInformation("Chargé {Count} hospitalisations brutes", rawHospitalisations.Count);

            // Charger les patients séparément
            var patientIds = rawHospitalisations.Select(h => h.IdPatient).Distinct().ToList();
            var patients = await _context.Patients
                .Include(p => p.Utilisateur)
                .Where(p => patientIds.Contains(p.IdUser))
                .ToDictionaryAsync(p => p.IdUser);

            // Charger les services séparément
            var serviceIds = rawHospitalisations.Where(h => h.IdService.HasValue).Select(h => h.IdService!.Value).Distinct().ToList();
            var services = await _context.Services
                .Where(s => serviceIds.Contains(s.IdService))
                .ToDictionaryAsync(s => s.IdService);

            // Charger les médecins séparément
            var medecinIds = rawHospitalisations.Where(h => h.IdMedecin.HasValue).Select(h => h.IdMedecin!.Value).Distinct().ToList();
            var medecins = await _context.Medecins
                .Include(m => m.Utilisateur)
                .Where(m => medecinIds.Contains(m.IdUser))
                .ToDictionaryAsync(m => m.IdUser);

            // Construire la réponse manuellement
            var hospitalisations = rawHospitalisations.Select(h => {
                var patient = patients.GetValueOrDefault(h.IdPatient);
                var service = h.IdService.HasValue ? services.GetValueOrDefault(h.IdService.Value) : null;
                var medecin = h.IdMedecin.HasValue ? medecins.GetValueOrDefault(h.IdMedecin.Value) : null;
                return new
                {
                    idAdmission = h.IdAdmission,
                    statut = h.Statut ?? "EN_ATTENTE",
                    urgence = h.Urgence ?? "normale",
                    dateEntree = h.DateEntree,
                    dateSortiePrevue = h.DateSortie,
                    motif = h.Motif ?? "",
                    diagnosticPrincipal = h.DiagnosticPrincipal ?? "",
                    patient = new
                    {
                        idPatient = h.IdPatient,
                        nom = patient?.Utilisateur?.Nom ?? "",
                        prenom = patient?.Utilisateur?.Prenom ?? "",
                        numeroDossier = patient?.NumeroDossier ?? ""
                    },
                    medecin = new
                    {
                        idMedecin = h.IdMedecin,
                        nom = medecin?.Utilisateur?.Nom ?? "",
                        prenom = medecin?.Utilisateur?.Prenom ?? ""
                    },
                    idLit = h.IdLit,
                    service = service?.NomService ?? ""
                };
            }).ToList();

            // Appliquer le filtre de recherche si présent
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower().Trim();
                hospitalisations = hospitalisations.Where(h =>
                    h.patient.nom.ToLower().Contains(searchLower) ||
                    h.patient.prenom.ToLower().Contains(searchLower) ||
                    h.patient.numeroDossier.ToLower().Contains(searchLower)
                ).ToList();
            }

            _logger.LogInformation("GetPatientsHospitalises - Trouvé {Count} hospitalisations (après filtre)", hospitalisations.Count);

            return Ok(new { 
                success = true, 
                data = hospitalisations, 
                count = hospitalisations.Count,
                isMajor = isMajor,
                enAttente = hospitalisations.Count(h => h.statut == "EN_ATTENTE"),
                enCours = hospitalisations.Count(h => h.statut == "EN_COURS")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur GetPatientsHospitalises");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer les lits disponibles pour attribution (Major)
    /// </summary>
    [HttpGet("lits/disponibles")]
    public async Task<IActionResult> GetLitsDisponibles()
    {
        try
        {
            var result = await _hospitalisationService.GetLitsDisponiblesAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur GetLitsDisponibles");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer les chambres avec leurs lits (Major)
    /// </summary>
    [HttpGet("chambres")]
    public async Task<IActionResult> GetChambres()
    {
        try
        {
            var result = await _hospitalisationService.GetChambresAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur GetChambres");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer les standards de chambre actifs pour sélection (Major)
    /// </summary>
    [HttpGet("hospitalisation/standards")]
    public async Task<IActionResult> GetStandardsForHospitalisation()
    {
        try
        {
            var standards = await _context.StandardsChambres
                .Where(s => s.Actif)
                .Select(s => new
                {
                    idStandard = s.IdStandard,
                    nom = s.Nom,
                    description = s.Description,
                    prixJournalier = s.PrixJournalier,
                    localisation = s.Localisation,
                    chambresDisponibles = s.Chambres!
                        .Where(c => c.Statut == "actif" && c.Lits!.Any(l => l.Statut == "libre"))
                        .Count()
                })
                .OrderBy(s => s.nom)
                .ToListAsync();

            return Ok(new { success = true, data = standards });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting standards");
            return StatusCode(500, new { message = "Erreur lors de la récupération des standards" });
        }
    }

    /// <summary>
    /// Récupérer les chambres disponibles par standard (Major)
    /// </summary>
    [HttpGet("hospitalisation/chambres/{idStandard}")]
    public async Task<IActionResult> GetChambresDisponiblesByStandard(int idStandard)
    {
        try
        {
            var chambres = await _context.Chambres
                .Include(c => c.Lits)
                .Include(c => c.Standard)
                .Where(c => c.IdStandard == idStandard && c.Statut == "actif")
                .Where(c => c.Lits != null && c.Lits.Any(l => l.Statut == "libre"))
                .OrderBy(c => c.Numero)
                .Select(c => new
                {
                    idChambre = c.IdChambre,
                    numero = c.Numero,
                    standardNom = c.Standard != null ? c.Standard.Nom : "",
                    prixJournalier = c.Standard != null ? c.Standard.PrixJournalier : 0,
                    localisation = c.Standard != null ? c.Standard.Localisation : null,
                    litsDisponibles = c.Lits!
                        .Where(l => l.Statut == "libre")
                        .Select(l => new { idLit = l.IdLit, numero = l.Numero })
                        .ToList()
                })
                .ToListAsync();

            return Ok(new { success = true, data = chambres });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chambres by standard");
            return StatusCode(500, new { message = "Erreur lors de la récupération des chambres" });
        }
    }

    /// <summary>
    /// Récupérer les détails complets d'une hospitalisation (Major)
    /// </summary>
    [HttpGet("hospitalisation/{idAdmission}/details")]
    public async Task<IActionResult> GetHospitalisationDetails(int idAdmission)
    {
        try
        {
            var hospitalisation = await _context.Hospitalisations
                .Include(h => h.Patient)
                    .ThenInclude(p => p!.Utilisateur)
                .Include(h => h.Medecin)
                    .ThenInclude(m => m!.Utilisateur)
                .Include(h => h.Lit)
                    .ThenInclude(l => l!.Chambre)
                        .ThenInclude(c => c!.Standard)
                .Include(h => h.Service)
                .FirstOrDefaultAsync(h => h.IdAdmission == idAdmission);

            if (hospitalisation == null)
            {
                return NotFound(new { success = false, message = "Hospitalisation non trouvée" });
            }

            Utilisateur? utilisateurAttribuant = null;
            if (hospitalisation.IdLitAttribuePar.HasValue)
            {
                utilisateurAttribuant = await _context.Utilisateurs
                    .FirstOrDefaultAsync(u => u.IdUser == hospitalisation.IdLitAttribuePar.Value);
            }

            // Récupérer les soins liés à cette hospitalisation
            var soins = await _context.SoinsHospitalisation
                .Include(s => s.Prescripteur)
                    .ThenInclude(m => m!.Utilisateur)
                .Where(s => s.IdHospitalisation == idAdmission)
                .OrderByDescending(s => s.DatePrescription)
                .Select(s => new
                {
                    idSoin = s.IdSoin,
                    typeSoin = s.TypeSoin ?? "",
                    description = s.Description ?? "",
                    dateHeure = s.DatePrescription,
                    statut = s.Statut ?? "planifie",
                    infirmier = s.Prescripteur != null && s.Prescripteur.Utilisateur != null 
                        ? $"Dr {s.Prescripteur.Utilisateur.Prenom} {s.Prescripteur.Utilisateur.Nom}" 
                        : null
                })
                .ToListAsync();

            // Récupérer les examens prescrits (BulletinExamen liés à l'hospitalisation)
            var examens = await _context.BulletinsExamen
                .Include(b => b.Examen)
                    .ThenInclude(e => e!.Specialite)
                .Include(b => b.Laboratoire)
                .Where(b => b.IdHospitalisation == idAdmission)
                .OrderByDescending(b => b.DateDemande)
                .Select(b => new
                {
                    idExamen = b.IdBulletinExamen,
                    idBulletinExamen = b.IdBulletinExamen,
                    typeExamen = b.Examen != null ? (b.Examen.Specialite != null ? b.Examen.Specialite.Nom : "Examen") : "Examen",
                    description = b.Examen != null ? b.Examen.NomExamen : "Examen prescrit",
                    datePrescription = b.DateDemande,
                    statut = b.Statut ?? "prescrit",
                    urgence = b.Urgence,
                    laboratoire = b.Laboratoire != null ? b.Laboratoire.NomLabo : null,
                    resultat = b.ResultatTexte,
                    dateResultat = b.DateResultat,
                    hasResultat = b.DateResultat != null || !string.IsNullOrEmpty(b.ResultatTexte)
                })
                .ToListAsync();

            // Récupérer les prescriptions médicamenteuses
            // TODO: Implémenter la liaison correcte avec les ordonnances
            var prescriptions = new List<object>();

            var result = new
            {
                idAdmission = hospitalisation.IdAdmission,
                statut = hospitalisation.Statut ?? "EN_ATTENTE",
                urgence = hospitalisation.Urgence ?? "normale",
                dateEntree = hospitalisation.DateEntree,
                dateSortiePrevue = hospitalisation.DateSortie,
                dateSortie = hospitalisation.DateSortie,
                motif = hospitalisation.Motif ?? "",
                diagnosticPrincipal = hospitalisation.DiagnosticPrincipal,
                patient = new
                {
                    idPatient = hospitalisation.IdPatient,
                    nom = hospitalisation.Patient?.Utilisateur?.Nom ?? "",
                    prenom = hospitalisation.Patient?.Utilisateur?.Prenom ?? "",
                    numeroDossier = hospitalisation.Patient?.NumeroDossier,
                    dateNaissance = hospitalisation.Patient?.Utilisateur?.Naissance,
                    sexe = hospitalisation.Patient?.Utilisateur?.Sexe,
                    telephone = hospitalisation.Patient?.Utilisateur?.Telephone
                },
                medecin = hospitalisation.Medecin != null ? new
                {
                    idMedecin = hospitalisation.IdMedecin,
                    nom = hospitalisation.Medecin.Utilisateur?.Nom ?? "",
                    prenom = hospitalisation.Medecin.Utilisateur?.Prenom ?? ""
                } : null,
                service = hospitalisation.Service?.NomService,
                lit = hospitalisation.Lit != null ? new
                {
                    idLit = hospitalisation.IdLit,
                    numero = hospitalisation.Lit.Numero,
                    chambre = hospitalisation.Lit.Chambre?.Numero ?? "",
                    standard = hospitalisation.Lit.Chambre?.Standard?.Nom
                } : null,
                litAttribuePar = hospitalisation.IdLitAttribuePar.HasValue ? new
                {
                    idUser = hospitalisation.IdLitAttribuePar,
                    nom = utilisateurAttribuant?.Nom,
                    prenom = utilisateurAttribuant?.Prenom,
                    role = hospitalisation.RoleLitAttribuePar,
                    date = hospitalisation.DateLitAttribue
                } : null,
                soins = soins,
                examens = examens,
                prescriptions = prescriptions
            };

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hospitalisation details");
            return StatusCode(500, new { message = "Erreur lors de la récupération des détails" });
        }
    }

    // ==================== GESTION DES SOINS ====================

    /// <summary>
    /// Récupérer les soins à exécuter pour la journée (pour l'infirmier)
    /// </summary>
    [HttpGet("soins/jour")]
    public async Task<IActionResult> GetSoinsJour([FromQuery] DateTime? date = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Utilisateur non authentifié" });

            var targetDate = date?.Date ?? DateTime.UtcNow.Date;

            var executions = await _context.ExecutionsSoins
                .Include(e => e.Soin).ThenInclude(s => s!.Hospitalisation).ThenInclude(h => h!.Patient).ThenInclude(p => p!.Utilisateur)
                .Include(e => e.Soin).ThenInclude(s => s!.Hospitalisation).ThenInclude(h => h!.Lit).ThenInclude(l => l!.Chambre)
                .Include(e => e.Executant)
                .Where(e => e.DatePrevue.HasValue && e.DatePrevue.Value.Date == targetDate)
                .Where(e => e.Soin!.Hospitalisation!.Statut == "EN_COURS")
                .OrderBy(e => GetOrdreForMoment(e.Moment ?? "autre"))
                .ThenBy(e => e.Soin!.Priorite == "urgente" ? 0 : e.Soin.Priorite == "haute" ? 1 : 2)
                .Select(e => new
                {
                    idExecution = e.IdExecution,
                    idSoin = e.IdSoin,
                    moment = e.Moment,
                    heurePrevue = e.HeurePrevue,
                    statut = e.Statut,
                    dateExecution = e.DateExecution,
                    executant = e.Executant != null ? $"{e.Executant.Prenom} {e.Executant.Nom}" : null,
                    observations = e.Observations,
                    soin = new
                    {
                        typeSoin = e.Soin!.TypeSoin,
                        description = e.Soin.Description,
                        priorite = e.Soin.Priorite,
                        instructions = e.Soin.Instructions
                    },
                    patient = new
                    {
                        idPatient = e.Soin.Hospitalisation!.IdPatient,
                        nom = e.Soin.Hospitalisation.Patient!.Utilisateur!.Nom,
                        prenom = e.Soin.Hospitalisation.Patient.Utilisateur.Prenom
                    },
                    chambre = e.Soin.Hospitalisation.Lit != null 
                        ? $"{e.Soin.Hospitalisation.Lit.Chambre!.Numero} - Lit {e.Soin.Hospitalisation.Lit.Numero}"
                        : "Non assigné"
                })
                .ToListAsync();

            // Grouper par moment
            var parMoment = executions
                .GroupBy(e => e.moment)
                .OrderBy(g => GetOrdreForMoment(g.Key ?? "autre"))
                .Select(g => new
                {
                    moment = g.Key,
                    total = g.Count(),
                    faits = g.Count(e => e.statut == "fait"),
                    prevus = g.Count(e => e.statut == "prevu"),
                    executions = g.ToList()
                })
                .ToList();

            return Ok(new { 
                success = true, 
                date = targetDate,
                totalExecutions = executions.Count,
                totalFaits = executions.Count(e => e.statut == "fait"),
                totalPrevus = executions.Count(e => e.statut == "prevu"),
                data = parMoment 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting soins jour");
            return StatusCode(500, new { message = "Erreur lors de la récupération des soins" });
        }
    }

    /// <summary>
    /// Marquer une exécution de soin comme faite
    /// </summary>
    [HttpPost("soins/executions/{idExecution}/marquer-fait")]
    public async Task<IActionResult> MarquerExecutionFaite(int idExecution, [FromBody] MarquerExecutionRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Utilisateur non authentifié" });

            var execution = await _context.ExecutionsSoins
                .Include(e => e.Soin)
                .FirstOrDefaultAsync(e => e.IdExecution == idExecution);

            if (execution == null)
                return NotFound(new { success = false, message = "Exécution non trouvée" });

            if (execution.Statut == "fait")
                return BadRequest(new { success = false, message = "Cette exécution a déjà été marquée comme faite" });

            // Mettre à jour l'exécution
            execution.Statut = "fait";
            execution.DateExecution = DateTime.UtcNow;
            execution.IdExecutant = userId.Value;
            execution.Observations = request.Observations;
            execution.UpdatedAt = DateTime.UtcNow;

            // Mettre à jour le compteur du soin parent
            if (execution.Soin != null)
            {
                execution.Soin.NbExecutionsEffectuees = await _context.ExecutionsSoins
                    .CountAsync(e => e.IdSoin == execution.IdSoin && e.Statut == "fait") + 1;

                // Vérifier si toutes les exécutions sont terminées
                var totalPrevues = execution.Soin.NbExecutionsPrevues;
                if (execution.Soin.NbExecutionsEffectuees >= totalPrevues)
                {
                    execution.Soin.Statut = "termine";
                }
                else if (execution.Soin.Statut == "prescrit")
                {
                    execution.Soin.Statut = "en_cours";
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Exécution {IdExecution} marquée comme faite par infirmier {UserId}", idExecution, userId.Value);

            return Ok(new { 
                success = true, 
                message = "Soin marqué comme effectué",
                dateExecution = execution.DateExecution
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking execution as done");
            return StatusCode(500, new { message = "Erreur lors de la mise à jour" });
        }
    }

    /// <summary>
    /// Tableau de bord infirmier : liste des soins à faire maintenant ou dans l'heure
    /// Affiche tous les soins prévus pour les hospitalisations en cours
    /// </summary>
    [HttpGet("soins-a-faire")]
    public async Task<IActionResult> GetSoinsAFaire()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Utilisateur non authentifié" });

            var maintenant = DateTime.UtcNow;
            var heureActuelle = maintenant.TimeOfDay;
            var dansUneHeure = heureActuelle.Add(TimeSpan.FromHours(1));

            // Récupérer les exécutions prévues pour aujourd'hui (non faites)
            var soinsAFaire = await _context.ExecutionsSoins
                .Include(e => e.Soin)
                    .ThenInclude(s => s!.Hospitalisation)
                        .ThenInclude(h => h!.Patient)
                            .ThenInclude(p => p!.Utilisateur)
                .Include(e => e.Soin)
                    .ThenInclude(s => s!.Hospitalisation)
                        .ThenInclude(h => h!.Lit)
                .Where(e => e.Statut == "prevu")
                .Where(e => e.DatePrevue.HasValue && e.DatePrevue.Value.Date == maintenant.Date)
                .Where(e => e.Soin!.Hospitalisation!.Statut == "en_cours")
                .OrderBy(e => e.HeurePrevue)
                .ThenBy(e => e.Soin!.Priorite == "urgente" ? 0 : e.Soin!.Priorite == "haute" ? 1 : 2)
                .ToListAsync();

            var result = soinsAFaire.Select(e => {
                var heurePrevue = e.HeurePrevue ?? TimeSpan.Zero;
                var estEnRetard = heurePrevue < heureActuelle;
                var estDansLHeure = heurePrevue <= dansUneHeure && heurePrevue >= heureActuelle;
                var estUrgent = e.Soin?.Priorite?.ToLower() == "urgente" || e.Soin?.Priorite?.ToLower() == "haute";

                return new {
                    idExecution = e.IdExecution,
                    idSoin = e.IdSoin,
                    numeroSeance = e.NumeroSeance,
                    heurePrevue = heurePrevue.ToString(@"hh\:mm"),
                    estEnRetard,
                    estDansLHeure,
                    estUrgent,
                    soin = new {
                        typeSoin = e.Soin?.TypeSoin,
                        description = e.Soin?.Description,
                        priorite = e.Soin?.Priorite,
                        instructions = e.Soin?.Instructions
                    },
                    patient = new {
                        idPatient = e.Soin?.Hospitalisation?.IdPatient,
                        nom = e.Soin?.Hospitalisation?.Patient?.Utilisateur?.Nom,
                        prenom = e.Soin?.Hospitalisation?.Patient?.Utilisateur?.Prenom,
                        chambre = e.Soin?.Hospitalisation?.Lit?.Numero
                    },
                    hospitalisation = new {
                        idAdmission = e.Soin?.IdHospitalisation
                    }
                };
            }).ToList();

            // Grouper par statut
            var enRetard = result.Where(r => r.estEnRetard).ToList();
            var dansLHeure = result.Where(r => r.estDansLHeure && !r.estEnRetard).ToList();
            var plusTard = result.Where(r => !r.estEnRetard && !r.estDansLHeure).ToList();

            return Ok(new {
                success = true,
                dateHeure = maintenant,
                resume = new {
                    total = result.Count,
                    enRetard = enRetard.Count(),
                    dansLHeure = dansLHeure.Count(),
                    plusTard = plusTard.Count(),
                    urgents = result.Count(r => r.estUrgent)
                },
                soinsEnRetard = enRetard,
                soinsDansLHeure = dansLHeure,
                soinsPlusTard = plusTard
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting soins à faire");
            return StatusCode(500, new { message = "Erreur lors de la récupération des soins" });
        }
    }

    /// <summary>
    /// Enregistrer automatiquement l'exécution d'un soin
    /// Trouve l'exécution prévue la plus proche dans le temps et la marque comme faite
    /// Supporte la sélection manuelle par IdExecution ou NumeroSeance
    /// </summary>
    [HttpPost("soins/{idSoin}/executer")]
    public async Task<IActionResult> EnregistrerExecutionSoin(int idSoin, [FromBody] EnregistrerExecutionRequest? request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Utilisateur non authentifié" });

            // Vérifier que le soin existe
            var soin = await _context.SoinsHospitalisation
                .Include(s => s.Hospitalisation)
                .FirstOrDefaultAsync(s => s.IdSoin == idSoin);

            if (soin == null)
                return NotFound(new { success = false, message = "Soin non trouvé" });

            // Récupérer la date et l'heure actuelles
            var maintenant = DateTime.UtcNow;
            var heureActuelle = maintenant.TimeOfDay;

            ExecutionSoin? executionCible = null;

            // 1. Sélection manuelle par IdExecution
            if (request?.IdExecution.HasValue == true)
            {
                executionCible = await _context.ExecutionsSoins
                    .FirstOrDefaultAsync(e => e.IdExecution == request.IdExecution.Value && e.IdSoin == idSoin);
                
                if (executionCible == null)
                    return NotFound(new { success = false, message = "Exécution spécifiée non trouvée" });
            }
            // 2. Sélection manuelle par NumeroSeance (pour aujourd'hui)
            else if (request?.NumeroSeance.HasValue == true)
            {
                executionCible = await _context.ExecutionsSoins
                    .Where(e => e.IdSoin == idSoin && e.Statut == "prevu")
                    .Where(e => e.NumeroSeance == request.NumeroSeance.Value)
                    .Where(e => e.DatePrevue.HasValue && e.DatePrevue.Value.Date == maintenant.Date)
                    .FirstOrDefaultAsync();
                
                if (executionCible == null)
                    return NotFound(new { success = false, message = $"Aucune exécution prévue pour la séance {request.NumeroSeance} aujourd'hui" });
            }
            // 3. Sélection automatique (comportement par défaut)
            else
            {
                var executionsPrevues = await _context.ExecutionsSoins
                    .Where(e => e.IdSoin == idSoin && e.Statut == "prevu")
                    .OrderBy(e => e.DatePrevue)
                    .ThenBy(e => e.HeurePrevue)
                    .ToListAsync();

                if (!executionsPrevues.Any())
                    return BadRequest(new { success = false, message = "Aucune exécution prévue à enregistrer pour ce soin" });

                // D'abord chercher une exécution pour aujourd'hui
                var executionsAujourdhui = executionsPrevues
                    .Where(e => e.DatePrevue.HasValue && e.DatePrevue.Value.Date == maintenant.Date)
                    .ToList();

                if (executionsAujourdhui.Any())
                {
                    // Trouver le moment le plus proche de l'heure actuelle
                    executionCible = executionsAujourdhui
                        .OrderBy(e => Math.Abs((e.HeurePrevue ?? TimeSpan.Zero).TotalMinutes - heureActuelle.TotalMinutes))
                        .FirstOrDefault();
                }
                else
                {
                    // Sinon prendre la première exécution prévue (la plus ancienne non faite)
                    executionCible = executionsPrevues.FirstOrDefault();
                }
            }

            if (executionCible == null)
                return BadRequest(new { success = false, message = "Impossible de trouver une exécution à enregistrer" });

            // Variable non-nullable après vérification
            var execution = executionCible;

            // Vérifier que l'exécution n'est pas déjà faite
            if (execution.Statut == "fait")
                return BadRequest(new { success = false, message = "Cette exécution a déjà été enregistrée" });
            
            // Validation des horaires : warning si heure très différente (>2h)
            string? warningHoraire = null;
            if (execution.HeurePrevue.HasValue)
            {
                var diffMinutes = Math.Abs((execution.HeurePrevue.Value - heureActuelle).TotalMinutes);
                if (diffMinutes > 120)
                {
                    warningHoraire = $"Attention: Cette exécution était prévue à {execution.HeurePrevue.Value:hh\\:mm}, " +
                                    $"il est actuellement {heureActuelle:hh\\:mm} (écart de {(int)(diffMinutes/60)}h{(int)(diffMinutes%60):00})";
                }
            }

            // Marquer l'exécution comme faite
            execution.Statut = "fait";
            execution.DateExecution = maintenant;
            execution.HeureExecution = maintenant.TimeOfDay;
            execution.IdExecutant = userId.Value;
            execution.Observations = request?.Observations;
            execution.UpdatedAt = maintenant;

            // Mettre à jour le compteur du soin parent
            soin.NbExecutionsEffectuees = await _context.ExecutionsSoins
                .CountAsync(e => e.IdSoin == idSoin && e.Statut == "fait") + 1;

            // Vérifier si toutes les exécutions sont terminées
            if (soin.NbExecutionsEffectuees >= soin.NbExecutionsPrevues)
            {
                soin.Statut = "termine";
            }
            else if (soin.Statut == "prescrit")
            {
                soin.Statut = "en_cours";
            }

            await _context.SaveChangesAsync();

            // Récupérer le nom de l'infirmier
            var infirmier = await _context.Utilisateurs.FindAsync(userId.Value);
            var nomInfirmier = infirmier != null ? $"{infirmier.Prenom} {infirmier.Nom}" : "Inconnu";

            // ==================== NOTIFICATIONS SOIN EFFECTUÉ ====================
            try
            {
                // Récupérer les IDs des acteurs concernés
                var destinataires = new List<int>();
                
                // Infirmier exécutant (confirmation)
                destinataires.Add(userId.Value);
                
                // Médecin prescripteur
                if (soin.IdPrescripteur.HasValue)
                    destinataires.Add(soin.IdPrescripteur.Value);
                
                // Patient
                var patient = await _context.Hospitalisations
                    .Where(h => h.IdAdmission == soin.IdHospitalisation)
                    .Select(h => h.Patient!.IdUser)
                    .FirstOrDefaultAsync();
                if (patient > 0) destinataires.Add(patient);
                
                // Major du service (chercher par rôle)
                var majors = await _context.Utilisateurs
                    .Where(u => u.Role == "major")
                    .Select(u => u.IdUser)
                    .ToListAsync();
                destinataires.AddRange(majors);

                // Envoyer les notifications
                if (destinataires.Any())
                {
                    await _notificationService.CreateBulkAsync(new CreateBulkNotificationRequest
                    {
                        UserIds = destinataires.Distinct().ToList(),
                        Type = "soin_effectue",
                        Titre = "Soin effectué",
                        Message = $"{soin.TypeSoin} - {soin.Description} effectué par {nomInfirmier}",
                        Lien = $"/hospitalisations/{soin.IdHospitalisation}/soins/{soin.IdSoin}",
                        Icone = "check-circle",
                        Priorite = "normale",
                        SendRealTime = true
                    });
                }
            }
            catch (Exception notifEx)
            {
                _logger.LogWarning("Erreur envoi notifications soin effectué: {Error}", notifEx.Message);
            }

            _logger.LogInformation(
                "Soin {IdSoin} exécuté: Exécution {IdExecution} ({Moment}) marquée faite à {Heure} par {Infirmier}", 
                idSoin, execution.IdExecution, execution.Moment, maintenant.ToString("HH:mm"), nomInfirmier);

            return Ok(new { 
                success = true, 
                message = $"Soin enregistré avec succès",
                warning = warningHoraire,
                data = new {
                    idExecution = execution.IdExecution,
                    numeroSeance = execution.NumeroSeance,
                    datePrevue = execution.DatePrevue,
                    heurePrevue = execution.HeurePrevue?.ToString(@"hh\:mm"),
                    heureExecution = maintenant.ToString("HH:mm"),
                    executant = nomInfirmier,
                    nbExecutionsRestantes = soin.NbExecutionsPrevues - soin.NbExecutionsEffectuees
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering soin execution");
            return StatusCode(500, new { message = "Erreur lors de l'enregistrement du soin" });
        }
    }

    /// <summary>
    /// Marquer une exécution de soin comme manquée
    /// </summary>
    [HttpPost("soins/executions/{idExecution}/marquer-manque")]
    public async Task<IActionResult> MarquerExecutionManquee(int idExecution, [FromBody] MarquerExecutionRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Utilisateur non authentifié" });

            var execution = await _context.ExecutionsSoins
                .FirstOrDefaultAsync(e => e.IdExecution == idExecution);

            if (execution == null)
                return NotFound(new { success = false, message = "Exécution non trouvée" });

            execution.Statut = "manque";
            execution.Observations = request.Observations;
            execution.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Exécution {IdExecution} marquée comme manquée", idExecution);

            return Ok(new { success = true, message = "Soin marqué comme manqué" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking execution as missed");
            return StatusCode(500, new { message = "Erreur lors de la mise à jour" });
        }
    }

    /// <summary>
    /// Obtenir l'ordre d'affichage pour un moment de la journée
    /// </summary>
    private static int GetOrdreForMoment(string moment)
    {
        return moment.ToLower() switch
        {
            "matin" => 1,
            "midi" => 2,
            "soir" => 3,
            "nuit" => 4,
            _ => 5
        };
    }
}

// ==================== DTOs pour les soins infirmier ====================

public class MarquerExecutionRequest
{
    public string? Observations { get; set; }
}

public class EnregistrerExecutionRequest
{
    public int? IdExecution { get; set; }
    public int? NumeroSeance { get; set; }
    public string? Observations { get; set; }
}
