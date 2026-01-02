using Mediconnet_Backend.Controllers.Base;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Medecin;
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
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MedecinController> _logger;

    public MedecinController(
        IMedecinService medecinService,
        ApplicationDbContext context, 
        ILogger<MedecinController> logger)
    {
        _medecinService = medecinService;
        _context = context;
        _logger = logger;
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
            _logger.LogError($"Error getting agenda: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération de l'agenda" });
        }
    }

    /// <summary>
    /// Obtenir les rendez-vous d'aujourd'hui (file d'attente)
    /// Inclut: patients enregistrés à l'accueil ET RDV confirmés du jour
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
            var now = DateTime.Now;

            // 1. RDV avec consultation prête (patients enregistrés à l'accueil)
            var rdvsAccueil = await _context.RendezVous
                .Include(r => r.Patient).ThenInclude(p => p!.Utilisateur)
                .Where(r => r.IdMedecin == userId.Value &&
                           r.DateHeure >= today &&
                           r.DateHeure < tomorrow &&
                           r.Statut != "annule")
                .Where(r => _context.Consultations.Any(c =>
                    c.IdRendezVous == r.IdRendezVous && 
                    c.Statut == "pret_consultation"))
                .Select(r => new
                {
                    idConsultation = _context.Consultations
                        .Where(c => c.IdRendezVous == r.IdRendezVous)
                        .Select(c => c.IdConsultation)
                        .FirstOrDefault(),
                    idRendezVous = r.IdRendezVous,
                    dateHeure = r.DateHeure,
                    duree = r.Duree,
                    statut = r.Statut,
                    motif = r.Motif,
                    typeRdv = r.TypeRdv,
                    dateCreation = r.DateCreation,
                    dateModification = r.DateModification,
                    patientNom = r.Patient != null && r.Patient.Utilisateur != null ? r.Patient.Utilisateur.Nom : "",
                    patientPrenom = r.Patient != null && r.Patient.Utilisateur != null ? r.Patient.Utilisateur.Prenom : "",
                    patientId = r.IdPatient,
                    isPremiereConsultation = !_context.Consultations
                        .Any(c => c.IdPatient == r.IdPatient && 
                                  c.IdMedecin == userId.Value && 
                                  c.Statut == "terminee"),
                    specialiteId = specialiteId,
                    origine = "accueil",
                    heureArrivee = (DateTime?)_context.Consultations
                        .Where(c => c.IdRendezVous == r.IdRendezVous)
                        .Select(c => c.DateHeure)
                        .FirstOrDefault()
                })
                .ToListAsync();

            // 2. RDV confirmés du jour (pas encore enregistrés à l'accueil)
            var idsRdvAccueil = rdvsAccueil.Select(r => r.idRendezVous).ToHashSet();
            
            var rdvsConfirmes = await _context.RendezVous
                .Include(r => r.Patient).ThenInclude(p => p!.Utilisateur)
                .Where(r => r.IdMedecin == userId.Value &&
                           r.DateHeure >= today &&
                           r.DateHeure < tomorrow &&
                           r.Statut == "confirme" &&
                           r.DateHeure > now) // Seulement les créneaux à venir
                .Where(r => !_context.Consultations.Any(c =>
                    c.IdRendezVous == r.IdRendezVous && 
                    c.Statut == "pret_consultation"))
                .Select(r => new
                {
                    idConsultation = 0,
                    idRendezVous = r.IdRendezVous,
                    dateHeure = r.DateHeure,
                    duree = r.Duree,
                    statut = r.Statut,
                    motif = r.Motif,
                    typeRdv = r.TypeRdv,
                    dateCreation = r.DateCreation,
                    dateModification = r.DateModification,
                    patientNom = r.Patient != null && r.Patient.Utilisateur != null ? r.Patient.Utilisateur.Nom : "",
                    patientPrenom = r.Patient != null && r.Patient.Utilisateur != null ? r.Patient.Utilisateur.Prenom : "",
                    patientId = r.IdPatient,
                    isPremiereConsultation = !_context.Consultations
                        .Any(c => c.IdPatient == r.IdPatient && 
                                  c.IdMedecin == userId.Value && 
                                  c.Statut == "terminee"),
                    specialiteId = specialiteId,
                    origine = "rdv_confirme",
                    heureArrivee = (DateTime?)null
                })
                .ToListAsync();

            // 3. Fusionner et trier par ordre chronologique (heure du créneau)
            var fileAttente = rdvsAccueil
                .Concat(rdvsConfirmes.Where(r => !idsRdvAccueil.Contains(r.idRendezVous)))
                .OrderBy(r => r.dateHeure)
                .ToList();

            return Ok(fileAttente);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting today's rdv: {ex.Message}");
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
            _logger.LogError($"Error getting upcoming rdv: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la récupération des prochains RDV" });
        }
    }
}
