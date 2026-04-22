using Mediconnet_Backend.Controllers.Base;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Medecin;
using Mediconnet_Backend.DTOs.Hospitalisation;
using Mediconnet_Backend.DTOs.Prescription;
using Mediconnet_Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// Contrôleur pour la gestion du profil médecin
/// Délègue la logique métier au service IMedecinService
/// </summary>
[Route("api/[controller]")]
public class MedecinController : BaseApiController
{
    private readonly IMedecinService _medecinService;
    private readonly IHospitalisationService _hospitalisationService;
    private readonly IPrescriptionService _prescriptionService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MedecinController> _logger;
    private readonly INotificationService _notificationService;

    public MedecinController(
        IMedecinService medecinService,
        IHospitalisationService hospitalisationService,
        IPrescriptionService prescriptionService,
        ApplicationDbContext context, 
        ILogger<MedecinController> logger,
        INotificationService notificationService)
    {
        _medecinService = medecinService;
        _hospitalisationService = hospitalisationService;
        _prescriptionService = prescriptionService;
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Obtenir le profil complet du médecin connecté
    /// </summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var profile = await _medecinService.GetProfileAsync(userId.Value);
            if (profile == null)
                return NotFound(new { message = "Médecin non trouvé" });

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting medecin profile: {Message}", ex.Message);
            return StatusCode(500, new { message = "Erreur lors de la récupération du profil" });
        }
    }

    /// <summary>
    /// Obtenir les statistiques du dashboard médecin
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var dashboard = await _medecinService.GetDashboardAsync(userId.Value);
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting medecin dashboard: {Message}", ex.Message);
            return StatusCode(500, new { message = "Erreur lors de la récupération du dashboard" });
        }
    }

    /// <summary>
    /// Mettre à jour le profil du médecin
    /// </summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateMedecinProfileRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var success = await _medecinService.UpdateProfileAsync(userId.Value, request);
            if (!success)
                return NotFound(new { message = "Médecin non trouvé" });

            return Ok(new { message = "Profil mis à jour avec succès" });
        }
        catch (Exception ex)
        {
            _logger.LogError("Error updating medecin profile: {Message}", ex.Message);
            return StatusCode(500, new { message = "Erreur lors de la mise à jour du profil" });
        }
    }

    /// <summary>
    /// Obtenir l'agenda du médecin (créneaux et RDV)
    /// </summary>
    [HttpGet("agenda")]
    public async Task<IActionResult> GetAgenda([FromQuery] string dateDebut, [FromQuery] string dateFin)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            if (!DateTime.TryParse(dateDebut, out var debut) || !DateTime.TryParse(dateFin, out var fin))
                return BadRequest(new { message = "Dates invalides" });

            var now = DateTime.Now;

            // Récupérer les créneaux configurés
            var creneauxConfigures = await _context.CreneauxDisponibles
                .Where(c => c.IdMedecin == userId.Value && c.Actif)
                .ToListAsync();

            // Récupérer les RDV existants
            var rdvExistants = await _context.RendezVous
                .Include(r => r.Patient).ThenInclude(p => p!.Utilisateur)
                .Where(r => r.IdMedecin == userId.Value &&
                           r.Statut != "annule" &&
                           r.DateHeure >= debut &&
                           r.DateHeure <= fin.AddDays(1))
                .ToListAsync();

            // Récupérer les indisponibilités
            var indisponibilites = await _context.IndisponibilitesMedecin
                .Where(i => i.IdMedecin == userId.Value &&
                           i.DateDebut <= fin &&
                           i.DateFin >= debut)
                .ToListAsync();

            var jours = new List<object>();
            var joursSemaine = new[] { "Dim", "Lun", "Mar", "Mer", "Jeu", "Ven", "Sam" };

            for (var date = debut.Date; date <= fin.Date; date = date.AddDays(1))
            {
                var slots = new List<object>();
                int totalDispo = 0, totalOccupe = 0, totalIndispo = 0;
                var rdvDejaAjoutes = new HashSet<int>(); // Pour éviter les doublons

                // Vérifier indisponibilité du jour
                var indispoJour = indisponibilites.FirstOrDefault(i =>
                    date >= i.DateDebut.Date && date <= i.DateFin.Date);

                // Convertir jour de la semaine
                var jourSemaine = (int)date.DayOfWeek;
                if (jourSemaine == 0) jourSemaine = 7;

                // Récupérer les plages pour ce jour
                var plagesJour = creneauxConfigures.Where(c => c.JourSemaine == jourSemaine).ToList();

                // Générer les slots à partir des créneaux configurés
                foreach (var plage in plagesJour)
                {
                    var heureActuelle = plage.HeureDebut;
                    var duree = plage.DureeParDefaut > 0 ? plage.DureeParDefaut : 30;

                    while (heureActuelle.Add(TimeSpan.FromMinutes(duree)) <= plage.HeureFin)
                    {
                        var dateHeure = date.Add(heureActuelle);
                        string statut;
                        string? raison = null;
                        int? idRdv = null;
                        string? patientNom = null, patientPrenom = null, motif = null;

                        // Vérifier d'abord s'il y a un RDV sur ce créneau
                        var rdv = rdvExistants.FirstOrDefault(r =>
                            dateHeure < r.DateHeure.AddMinutes(r.Duree) &&
                            dateHeure.AddMinutes(duree) > r.DateHeure);

                        if (dateHeure <= now)
                        {
                            if (rdv != null)
                            {
                                statut = "passe";
                                idRdv = rdv.IdRendezVous;
                                patientNom = rdv.Patient?.Utilisateur?.Nom;
                                patientPrenom = rdv.Patient?.Utilisateur?.Prenom;
                                motif = rdv.Motif;
                                totalOccupe++;
                                rdvDejaAjoutes.Add(rdv.IdRendezVous);
                            }
                            else
                            {
                                statut = "passe";
                            }
                        }
                        else if (indispoJour != null)
                        {
                            statut = "indisponible";
                            raison = indispoJour.Motif ?? indispoJour.Type;
                            totalIndispo++;
                        }
                        else if (rdv != null)
                        {
                            statut = "occupe";
                            idRdv = rdv.IdRendezVous;
                            patientNom = rdv.Patient?.Utilisateur?.Nom;
                            patientPrenom = rdv.Patient?.Utilisateur?.Prenom;
                            motif = rdv.Motif;
                            totalOccupe++;
                            rdvDejaAjoutes.Add(rdv.IdRendezVous);
                        }
                        else
                        {
                            statut = "disponible";
                            totalDispo++;
                        }

                        slots.Add(new
                        {
                            dateHeure = dateHeure,
                            duree = duree,
                            statut = statut,
                            disponible = statut == "disponible",
                            raison = raison,
                            idRendezVous = idRdv,
                            patientNom = patientNom,
                            patientPrenom = patientPrenom,
                            motif = motif
                        });

                        heureActuelle = heureActuelle.Add(TimeSpan.FromMinutes(duree));
                    }
                }

                // IMPORTANT: Ajouter les RDV du jour qui ne correspondent pas aux créneaux configurés
                // (ex: RDV pris alors que les créneaux sont désactivés)
                var rdvDuJour = rdvExistants.Where(r => r.DateHeure.Date == date).ToList();
                foreach (var rdv in rdvDuJour)
                {
                    if (!rdvDejaAjoutes.Contains(rdv.IdRendezVous))
                    {
                        var statut = rdv.DateHeure <= now ? "passe" : "occupe";
                        slots.Add(new
                        {
                            dateHeure = rdv.DateHeure,
                            duree = rdv.Duree,
                            statut = statut,
                            disponible = false,
                            raison = (string?)null,
                            idRendezVous = rdv.IdRendezVous,
                            patientNom = rdv.Patient?.Utilisateur?.Nom,
                            patientPrenom = rdv.Patient?.Utilisateur?.Prenom,
                            motif = rdv.Motif
                        });
                        totalOccupe++;
                        rdvDejaAjoutes.Add(rdv.IdRendezVous);
                    }
                }

                jours.Add(new
                {
                    date = date.ToString("yyyy-MM-dd"),
                    jourSemaine = joursSemaine[(int)date.DayOfWeek],
                    slots = slots.OrderBy(s => ((dynamic)s).dateHeure).ToList(),
                    totalDisponibles = totalDispo,
                    totalOccupes = totalOccupe,
                    totalIndisponibles = totalIndispo
                });
            }

            return Ok(new
            {
                dateDebut = debut.ToString("yyyy-MM-dd"),
                dateFin = fin.ToString("yyyy-MM-dd"),
                jours = jours
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agenda");
            return StatusCode(500, new { message = "Erreur lors de la récupération de l'agenda" });
        }
    }

    /// <summary>
    /// Obtenir les rendez-vous d'aujourd'hui (file d'attente)
    /// Inclut: tous les RDV du jour (confirmés, en attente infirmier, prêts pour consultation)
    /// Le médecin peut démarrer une consultation à tout moment, sans dépendance préalable à l'infirmière
    /// Triés par ordre chronologique (heure du créneau)
    /// </summary>
    [HttpGet("rdv/aujourdhui")]
    public async Task<IActionResult> GetRdvAujourdHui()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            // Récupérer la spécialité du médecin connecté
            var medecin = await _context.Medecins
                .Where(m => m.IdUser == userId.Value)
                .Select(m => new { m.IdSpecialite })
                .FirstOrDefaultAsync();
            
            var specialiteId = medecin?.IdSpecialite ?? 0;

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Récupérer tous les RDV du jour (non annulés)
            var rdvsDuJour = await _context.RendezVous
                .Include(r => r.Patient).ThenInclude(p => p!.Utilisateur)
                .Where(r => r.IdMedecin == userId.Value &&
                           r.DateHeure >= today &&
                           r.DateHeure < tomorrow &&
                           r.Statut != "annule")
                .Select(r => new
                {
                    rdv = r,
                    consultation = _context.Consultations
                        .Where(c => c.IdRendezVous == r.IdRendezVous)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var fileAttente = rdvsDuJour.Select(item => new
            {
                idConsultation = item.consultation?.IdConsultation ?? 0,
                idRendezVous = item.rdv.IdRendezVous,
                dateHeure = item.rdv.DateHeure,
                duree = item.rdv.Duree,
                statut = item.rdv.Statut,
                statutConsultation = item.consultation?.Statut,
                motif = item.rdv.Motif,
                typeRdv = item.rdv.TypeRdv,
                dateCreation = item.rdv.DateCreation,
                dateModification = item.rdv.DateModification,
                patientNom = item.rdv.Patient?.Utilisateur?.Nom ?? "",
                patientPrenom = item.rdv.Patient?.Utilisateur?.Prenom ?? "",
                patientId = item.rdv.IdPatient,
                isPremiereConsultation = !_context.Consultations
                    .Any(c => c.IdPatient == item.rdv.IdPatient && 
                              c.IdMedecin == userId.Value && 
                              (c.Statut == "terminee" || c.Statut == "termine")),
                specialiteId = specialiteId,
                origine = item.consultation != null ? 
                    (item.consultation.Statut == "pret_consultation" ? "accueil" : "consultation_existante") : 
                    "rdv_confirme",
                heureArrivee = item.consultation?.DateHeure,
                hasParametresVitaux = item.consultation != null && 
                    _context.Parametres.Any(p => p.IdConsultation == item.consultation.IdConsultation)
            })
            .OrderBy(r => r.dateHeure)
            .ToList();

            return Ok(fileAttente);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting today's rdv");
            return StatusCode(500, new { message = "Erreur lors de la récupération des RDV" });
        }
    }

    /// <summary>
    /// Obtenir les prochains rendez-vous
    /// </summary>
    [HttpGet("rdv/prochains")]
    public async Task<IActionResult> GetProchainRdv([FromQuery] int limite = 5)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var now = DateTime.Now;

            var rdvs = await _context.RendezVous
                .Include(r => r.Patient).ThenInclude(p => p!.Utilisateur)
                .Where(r => r.IdMedecin == userId.Value &&
                           r.DateHeure > now &&
                           (r.Statut == "planifie" || r.Statut == "confirme"))
                .OrderBy(r => r.DateHeure)
                .Take(limite)
                .Select(r => new
                {
                    idRendezVous = r.IdRendezVous,
                    dateHeure = r.DateHeure,
                    duree = r.Duree,
                    statut = r.Statut,
                    motif = r.Motif,
                    typeRdv = r.TypeRdv,
                    patientNom = r.Patient != null && r.Patient.Utilisateur != null ? r.Patient.Utilisateur.Nom : "",
                    patientPrenom = r.Patient != null && r.Patient.Utilisateur != null ? r.Patient.Utilisateur.Prenom : "",
                    patientId = r.IdPatient
                })
                .ToListAsync();

            return Ok(rdvs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upcoming rdv");
            return StatusCode(500, new { message = "Erreur lors de la récupération des prochains RDV" });
        }
    }

    /// <summary>
    /// Obtenir les patients hospitalisés du médecin
    /// </summary>
    [HttpGet("patients/hospitalises")]
    public async Task<IActionResult> GetPatientsHospitalises()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            // Récupérer les patients hospitalisés par ce médecin (EN_ATTENTE = en attente de lit, EN_COURS = hospitalisé)
            var statutsActifs = new[] { "EN_ATTENTE", "EN_COURS" };
            var patientsHospitalises = await _context.Hospitalisations
                .Include(h => h.Patient).ThenInclude(p => p!.Utilisateur)
                .Include(h => h.Lit).ThenInclude(l => l!.Chambre).ThenInclude(c => c!.Standard)
                .Include(h => h.Service)
                .Where(h => statutsActifs.Contains(h.Statut!))
                .Where(h => h.IdMedecin == userId.Value)
                .Select(h => new
                {
                    idAdmission = h.IdAdmission,
                    idPatient = h.IdPatient,
                    patientNom = h.Patient != null && h.Patient.Utilisateur != null ? h.Patient.Utilisateur.Nom : "",
                    patientPrenom = h.Patient != null && h.Patient.Utilisateur != null ? h.Patient.Utilisateur.Prenom : "",
                    numeroDossier = h.Patient != null ? h.Patient.NumeroDossier : null,
                    sexe = h.Patient != null && h.Patient.Utilisateur != null ? h.Patient.Utilisateur.Sexe : null,
                    dateNaissance = h.Patient != null && h.Patient.Utilisateur != null ? h.Patient.Utilisateur.Naissance : (DateTime?)null,
                    telephone = h.Patient != null && h.Patient.Utilisateur != null ? h.Patient.Utilisateur.Telephone : null,
                    dateEntree = h.DateEntree,
                    dateSortiePrevue = h.DateSortie,
                    motif = h.Motif,
                    statut = h.Statut,
                    urgence = h.Urgence,
                    numeroLit = h.Lit != null ? h.Lit.Numero : null,
                    numeroChambre = h.Lit != null && h.Lit.Chambre != null ? h.Lit.Chambre.Numero : null,
                    idLit = h.IdLit,
                    idChambre = h.Lit != null ? h.Lit.IdChambre : 0,
                    service = h.Service != null ? h.Service.NomService : null
                })
                .OrderByDescending(h => h.urgence == "critique")
                .ThenByDescending(h => h.urgence == "urgente")
                .ThenByDescending(h => h.statut == "EN_ATTENTE")
                .ThenBy(h => h.dateEntree)
                .ToListAsync();

            return Ok(new { 
                success = true, 
                data = patientsHospitalises,
                total = patientsHospitalises.Count,
                enAttente = patientsHospitalises.Count(h => h.statut == "EN_ATTENTE"),
                enCours = patientsHospitalises.Count(h => h.statut == "EN_COURS")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hospitalized patients");
            return StatusCode(500, new { message = "Erreur lors de la récupération des patients hospitalisés" });
        }
    }

    /// <summary>
    /// Obtenir les détails d'une hospitalisation
    /// </summary>
    [HttpGet("hospitalisation/{idAdmission}")]
    public async Task<IActionResult> GetHospitalisationDetail(int idAdmission)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var hospitalisation = await _context.Hospitalisations
                .Include(h => h.Patient).ThenInclude(p => p!.Utilisateur)
                .Include(h => h.Lit).ThenInclude(l => l!.Chambre)
                .Where(h => h.IdAdmission == idAdmission)
                .Select(h => new
                {
                    idAdmission = h.IdAdmission,
                    idPatient = h.IdPatient,
                    patientNom = h.Patient != null && h.Patient.Utilisateur != null ? h.Patient.Utilisateur.Nom : "",
                    patientPrenom = h.Patient != null && h.Patient.Utilisateur != null ? h.Patient.Utilisateur.Prenom : "",
                    numeroDossier = h.Patient != null ? h.Patient.NumeroDossier : null,
                    sexe = h.Patient != null && h.Patient.Utilisateur != null ? h.Patient.Utilisateur.Sexe : null,
                    dateNaissance = h.Patient != null && h.Patient.Utilisateur != null ? h.Patient.Utilisateur.Naissance : (DateTime?)null,
                    telephone = h.Patient != null && h.Patient.Utilisateur != null ? h.Patient.Utilisateur.Telephone : null,
                    email = h.Patient != null && h.Patient.Utilisateur != null ? h.Patient.Utilisateur.Email : null,
                    adresse = h.Patient != null && h.Patient.Utilisateur != null ? h.Patient.Utilisateur.Adresse : null,
                    groupeSanguin = h.Patient != null ? h.Patient.GroupeSanguin : null,
                    personneContact = h.Patient != null ? h.Patient.PersonneContact : null,
                    numeroContact = h.Patient != null ? h.Patient.NumeroContact : null,
                    dateEntree = h.DateEntree,
                    dateSortiePrevue = h.DateSortie,
                    motif = h.Motif,
                    statut = h.Statut,
                    numeroLit = h.Lit != null ? h.Lit.Numero : null,
                    numeroChambre = h.Lit != null && h.Lit.Chambre != null ? h.Lit.Chambre.Numero : null,
                    idLit = h.IdLit,
                    idChambre = h.Lit != null ? h.Lit.IdChambre : 0,
                    dureeJours = (int)(DateTime.Now - h.DateEntree).TotalDays
                })
                .FirstOrDefaultAsync();

            if (hospitalisation == null)
                return NotFound(new { message = "Hospitalisation non trouvée" });

            // Récupérer les dernières consultations du patient avec ce médecin
            var consultations = await _context.Consultations
                .Where(c => c.IdPatient == hospitalisation.idPatient && c.IdMedecin == userId.Value)
                .OrderByDescending(c => c.DateHeure)
                .Take(5)
                .Select(c => new
                {
                    idConsultation = c.IdConsultation,
                    dateConsultation = c.DateHeure,
                    motif = c.Motif,
                    diagnostic = c.Diagnostic,
                    statut = c.Statut
                })
                .ToListAsync();

            return Ok(new { 
                success = true, 
                hospitalisation,
                consultations
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hospitalisation detail");
            return StatusCode(500, new { message = "Erreur lors de la récupération des détails" });
        }
    }

    // ==================== HOSPITALISATION - CHAMBRES DISPONIBLES ====================

    /// <summary>
    /// Récupérer les standards de chambre actifs pour sélection
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
                    privileges = s.Privileges,
                    localisation = s.Localisation,
                    chambresDisponibles = _context.Chambres
                        .Count(c => c.IdStandard == s.IdStandard && c.Statut == "actif" && 
                               c.Lits != null && c.Lits.Any(l => l.Statut == "libre"))
                })
                .OrderBy(s => s.nom)
                .ToListAsync();

            return Ok(new { success = true, data = standards });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting standards for hospitalisation");
            return StatusCode(500, new { message = "Erreur lors de la récupération des standards" });
        }
    }

    /// <summary>
    /// Récupérer les chambres disponibles par standard
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
    /// Attribuer un lit à une hospitalisation en attente (Médecin peut aussi le faire)
    /// </summary>
    [HttpPost("hospitalisation/attribuer-lit")]
    public async Task<IActionResult> AttribuerLit([FromBody] AttribuerLitRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Utilisateur non authentifié" });

            var role = GetCurrentUserRole() ?? "medecin";
            var result = await _hospitalisationService.AttribuerLitAsync(request, userId.Value, role);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            _logger.LogInformation("Lit attribué par médecin: Hospitalisation {IdAdmission}, Lit {IdLit}, Médecin {MedecinId}", 
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
            _logger.LogError(ex, "Erreur AttribuerLit (médecin)");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Ordonner une hospitalisation (nouveau workflow)
    /// Le médecin ne choisit PAS de lit - le Major du service l'attribuera
    /// Notifications envoyées au patient (email + cloche) et au Major (cloche)
    /// </summary>
    [HttpPost("hospitalisation/ordonner")]
    public async Task<IActionResult> OrdonnerHospitalisation([FromBody] OrdonnerHospitalisationRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Utilisateur non authentifié" });

            var result = await _hospitalisationService.OrdonnerHospitalisationAsync(request, userId.Value);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            _logger.LogInformation("Hospitalisation ordonnée: Patient {PatientId}, Médecin {MedecinId}", 
                request.IdPatient, userId.Value);

            return Ok(new
            {
                success = true,
                message = result.Message,
                idAdmission = result.IdAdmission
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ordering hospitalisation");
            return StatusCode(500, new { message = "Erreur lors de l'ordonnance de l'hospitalisation" });
        }
    }

    /// <summary>
    /// Ordonner une hospitalisation complète avec prescriptions (nouveau workflow multi-étapes)
    /// Inclut: hospitalisation + examens + médicaments + soins complémentaires
    /// </summary>
    [HttpPost("hospitalisation/ordonner-complete")]
    public async Task<IActionResult> OrdonnerHospitalisationComplete([FromBody] OrdonnerHospitalisationCompleteRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Utilisateur non authentifié" });

            var result = await _hospitalisationService.OrdonnerHospitalisationCompleteAsync(request, userId.Value);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            _logger.LogInformation("Hospitalisation complète ordonnée: Patient {PatientId}, Médecin {MedecinId}, Examens: {NbExamens}, Médicaments: {NbMedicaments}", 
                request.IdPatient, userId.Value, request.Examens?.Count ?? 0, request.Medicaments?.Count ?? 0);

            return Ok(new
            {
                success = true,
                message = result.Message,
                idAdmission = result.IdAdmission
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ordering complete hospitalisation");
            return StatusCode(500, new { message = "Erreur lors de l'ordonnance de l'hospitalisation complète" });
        }
    }

    // ==================== CONSULTATION DIRECTE ====================

    /// <summary>
    /// Créer et démarrer une consultation directement à partir d'un RDV confirmé
    /// Permet au médecin d'initier une consultation sans passage préalable par l'infirmière
    /// </summary>
    [HttpPost("rdv/{idRendezVous}/creer-consultation")]
    public async Task<IActionResult> CreerConsultationDirecte(int idRendezVous)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            // Vérifier que le RDV existe et appartient à ce médecin
            var rdv = await _context.RendezVous
                .Include(r => r.Patient)
                .FirstOrDefaultAsync(r => r.IdRendezVous == idRendezVous && r.IdMedecin == userId.Value);

            if (rdv == null)
                return NotFound(new { success = false, message = "Rendez-vous non trouvé" });

            // Vérifier si une consultation existe déjà pour ce RDV
            var existingConsultation = await _context.Consultations
                .FirstOrDefaultAsync(c => c.IdRendezVous == idRendezVous);

            if (existingConsultation != null)
            {
                // Consultation existe déjà, retourner son ID
                return Ok(new { 
                    success = true, 
                    idConsultation = existingConsultation.IdConsultation,
                    message = "Consultation existante récupérée",
                    isNew = false
                });
            }

            // Créer une nouvelle consultation
            var consultation = new Consultation
            {
                IdPatient = rdv.IdPatient,
                IdMedecin = userId.Value,
                IdRendezVous = idRendezVous,
                DateHeure = DateTime.UtcNow,
                Motif = rdv.Motif,
                Statut = "en_cours",
                UpdatedAt = DateTime.UtcNow
            };

            _context.Consultations.Add(consultation);

            // Mettre à jour le statut du RDV
            rdv.Statut = "en_cours";
            rdv.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Consultation directe créée: {IdConsultation} pour RDV {IdRendezVous} par médecin {MedecinId}", 
                consultation.IdConsultation, idRendezVous, userId.Value);

            return Ok(new { 
                success = true, 
                idConsultation = consultation.IdConsultation,
                message = "Consultation créée et démarrée",
                isNew = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating direct consultation");
            return StatusCode(500, new { success = false, message = "Erreur lors de la création de la consultation" });
        }
    }

    /// <summary>
    /// Créer une consultation spontanée pour un patient (sans RDV préalable)
    /// Permet au médecin d'initier une consultation à tout moment
    /// </summary>
    [HttpPost("patient/{idPatient}/consultation-spontanee")]
    public async Task<IActionResult> CreerConsultationSpontanee(int idPatient, [FromBody] CreerConsultationSpontaneeRequest? request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            // Vérifier que le patient existe
            var patient = await _context.Patients
                .Include(p => p.Utilisateur)
                .FirstOrDefaultAsync(p => p.IdUser == idPatient);

            if (patient == null)
                return NotFound(new { success = false, message = "Patient non trouvé" });

            // Créer une nouvelle consultation sans RDV
            var consultation = new Consultation
            {
                IdPatient = idPatient,
                IdMedecin = userId.Value,
                IdRendezVous = null,
                DateHeure = DateTime.UtcNow,
                Motif = request?.Motif ?? "Consultation spontanée",
                Statut = "en_cours",
                UpdatedAt = DateTime.UtcNow
            };

            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Consultation spontanée créée: {IdConsultation} pour patient {IdPatient} par médecin {MedecinId}", 
                consultation.IdConsultation, idPatient, userId.Value);

            return Ok(new { 
                success = true, 
                idConsultation = consultation.IdConsultation,
                message = "Consultation spontanée créée"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating spontaneous consultation");
            return StatusCode(500, new { success = false, message = "Erreur lors de la création de la consultation" });
        }
    }

    /// <summary>
    /// Récupérer les détails complets d'une hospitalisation (Médecin)
    /// </summary>
    [HttpGet("hospitalisation/{idAdmission}/details")]
    public async Task<IActionResult> GetHospitalisationDetails(int idAdmission)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Utilisateur non authentifié" });

            var hospitalisation = await _context.Hospitalisations
                .Include(h => h.Patient)
                    .ThenInclude(p => p!.Utilisateur)
                .Include(h => h.Medecin)
                    .ThenInclude(m => m!.Utilisateur)
                .Include(h => h.Lit)
                    .ThenInclude(l => l!.Chambre)
                        .ThenInclude(c => c!.Standard)
                .Include(h => h.Service)
                .FirstOrDefaultAsync(h => h.IdAdmission == idAdmission && h.IdMedecin == userId.Value);

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

            // Récupérer les soins liés à cette hospitalisation avec progression
            var soins = await _context.SoinsHospitalisation
                .Include(s => s.Prescripteur)
                    .ThenInclude(m => m!.Utilisateur)
                .Include(s => s.Executions)
                .Where(s => s.IdHospitalisation == idAdmission)
                .OrderByDescending(s => s.DatePrescription)
                .Select(s => new
                {
                    idSoin = s.IdSoin,
                    typeSoin = s.TypeSoin ?? "",
                    description = s.Description ?? "",
                    dureeJours = s.DureeJours,
                    moments = s.Moments,
                    priorite = s.Priorite ?? "normale",
                    instructions = s.Instructions,
                    statut = s.Statut ?? "prescrit",
                    datePrescription = s.DatePrescription,
                    dateDebut = s.DateDebut,
                    dateFinPrevue = s.DateFinPrevue,
                    prescripteur = s.Prescripteur != null && s.Prescripteur.Utilisateur != null 
                        ? $"Dr {s.Prescripteur.Utilisateur.Prenom} {s.Prescripteur.Utilisateur.Nom}" 
                        : null,
                    nbExecutionsPrevues = s.NbExecutionsPrevues,
                    nbExecutionsEffectuees = s.Executions.Count(e => e.Statut == "fait"),
                    nbExecutionsManquees = s.Executions.Count(e => e.Statut == "manque"),
                    progression = s.NbExecutionsPrevues > 0 
                        ? Math.Round((double)s.Executions.Count(e => e.Statut == "fait") / s.NbExecutionsPrevues * 100, 1)
                        : 0
                })
                .ToListAsync();

            // Récupérer les examens prescrits pour cette hospitalisation
            // Inclut les examens liés directement à l'hospitalisation OU via la consultation initiale de l'hospitalisation
            var examens = await _context.Set<BulletinExamen>()
                .Include(b => b.Examen)
                    .ThenInclude(e => e!.Specialite)
                .Include(b => b.Laboratoire)
                .Where(b => b.IdHospitalisation == idAdmission 
                    || (hospitalisation.IdConsultation != null && b.IdConsultation == hospitalisation.IdConsultation))
                .OrderByDescending(b => b.DateDemande)
                .Select(b => new
                {
                    idExamen = b.IdBulletinExamen,
                    idBulletinExamen = b.IdBulletinExamen,
                    typeExamen = b.Examen != null ? (b.Examen.Specialite != null ? b.Examen.Specialite.Nom : "Examen") : "Examen",
                    description = b.Examen != null ? b.Examen.NomExamen : (b.Instructions ?? "Examen prescrit"),
                    datePrescription = b.DateDemande,
                    statut = b.Statut ?? "prescrit",
                    urgence = b.Urgence,
                    laboratoire = b.Laboratoire != null ? b.Laboratoire.NomLabo : null,
                    resultat = b.ResultatTexte,
                    dateResultat = b.DateResultat,
                    hasResultat = b.DateResultat != null || !string.IsNullOrEmpty(b.ResultatTexte)
                })
                .ToListAsync();

            // Récupérer les prescriptions médicamenteuses liées à cette hospitalisation
            // Inclut: ordonnances directement liées à l'hospitalisation OU via consultation pendant l'hospitalisation
            var prescriptions = await _context.Set<PrescriptionMedicament>()
                .Include(pm => pm.Ordonnance)
                    .ThenInclude(o => o!.Consultation)
                .Include(pm => pm.Ordonnance)
                    .ThenInclude(o => o!.Patient)
                .Include(pm => pm.Medicament)
                .Where(pm => pm.Ordonnance != null 
                    && (
                        // Ordonnances directement liées à l'hospitalisation
                        pm.Ordonnance.IdHospitalisation == idAdmission
                        // OU ordonnances via consultation pendant l'hospitalisation
                        || (pm.Ordonnance.Consultation != null 
                            && pm.Ordonnance.Consultation.IdPatient == hospitalisation.IdPatient
                            && pm.Ordonnance.Date >= hospitalisation.DateEntree.Date
                            && (hospitalisation.DateSortie == null || pm.Ordonnance.Date <= hospitalisation.DateSortie.Value.Date))
                    ))
                .OrderByDescending(pm => pm.Ordonnance!.Date)
                .Select(pm => new
                {
                    idPrescription = pm.IdPrescriptionMed,
                    medicament = pm.Medicament != null ? pm.Medicament.Nom : "Médicament",
                    dosage = pm.Medicament != null ? pm.Medicament.Dosage : null,
                    posologie = pm.Posologie ?? "",
                    frequence = pm.Frequence,
                    voieAdministration = pm.VoieAdministration,
                    duree = pm.DureeTraitement,
                    dateDebut = pm.Ordonnance!.Date,
                    instructions = pm.Instructions
                })
                .ToListAsync();

            var result = new
            {
                idAdmission = hospitalisation.IdAdmission,
                statut = hospitalisation.Statut ?? "en_attente",
                urgence = hospitalisation.Urgence ?? "normale",
                dateEntree = hospitalisation.DateEntree,
                dateSortiePrevue = hospitalisation.DateSortiePrevue,
                dateSortie = hospitalisation.Statut == "termine" ? hospitalisation.DateSortie : (DateTime?)null,
                motifSortie = hospitalisation.MotifSortie,
                resumeMedical = hospitalisation.ResumeMedical,
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

    // ==================== SOINS HOSPITALISATION ====================

    /// <summary>
    /// Ajouter un soin à une hospitalisation avec génération automatique des exécutions planifiées
    /// Supporte les séances (nb fois par jour) avec horaires auto ou personnalisés
    /// </summary>
    [HttpPost("hospitalisation/{idAdmission}/soins")]
    public async Task<IActionResult> AjouterSoin(int idAdmission, [FromBody] AjouterSoinRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Utilisateur non authentifié" });

            var hospitalisation = await _context.Hospitalisations
                .FirstOrDefaultAsync(h => h.IdAdmission == idAdmission && h.IdMedecin == userId.Value);

            if (hospitalisation == null)
                return NotFound(new { success = false, message = "Hospitalisation non trouvée" });

            var dureeJours = request.DureeJours ?? 7;
            var dateDebut = request.DateDebut ?? DateTime.UtcNow.Date;
            var dateFinPrevue = dateDebut.AddDays(dureeJours - 1);
            var nbFoisParJour = request.NbFoisParJour ?? 1;

            // Générer ou parser les horaires
            List<TimeSpan> horaires;
            string? horairesJson = null;

            if (!string.IsNullOrEmpty(request.HorairesPersonnalises))
            {
                // Horaires personnalisés fournis
                horaires = ParseHorairesPersonnalises(request.HorairesPersonnalises);
                horairesJson = request.HorairesPersonnalises;
                nbFoisParJour = horaires.Count;
            }
            else
            {
                // Générer horaires automatiquement
                horaires = GenererHorairesAutomatiques(nbFoisParJour);
                horairesJson = System.Text.Json.JsonSerializer.Serialize(
                    horaires.Select(h => h.ToString(@"hh\:mm")).ToList()
                );
            }

            var nbExecutionsPrevues = dureeJours * nbFoisParJour;

            var soin = new SoinHospitalisation
            {
                IdHospitalisation = idAdmission,
                TypeSoin = request.TypeSoin ?? "soins_infirmiers",
                Description = request.Description ?? "",
                Frequence = $"{nbFoisParJour}x/jour",
                DureeJours = dureeJours,
                NbFoisParJour = nbFoisParJour,
                HorairesPersonnalises = horairesJson,
                Moments = null, // Legacy
                Priorite = request.Priorite ?? "normale",
                Instructions = request.Instructions,
                Statut = "prescrit",
                DatePrescription = DateTime.UtcNow,
                DateDebut = dateDebut,
                DateFinPrevue = dateFinPrevue,
                IdPrescripteur = userId.Value,
                NbExecutionsPrevues = nbExecutionsPrevues,
                NbExecutionsEffectuees = 0
            };

            _context.SoinsHospitalisation.Add(soin);
            await _context.SaveChangesAsync();

            // Générer les exécutions avec séances
            var executions = GenererExecutionsAvecSeances(soin.IdSoin, dateDebut, dureeJours, horaires);
            _context.ExecutionsSoins.AddRange(executions);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Soin ajouté avec {NbExec} exécutions ({NbSeances} séances/jour): Hospitalisation {IdAdmission}", 
                executions.Count, nbFoisParJour, idAdmission);

            return Ok(new { 
                success = true, 
                message = "Soin ajouté avec succès", 
                idSoin = soin.IdSoin,
                nbExecutionsPlanifiees = executions.Count,
                horaires = horaires.Select(h => h.ToString(@"hh\:mm")).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding soin");
            return StatusCode(500, new { message = "Erreur lors de l'ajout du soin" });
        }
    }

    /// <summary>
    /// Générer un aperçu des horaires automatiques (sans créer le soin)
    /// </summary>
    [HttpGet("soins/horaires-auto")]
    public IActionResult GetHorairesAutomatiques([FromQuery] int nbFoisParJour = 1)
    {
        if (nbFoisParJour < 1) nbFoisParJour = 1;
        if (nbFoisParJour > 12) nbFoisParJour = 12;

        var horaires = GenererHorairesAutomatiques(nbFoisParJour);
        return Ok(new {
            success = true,
            nbSeances = nbFoisParJour,
            horaires = horaires.Select((h, i) => new {
                seance = i + 1,
                heure = h.ToString(@"hh\:mm")
            }).ToList()
        });
    }

    /// <summary>
    /// Générer des horaires automatiques répartis sur la journée (6h-22h)
    /// </summary>
    private List<TimeSpan> GenererHorairesAutomatiques(int nbFoisParJour)
    {
        var horaires = new List<TimeSpan>();
        if (nbFoisParJour <= 0) nbFoisParJour = 1;
        if (nbFoisParJour > 12) nbFoisParJour = 12;

        // Plage horaire: 6h à 22h (16 heures)
        var heureDebut = 6; // 6h
        var heureFin = 22;  // 22h
        var plageMinutes = (heureFin - heureDebut) * 60;

        if (nbFoisParJour == 1)
        {
            horaires.Add(new TimeSpan(8, 0, 0)); // 8h par défaut
        }
        else
        {
            var intervalleMinutes = plageMinutes / (nbFoisParJour);
            for (int i = 0; i < nbFoisParJour; i++)
            {
                var minutesDepuisDebut = (int)(intervalleMinutes * i + intervalleMinutes / 2);
                var heure = heureDebut + (minutesDepuisDebut / 60);
                var minutes = (minutesDepuisDebut % 60 / 30) * 30; // Arrondir à 30 min
                horaires.Add(new TimeSpan(heure, minutes, 0));
            }
        }

        return horaires;
    }

    /// <summary>
    /// Parser les horaires personnalisés depuis JSON ["08:00","12:00","18:00"]
    /// </summary>
    private List<TimeSpan> ParseHorairesPersonnalises(string horairesJson)
    {
        try
        {
            var heures = System.Text.Json.JsonSerializer.Deserialize<List<string>>(horairesJson);
            if (heures == null || !heures.Any())
                return new List<TimeSpan> { new TimeSpan(8, 0, 0) };

            return heures
                .Select(h => TimeSpan.TryParse(h, out var ts) ? ts : (TimeSpan?)null)
                .Where(ts => ts.HasValue)
                .Select(ts => ts!.Value)
                .OrderBy(ts => ts)
                .ToList();
        }
        catch
        {
            return new List<TimeSpan> { new TimeSpan(8, 0, 0) };
        }
    }

    /// <summary>
    /// Générer les exécutions planifiées avec numéro de séance
    /// </summary>
    private List<ExecutionSoin> GenererExecutionsAvecSeances(int idSoin, DateTime dateDebut, int dureeJours, List<TimeSpan> horaires)
    {
        var executions = new List<ExecutionSoin>();
        var numeroExecution = 1;

        for (int jour = 0; jour < dureeJours; jour++)
        {
            var datePrevue = dateDebut.AddDays(jour);
            var numeroSeance = 1;

            foreach (var heure in horaires)
            {
                executions.Add(new ExecutionSoin
                {
                    IdSoin = idSoin,
                    DatePrevue = datePrevue,
                    NumeroSeance = numeroSeance,
                    Moment = null, // Legacy - on utilise NumeroSeance + HeurePrevue
                    HeurePrevue = heure,
                    Statut = "prevu",
                    NumeroExecution = numeroExecution++,
                    CreatedAt = DateTime.UtcNow
                });
                numeroSeance++;
            }
        }

        return executions;
    }

    /// <summary>
    /// Modifier le statut d'un soin
    /// </summary>
    [HttpPut("hospitalisation/soins/{idSoin}/statut")]
    public async Task<IActionResult> ModifierStatutSoin(int idSoin, [FromBody] ModifierStatutSoinRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Utilisateur non authentifié" });

            var soin = await _context.SoinsHospitalisation
                .Include(s => s.Hospitalisation)
                .FirstOrDefaultAsync(s => s.IdSoin == idSoin && s.Hospitalisation!.IdMedecin == userId.Value);

            if (soin == null)
                return NotFound(new { success = false, message = "Soin non trouvé" });

            soin.Statut = request.Statut ?? soin.Statut;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Statut du soin mis à jour" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating soin status");
            return StatusCode(500, new { message = "Erreur lors de la mise à jour du statut" });
        }
    }

    /// <summary>
    /// Enregistrer automatiquement l'exécution d'un soin (médecin peut aussi le faire)
    /// Trouve l'exécution prévue la plus proche dans le temps et la marque comme faite
    /// </summary>
    [HttpPost("soins/{idSoin}/executer")]
    public async Task<IActionResult> EnregistrerExecutionSoin(int idSoin, [FromBody] EnregistrerExecutionMedecinRequest? request)
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

            // Trouver l'exécution prévue la plus proche (non encore faite)
            var executionsPrevues = await _context.ExecutionsSoins
                .Where(e => e.IdSoin == idSoin && e.Statut == "prevu")
                .OrderBy(e => e.DatePrevue)
                .ThenBy(e => e.HeurePrevue)
                .ToListAsync();

            if (!executionsPrevues.Any())
                return BadRequest(new { success = false, message = "Aucune exécution prévue à enregistrer pour ce soin" });

            // Trouver l'exécution la plus proche
            ExecutionSoin? executionCible = null;

            // D'abord chercher une exécution pour aujourd'hui
            var executionsAujourdhui = executionsPrevues
                .Where(e => e.DatePrevue.HasValue && e.DatePrevue.Value.Date == maintenant.Date)
                .ToList();

            if (executionsAujourdhui.Any())
            {
                executionCible = executionsAujourdhui
                    .OrderBy(e => Math.Abs((e.HeurePrevue ?? TimeSpan.Zero).TotalMinutes - heureActuelle.TotalMinutes))
                    .FirstOrDefault();
            }
            else
            {
                executionCible = executionsPrevues.FirstOrDefault();
            }

            if (executionCible == null)
                return BadRequest(new { success = false, message = "Impossible de trouver une exécution à enregistrer" });

            var execution = executionCible;

            if (execution.Statut == "fait")
                return BadRequest(new { success = false, message = "Cette exécution a déjà été enregistrée" });

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

            if (soin.NbExecutionsEffectuees >= soin.NbExecutionsPrevues)
            {
                soin.Statut = "termine";
            }
            else if (soin.Statut == "prescrit")
            {
                soin.Statut = "en_cours";
            }

            await _context.SaveChangesAsync();

            var executant = await _context.Utilisateurs.FindAsync(userId.Value);
            var nomExecutant = executant != null ? $"{executant.Prenom} {executant.Nom}" : "Inconnu";

            // ==================== NOTIFICATIONS SOIN EFFECTUÉ ====================
            try
            {
                var destinataires = new List<int>();
                
                // Patient
                var patient = await _context.Hospitalisations
                    .Where(h => h.IdAdmission == soin.IdHospitalisation)
                    .Select(h => h.Patient!.IdUser)
                    .FirstOrDefaultAsync();
                if (patient > 0) destinataires.Add(patient);
                
                // Infirmiers du service
                var infirmiers = await _context.Utilisateurs
                    .Where(u => u.Role == "infirmier")
                    .Select(u => u.IdUser)
                    .Take(10)
                    .ToListAsync();
                destinataires.AddRange(infirmiers);
                
                // Major du service
                var majors = await _context.Utilisateurs
                    .Where(u => u.Role == "major")
                    .Select(u => u.IdUser)
                    .ToListAsync();
                destinataires.AddRange(majors);

                if (destinataires.Any())
                {
                    await _notificationService.CreateBulkAsync(new CreateBulkNotificationRequest
                    {
                        UserIds = destinataires.Distinct().ToList(),
                        Type = "soin_effectue",
                        Titre = "Soin effectué",
                        Message = $"{soin.TypeSoin} - {soin.Description} effectué par Dr {nomExecutant}",
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
                "Soin {IdSoin} exécuté par médecin: Exécution {IdExecution} ({Moment}) marquée faite à {Heure} par {Executant}", 
                idSoin, execution.IdExecution, execution.Moment, maintenant.ToString("HH:mm"), nomExecutant);

            return Ok(new { 
                success = true, 
                message = "Soin enregistré avec succès",
                data = new {
                    idExecution = execution.IdExecution,
                    moment = execution.Moment,
                    datePrevue = execution.DatePrevue,
                    heureExecution = maintenant.ToString("HH:mm"),
                    executant = nomExecutant,
                    nbExecutionsRestantes = soin.NbExecutionsPrevues - soin.NbExecutionsEffectuees
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering soin execution (medecin)");
            return StatusCode(500, new { message = "Erreur lors de l'enregistrement du soin" });
        }
    }

    /// <summary>
    /// Récupérer les soins d'une hospitalisation avec progression d'exécution
    /// </summary>
    [HttpGet("hospitalisation/{idAdmission}/soins")]
    public async Task<IActionResult> GetSoinsHospitalisation(int idAdmission)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Utilisateur non authentifié" });

            var soins = await _context.SoinsHospitalisation
                .Include(s => s.Prescripteur).ThenInclude(m => m!.Utilisateur)
                .Include(s => s.Executions)
                .Where(s => s.IdHospitalisation == idAdmission)
                .OrderByDescending(s => s.DatePrescription)
                .Select(s => new
                {
                    idSoin = s.IdSoin,
                    typeSoin = s.TypeSoin,
                    description = s.Description,
                    frequence = s.Frequence,
                    dureeJours = s.DureeJours,
                    moments = s.Moments,
                    priorite = s.Priorite,
                    instructions = s.Instructions,
                    statut = s.Statut,
                    datePrescription = s.DatePrescription,
                    dateDebut = s.DateDebut,
                    dateFinPrevue = s.DateFinPrevue,
                    prescripteur = s.Prescripteur != null && s.Prescripteur.Utilisateur != null
                        ? $"Dr {s.Prescripteur.Utilisateur.Prenom} {s.Prescripteur.Utilisateur.Nom}"
                        : null,
                    nbExecutionsPrevues = s.NbExecutionsPrevues,
                    nbExecutionsEffectuees = s.Executions.Count(e => e.Statut == "fait"),
                    nbExecutionsManquees = s.Executions.Count(e => e.Statut == "manque"),
                    progression = s.NbExecutionsPrevues > 0 
                        ? Math.Round((double)s.Executions.Count(e => e.Statut == "fait") / s.NbExecutionsPrevues * 100, 1)
                        : 0
                })
                .ToListAsync();

            return Ok(new { success = true, data = soins });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting soins");
            return StatusCode(500, new { message = "Erreur lors de la récupération des soins" });
        }
    }

    /// <summary>
    /// Récupérer les détails d'un soin avec toutes ses exécutions planifiées
    /// </summary>
    [HttpGet("hospitalisation/soins/{idSoin}/details")]
    public async Task<IActionResult> GetSoinDetails(int idSoin)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Utilisateur non authentifié" });

            var soin = await _context.SoinsHospitalisation
                .Include(s => s.Prescripteur).ThenInclude(m => m!.Utilisateur)
                .Include(s => s.Executions).ThenInclude(e => e.Executant)
                .Include(s => s.Hospitalisation).ThenInclude(h => h!.Patient)
                .FirstOrDefaultAsync(s => s.IdSoin == idSoin);

            if (soin == null)
                return NotFound(new { success = false, message = "Soin non trouvé" });

            // Grouper les exécutions par jour
            var executionsParJour = soin.Executions
                .Where(e => e.DatePrevue.HasValue)
                .GroupBy(e => e.DatePrevue!.Value.Date)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    date = g.Key,
                    jourSemaine = g.Key.ToString("dddd", new System.Globalization.CultureInfo("fr-FR")),
                    executions = g.OrderBy(e => e.NumeroSeance).ThenBy(e => e.HeurePrevue).Select(e => new
                    {
                        idExecution = e.IdExecution,
                        datePrevue = e.DatePrevue,
                        moment = e.Moment ?? "autre",
                        numeroSeance = e.NumeroSeance,
                        heurePrevue = e.HeurePrevue?.ToString(@"hh\:mm"),
                        heureExecution = e.HeureExecution?.ToString(@"hh\:mm"),
                        statut = e.Statut ?? "prevu",
                        dateExecution = e.DateExecution,
                        executant = e.Executant != null ? $"{e.Executant.Prenom} {e.Executant.Nom}" : null,
                        observations = e.Observations
                    }).ToList()
                })
                .ToList();

            var result = new
            {
                idSoin = soin.IdSoin,
                typeSoin = soin.TypeSoin,
                description = soin.Description,
                frequence = soin.Frequence,
                dureeJours = soin.DureeJours,
                moments = soin.Moments,
                priorite = soin.Priorite,
                instructions = soin.Instructions,
                statut = soin.Statut,
                datePrescription = soin.DatePrescription,
                dateDebut = soin.DateDebut,
                dateFinPrevue = soin.DateFinPrevue,
                prescripteur = soin.Prescripteur?.Utilisateur != null
                    ? $"Dr {soin.Prescripteur.Utilisateur.Prenom} {soin.Prescripteur.Utilisateur.Nom}"
                    : null,
                patient = soin.Hospitalisation?.Patient?.Utilisateur != null
                    ? $"{soin.Hospitalisation.Patient.Utilisateur.Prenom} {soin.Hospitalisation.Patient.Utilisateur.Nom}"
                    : null,
                nbExecutionsPrevues = soin.NbExecutionsPrevues,
                nbExecutionsEffectuees = soin.Executions.Count(e => e.Statut == "fait"),
                nbExecutionsManquees = soin.Executions.Count(e => e.Statut == "manque"),
                nbExecutionsPrevuesRestantes = soin.Executions.Count(e => e.Statut == "prevu"),
                executionsParJour = executionsParJour
            };

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting soin details");
            return StatusCode(500, new { message = "Erreur lors de la récupération des détails du soin" });
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

    /// <summary>
    /// Prescrire plusieurs examens pour une hospitalisation
    /// </summary>
    [HttpPost("hospitalisation/{idAdmission}/examens")]
    public async Task<IActionResult> PrescrireExamens(int idAdmission, [FromBody] PrescrireExamensHospRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Utilisateur non authentifié" });

            var hospitalisation = await _context.Hospitalisations
                .FirstOrDefaultAsync(h => h.IdAdmission == idAdmission && h.IdMedecin == userId.Value);

            if (hospitalisation == null)
                return NotFound(new { success = false, message = "Hospitalisation non trouvée" });

            if (request.Examens == null || !request.Examens.Any())
                return BadRequest(new { success = false, message = "Aucun examen spécifié" });

            var bulletinsCreated = new List<int>();

            foreach (var examen in request.Examens)
            {
                // Chercher l'examen dans le catalogue ou utiliser les instructions pour stocker le nom
                int? idExamen = null;
                var examenCatalogue = await _context.Set<ExamenCatalogue>()
                    .FirstOrDefaultAsync(e => e.NomExamen.ToLower().Contains(examen.NomExamen!.ToLower()));
                
                if (examenCatalogue != null)
                {
                    idExamen = examenCatalogue.IdExamen;
                }

                // Construire les instructions avec le type et nom si pas trouvé dans le catalogue
                var instructions = string.IsNullOrEmpty(examen.Notes) 
                    ? $"[{examen.TypeExamen}] {examen.NomExamen}" 
                    : $"[{examen.TypeExamen}] {examen.NomExamen} - {examen.Notes}";

                var bulletin = new BulletinExamen
                {
                    DateDemande = DateTime.UtcNow,
                    IdHospitalisation = idAdmission,
                    IdExamen = idExamen,
                    IdLabo = examen.IdLaboratoire,
                    Instructions = instructions,
                    Urgence = examen.Urgence,
                    Statut = "prescrit"
                };

                _context.Set<BulletinExamen>().Add(bulletin);
                await _context.SaveChangesAsync();
                bulletinsCreated.Add(bulletin.IdBulletinExamen);
            }

            _logger.LogInformation("Examens prescrits: Hospitalisation {IdAdmission}, {NbExamens} examens", 
                idAdmission, request.Examens.Count);

            return Ok(new { 
                success = true, 
                message = $"{request.Examens.Count} examen(s) prescrit(s) avec succès", 
                idBulletins = bulletinsCreated 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error prescribing exams");
            return StatusCode(500, new { success = false, message = "Erreur lors de la prescription des examens" });
        }
    }

    /// <summary>
    /// Ajouter une ordonnance pour une hospitalisation
    /// Utilise le service centralisé IPrescriptionService
    /// </summary>
    [HttpPost("hospitalisation/{idAdmission}/ordonnance")]
    public async Task<IActionResult> AjouterOrdonnanceHospitalisation(int idAdmission, [FromBody] OrdonnanceHospitalisationRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Utilisateur non authentifié" });

            if (request.Medicaments == null || !request.Medicaments.Any())
                return BadRequest(new { success = false, message = "Aucun médicament spécifié" });

            // Convertir les médicaments vers le format du service centralisé
            var medicamentsRequest = request.Medicaments.Select(med => new MedicamentPrescriptionRequest
            {
                NomMedicament = med.NomMedicament ?? "",
                Dosage = med.Dosage,
                Quantite = med.Quantite ?? 1,
                Posologie = med.Posologie,
                VoieAdministration = med.VoieAdministration,
                FormePharmaceutique = med.FormePharmaceutique,
                Instructions = med.Instructions,
                DureeTraitement = med.Duree
            }).ToList();

            // Utiliser le service centralisé
            var result = await _prescriptionService.CreerOrdonnanceHospitalisationAsync(
                idAdmission,
                medicamentsRequest,
                request.Notes,
                userId.Value);

            if (!result.Success)
            {
                return BadRequest(new { 
                    success = false, 
                    message = result.Message,
                    erreurs = result.Erreurs
                });
            }

            _logger.LogInformation("Ordonnance créée pour hospitalisation {IdAdmission}: {NbMedicaments} médicaments (via service centralisé)", 
                idAdmission, request.Medicaments.Count);

            return Ok(new { 
                success = true, 
                message = "Ordonnance enregistrée avec succès", 
                idOrdonnance = result.IdOrdonnance,
                alertes = result.Alertes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ordonnance for hospitalisation");
            return StatusCode(500, new { success = false, message = "Erreur lors de la création de l'ordonnance" });
        }
    }
}

// ==================== DTOs pour les soins ====================

public class AjouterSoinRequest
{
    public string? TypeSoin { get; set; }
    public string? Description { get; set; }
    public string? Frequence { get; set; }
    public int? DureeJours { get; set; }
    public string? Moments { get; set; } // Legacy
    public string? Priorite { get; set; }
    public string? Instructions { get; set; }
    public DateTime? DateDebut { get; set; }
    /// <summary>
    /// Nombre de fois par jour (1 à N séances)
    /// </summary>
    public int? NbFoisParJour { get; set; }
    /// <summary>
    /// Horaires personnalisés au format JSON: ["08:00","12:00","18:00"]
    /// Si null, les horaires sont générés automatiquement
    /// </summary>
    public string? HorairesPersonnalises { get; set; }
}

public class ModifierStatutSoinRequest
{
    public string? Statut { get; set; }
}

public class PrescrireExamenHospRequest
{
    public int? IdExamen { get; set; }
    public int? IdLabo { get; set; }
    public string? Instructions { get; set; }
    public bool Urgence { get; set; } = false;
}

public class PrescrireExamensHospRequest
{
    public List<ExamenHospDto>? Examens { get; set; }
    public string? Notes { get; set; }
}

public class ExamenHospDto
{
    public string? TypeExamen { get; set; }
    public string? NomExamen { get; set; }
    public string? Description { get; set; }
    public bool Urgence { get; set; } = false;
    public string? Notes { get; set; }
    public int? IdLaboratoire { get; set; }
}

public class CreerConsultationSpontaneeRequest
{
    public string? Motif { get; set; }
}

public class OrdonnanceHospitalisationRequest
{
    public List<MedicamentOrdonnanceDto>? Medicaments { get; set; }
    public string? Notes { get; set; }
    public string? DureeTraitement { get; set; }
}

public class MedicamentOrdonnanceDto
{
    public string? NomMedicament { get; set; }
    public string? Dosage { get; set; }
    public string? Posologie { get; set; }
    public string? FormePharmaceutique { get; set; }
    public string? VoieAdministration { get; set; }
    public string? Duree { get; set; }
    public string? Instructions { get; set; }
    public int? Quantite { get; set; }
}

public class EnregistrerExecutionMedecinRequest
{
    public string? Observations { get; set; }
}
