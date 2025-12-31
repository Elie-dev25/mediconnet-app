using Mediconnet_Backend.Controllers.Base;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Accueil;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// Controleur pour la gestion de l'accueil
/// Permet l'enregistrement des patients a leur arrivee
/// </summary>
[Route("api/[controller]")]
[Authorize(Roles = "accueil,administrateur")]
public class AccueilController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly IConsultationService _consultationService;
    private readonly ILogger<AccueilController> _logger;

    public AccueilController(
        ApplicationDbContext context,
        IConsultationService consultationService,
        ILogger<AccueilController> logger)
    {
        _context = context;
        _consultationService = consultationService;
        _logger = logger;
    }

    /// <summary>
    /// Obtenir le profil de l'agent d'accueil connecte
    /// </summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var accueil = await _context.Accueils
                .Include(a => a.Utilisateur)
                .FirstOrDefaultAsync(a => a.IdUser == userId.Value);

            if (accueil == null || accueil.Utilisateur == null)
                return NotFound(new { message = "Agent d'accueil non trouve" });

            var profile = new AccueilProfileDto
            {
                IdUser = accueil.IdUser,
                Nom = accueil.Utilisateur.Nom,
                Prenom = accueil.Utilisateur.Prenom,
                Email = accueil.Utilisateur.Email,
                Telephone = accueil.Utilisateur.Telephone,
                Poste = accueil.Poste,
                DateEmbauche = accueil.DateEmbauche,
                CreatedAt = accueil.Utilisateur.CreatedAt
            };

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting accueil profile: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la recuperation du profil" });
        }
    }

    /// <summary>
    /// Obtenir les statistiques du dashboard accueil
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Patients enregistres aujourd'hui
            var patientsAujourdHui = await _context.Patients
                .CountAsync(p => p.DateCreation >= today && p.DateCreation < tomorrow);

            // RDV prevus aujourd'hui
            var rdvAujourdHui = await _context.RendezVous
                .CountAsync(r => r.DateHeure >= today && 
                               r.DateHeure < tomorrow &&
                               r.Statut != "annule");

            // RDV en cours (confirmes)
            var rdvEnCours = await _context.RendezVous
                .CountAsync(r => r.DateHeure >= today && 
                               r.DateHeure < tomorrow &&
                               r.Statut == "confirme");

            var dashboard = new AccueilDashboardDto
            {
                PatientsEnregistresAujourdHui = patientsAujourdHui,
                PatientsEnAttente = 0, // A implementer avec table file_attente
                RdvPrevusAujourdHui = rdvAujourdHui,
                RdvEnCours = rdvEnCours
            };

            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting accueil dashboard: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la recuperation du dashboard" });
        }
    }

    /// <summary>
    /// Rechercher un patient par nom, telephone ou numero de dossier
    /// </summary>
    [HttpGet("patients/recherche")]
    public async Task<IActionResult> RechercherPatient([FromQuery] string? terme, [FromQuery] string? numeroDossier, [FromQuery] string? telephone)
    {
        try
        {
            var query = _context.Patients
                .Include(p => p.Utilisateur)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(numeroDossier))
            {
                query = query.Where(p => p.NumeroDossier == numeroDossier);
            }
            else if (!string.IsNullOrWhiteSpace(telephone))
            {
                query = query.Where(p => p.Utilisateur != null && 
                                        p.Utilisateur.Telephone != null &&
                                        p.Utilisateur.Telephone.Contains(telephone));
            }
            else if (!string.IsNullOrWhiteSpace(terme))
            {
                var termeLower = terme.ToLower();
                query = query.Where(p => p.Utilisateur != null && 
                    (p.Utilisateur.Nom.ToLower().Contains(termeLower) ||
                     p.Utilisateur.Prenom.ToLower().Contains(termeLower) ||
                     (p.NumeroDossier != null && p.NumeroDossier.Contains(terme))));
            }
            else
            {
                return BadRequest(new { message = "Veuillez fournir un critere de recherche" });
            }

            var patients = await query
                .Take(20)
                .Select(p => new
                {
                    idPatient = p.IdUser,
                    numeroDossier = p.NumeroDossier,
                    nom = p.Utilisateur!.Nom,
                    prenom = p.Utilisateur.Prenom,
                    telephone = p.Utilisateur.Telephone,
                    email = p.Utilisateur.Email,
                    dateNaissance = p.Utilisateur.Naissance,
                    sexe = p.Utilisateur.Sexe
                })
                .ToListAsync();

            return Ok(patients);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error searching patients: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la recherche" });
        }
    }

    /// <summary>
    /// Enregistrer l'arrivee d'un patient (nouveau ou existant)
    /// </summary>
    [HttpPost("patients/arrivee")]
    public async Task<IActionResult> EnregistrerArrivee([FromBody] EnregistrerArriveePatientRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Verifier si le patient existe deja (par telephone ou email)
            Patient? patientExistant = null;
            
            if (!string.IsNullOrEmpty(request.Telephone))
            {
                patientExistant = await _context.Patients
                    .Include(p => p.Utilisateur)
                    .FirstOrDefaultAsync(p => p.Utilisateur != null && 
                                             p.Utilisateur.Telephone == request.Telephone);
            }
            
            if (patientExistant == null && !string.IsNullOrEmpty(request.Email))
            {
                patientExistant = await _context.Patients
                    .Include(p => p.Utilisateur)
                    .FirstOrDefaultAsync(p => p.Utilisateur != null && 
                                             p.Utilisateur.Email == request.Email);
            }

            if (patientExistant != null)
            {
                // Patient existant - marquer son arrivee
                _logger.LogInformation($"Patient existant trouve: {patientExistant.IdUser}");
                
                // Si RDV specifie, le confirmer
                if (request.IdRendezVous.HasValue)
                {
                    var rdv = await _context.RendezVous
                        .FirstOrDefaultAsync(r => r.IdRendezVous == request.IdRendezVous.Value);
                    if (rdv != null)
                    {
                        if (rdv.Statut == "planifie")
                        {
                            rdv.Statut = "confirme";
                        }

                        // Toujours tracer l'heure d'arrivée/confirmation à l'accueil
                        rdv.DateModification = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new EnregistrerArriveeResponse
                {
                    Success = true,
                    Message = "Arrivee enregistree avec succes",
                    IdPatient = patientExistant.IdUser,
                    NumeroDossier = patientExistant.NumeroDossier,
                    NouveauPatient = false
                });
            }

            // Nouveau patient - creer le compte
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Generer numero de dossier unique
                var numeroDossier = await GenererNumeroDossier();

                // Creer l'utilisateur
                var utilisateur = new Utilisateur
                {
                    Nom = request.Nom,
                    Prenom = request.Prenom,
                    Email = request.Email ?? $"patient_{numeroDossier}@temp.local",
                    Telephone = request.Telephone,
                    Naissance = request.DateNaissance,
                    Sexe = request.Sexe,
                    Role = "patient",
                    EmailConfirmed = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Utilisateurs.Add(utilisateur);
                await _context.SaveChangesAsync();

                // Creer le patient
                var patient = new Patient
                {
                    IdUser = utilisateur.IdUser,
                    NumeroDossier = numeroDossier,
                    DateCreation = DateTime.UtcNow
                };

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation($"Nouveau patient enregistre: {utilisateur.IdUser}, Dossier: {numeroDossier}");

                return Ok(new EnregistrerArriveeResponse
                {
                    Success = true,
                    Message = $"Nouveau patient enregistre avec le dossier {numeroDossier}",
                    IdPatient = utilisateur.IdUser,
                    NumeroDossier = numeroDossier,
                    NouveauPatient = true
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error registering patient arrival: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de l'enregistrement" });
        }
    }

    /// <summary>
    /// Obtenir les RDV du jour pour l'accueil
    /// </summary>
    [HttpGet("rdv/aujourdhui")]
    public async Task<IActionResult> GetRdvAujourdHui()
    {
        try
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var rdvs = await _context.RendezVous
                .Include(r => r.Patient).ThenInclude(p => p!.Utilisateur)
                .Include(r => r.Medecin).ThenInclude(m => m!.Utilisateur)
                .Where(r => r.DateHeure >= today && 
                           r.DateHeure < tomorrow &&
                           r.Statut != "annule")
                .OrderBy(r => r.DateHeure)
                .Select(r => new RdvAccueilDto
                {
                    IdRendezVous = r.IdRendezVous,
                    DateHeure = r.DateHeure,
                    PatientNom = r.Patient != null && r.Patient.Utilisateur != null ? r.Patient.Utilisateur.Nom : "",
                    PatientPrenom = r.Patient != null && r.Patient.Utilisateur != null ? r.Patient.Utilisateur.Prenom : "",
                    PatientTelephone = r.Patient != null && r.Patient.Utilisateur != null ? r.Patient.Utilisateur.Telephone : null,
                    MedecinNom = r.Medecin != null && r.Medecin.Utilisateur != null ? r.Medecin.Utilisateur.Nom : "",
                    MedecinPrenom = r.Medecin != null && r.Medecin.Utilisateur != null ? r.Medecin.Utilisateur.Prenom : "",
                    Statut = r.Statut,
                    Motif = r.Motif,
                    PatientArrive = r.Statut == "confirme" || r.Statut == "en_cours" || r.Statut == "termine"
                })
                .ToListAsync();

            return Ok(rdvs);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting today's rdv: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la recuperation des RDV" });
        }
    }

    /// <summary>
    /// Marquer l'arrivee d'un patient pour un RDV
    /// </summary>
    [HttpPost("rdv/marquer-arrivee")]
    public async Task<IActionResult> MarquerArriveeRdv([FromBody] MarquerArriveeRdvRequest request)
    {
        try
        {
            var rdv = await _context.RendezVous
                .Include(r => r.Patient).ThenInclude(p => p!.Utilisateur)
                .FirstOrDefaultAsync(r => r.IdRendezVous == request.IdRendezVous);

            if (rdv == null)
                return NotFound(new { message = "Rendez-vous non trouve" });

            if (rdv.Statut == "annule")
                return BadRequest(new { message = "Ce rendez-vous a ete annule" });

            if (rdv.Statut == "termine")
                return BadRequest(new { message = "Ce rendez-vous est deja termine" });

            rdv.Statut = "confirme";
            rdv.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Arrivee marquee pour RDV {rdv.IdRendezVous}");

            return Ok(new 
            { 
                success = true, 
                message = "Arrivee du patient enregistree",
                patientNom = rdv.Patient?.Utilisateur?.Nom,
                patientPrenom = rdv.Patient?.Utilisateur?.Prenom
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error marking arrival: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de l'enregistrement de l'arrivee" });
        }
    }

    /// <summary>
    /// Enregistrer une consultation pour un patient
    /// </summary>
    [HttpPost("consultations/enregistrer")]
    public async Task<IActionResult> EnregistrerConsultation([FromBody] EnregistrerConsultationRequest request)
    {
        try
        {
            var authCheck = CheckAuthentication();
            if (authCheck != null) return authCheck;

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "Utilisateur non authentifié" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new EnregistrerConsultationResponse
                {
                    Success = false,
                    Message = "Données invalides"
                });
            }

            var result = await _consultationService.EnregistrerConsultationAsync(request, userId.Value);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation($"Consultation enregistrée: ID={result.IdConsultation}, Patient={request.IdPatient}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error registering consultation: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de l'enregistrement de la consultation" });
        }
    }

    /// <summary>
    /// Obtenir la liste des médecins disponibles
    /// </summary>
    [HttpGet("medecins/disponibles")]
    public async Task<IActionResult> GetMedecinsDisponibles()
    {
        try
        {
            var medecins = await _consultationService.GetMedecinsDisponiblesAsync();
            return Ok(medecins);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting available doctors: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des médecins" });
        }
    }

    /// <summary>
    /// Obtenir la liste des médecins filtrés par service et/ou spécialité
    /// </summary>
    [HttpGet("medecins/filtrer")]
    public async Task<IActionResult> GetMedecinsFiltres([FromQuery] int? idService, [FromQuery] int? idSpecialite)
    {
        try
        {
            var medecins = await _consultationService.GetMedecinsFiltresAsync(idService, idSpecialite);
            return Ok(medecins);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting filtered doctors: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des médecins" });
        }
    }

    /// <summary>
    /// Obtenir la liste des services hospitaliers
    /// </summary>
    [HttpGet("services")]
    public async Task<IActionResult> GetServices()
    {
        try
        {
            var services = await _consultationService.GetServicesAsync();
            return Ok(services);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting services: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des services" });
        }
    }

    /// <summary>
    /// Obtenir la liste des spécialités médicales
    /// </summary>
    [HttpGet("specialites")]
    public async Task<IActionResult> GetSpecialites()
    {
        try
        {
            var specialites = await _consultationService.GetSpecialitesAsync();
            return Ok(specialites);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting specialties: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des spécialités" });
        }
    }

    /// <summary>
    /// Obtenir la liste des médecins avec leur statut de disponibilité en temps réel
    /// </summary>
    [HttpGet("medecins/disponibilite")]
    public async Task<IActionResult> GetMedecinsAvecDisponibilite([FromQuery] int? idService, [FromQuery] int? idSpecialite)
    {
        try
        {
            var result = await _consultationService.GetMedecinsAvecDisponibiliteAsync(idService, idSpecialite);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting doctors availability: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération de la disponibilité des médecins" });
        }
    }

    /// <summary>
    /// Vérifier si un patient a un paiement de consultation encore valide
    /// </summary>
    [HttpGet("verifier-paiement/{idPatient}/{idMedecin}")]
    public async Task<IActionResult> VerifierPaiementValide(int idPatient, int idMedecin)
    {
        try
        {
            var result = await _consultationService.VerifierPaiementValideAsync(idPatient, idMedecin);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error checking payment validity: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la vérification du paiement" });
        }
    }

    /// <summary>
    /// Obtenir les créneaux du jour d'un médecin avec leur statut (disponible/occupé/passé)
    /// </summary>
    [HttpGet("medecins/{idMedecin}/creneaux-jour")]
    public async Task<IActionResult> GetCreneauxMedecinJour(int idMedecin)
    {
        try
        {
            var today = DateTime.Today;
            var now = DateTime.Now;
            
            // Récupérer le médecin avec sa spécialité
            var medecin = await _context.Medecins
                .Include(m => m.Specialite)
                .FirstOrDefaultAsync(m => m.IdUser == idMedecin);
            
            if (medecin == null)
                return NotFound(new { message = "Médecin non trouvé" });

            // Récupérer les créneaux horaires du médecin pour le jour actuel
            var jourSemaine = (int)today.DayOfWeek;
            if (jourSemaine == 0) jourSemaine = 7; // Dimanche = 7
            
            var creneauxMedecin = await _context.CreneauxDisponibles
                .Where(c => c.IdMedecin == idMedecin && c.JourSemaine == jourSemaine && c.Actif)
                .OrderBy(c => c.HeureDebut)
                .ToListAsync();

            // Récupérer les RDV confirmés du jour
            var tomorrow = today.AddDays(1);
            var rdvsConfirmes = await _context.RendezVous
                .Where(r => r.IdMedecin == idMedecin &&
                           r.DateHeure >= today &&
                           r.DateHeure < tomorrow &&
                           (r.Statut == "confirme" || r.Statut == "planifie"))
                .Select(r => new { r.DateHeure, r.Duree })
                .ToListAsync();

            // Récupérer les consultations en cours/prêtes du jour
            var consultationsActives = await _context.Consultations
                .Where(c => c.IdMedecin == idMedecin &&
                           c.DateHeure >= today &&
                           c.DateHeure < tomorrow &&
                           (c.Statut == "pret_consultation" || c.Statut == "en_cours"))
                .Select(c => c.DateHeure)
                .ToListAsync();

            // Construire la liste des créneaux avec leur statut
            var creneauxResult = new List<object>();
            
            foreach (var creneau in creneauxMedecin)
            {
                var heureDebut = today.Add(creneau.HeureDebut);
                var heureFin = today.Add(creneau.HeureFin);
                var dureeMinutes = (int)(heureFin - heureDebut).TotalMinutes;
                var intervalleMinutes = 30; // Intervalle standard de 30 min
                
                // Diviser le créneau en slots de 30 minutes
                var current = heureDebut;
                while (current < heureFin)
                {
                    var slotFin = current.AddMinutes(intervalleMinutes);
                    if (slotFin > heureFin) slotFin = heureFin;
                    
                    // Déterminer le statut du slot
                    string statut;
                    if (current < now)
                    {
                        statut = "passe"; // Gris
                    }
                    else if (rdvsConfirmes.Any(r => r.DateHeure <= current && r.DateHeure.AddMinutes(r.Duree) > current) ||
                             consultationsActives.Any(c => c <= current && c.AddMinutes(30) > current))
                    {
                        statut = "occupe"; // Rouge
                    }
                    else
                    {
                        statut = "disponible"; // Vert
                    }
                    
                    creneauxResult.Add(new
                    {
                        heureDebut = current.ToString("HH:mm"),
                        heureFin = slotFin.ToString("HH:mm"),
                        dateHeure = current,
                        statut = statut,
                        selectionnable = statut == "disponible"
                    });
                    
                    current = slotFin;
                }
            }

            // Coût de consultation de la spécialité
            var coutConsultation = medecin.Specialite?.CoutConsultation ?? 5000;

            return Ok(new
            {
                idMedecin = idMedecin,
                date = today.ToString("yyyy-MM-dd"),
                coutConsultation = coutConsultation,
                creneaux = creneauxResult
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting doctor slots: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des créneaux" });
        }
    }

    /// <summary>
    /// Generer un numero de dossier unique
    /// </summary>
    private async Task<string> GenererNumeroDossier()
    {
        var annee = DateTime.Now.Year.ToString();
        var prefix = $"PAT{annee}";
        
        // Trouver le dernier numero
        var dernierDossier = await _context.Patients
            .Where(p => p.NumeroDossier != null && p.NumeroDossier.StartsWith(prefix))
            .OrderByDescending(p => p.NumeroDossier)
            .Select(p => p.NumeroDossier)
            .FirstOrDefaultAsync();

        int numero = 1;
        if (!string.IsNullOrEmpty(dernierDossier))
        {
            var partieNumerique = dernierDossier.Substring(prefix.Length);
            if (int.TryParse(partieNumerique, out int dernierNumero))
            {
                numero = dernierNumero + 1;
            }
        }

        return $"{prefix}{numero:D5}";
    }
}
