using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Entities.Documents;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.DTOs.Laborantin;
using Mediconnet_Backend.Services;
using System.Security.Claims;

namespace Mediconnet_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "laborantin,administrateur")]
public class LaborantinController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IDocumentStorageService _storageService;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly IAssuranceCouvertureService _assuranceService;
    private readonly ILogger<LaborantinController> _logger;

    public LaborantinController(
        ApplicationDbContext context,
        IDocumentStorageService storageService,
        IEmailService emailService,
        INotificationService notificationService,
        IAssuranceCouvertureService assuranceService,
        ILogger<LaborantinController> logger)
    {
        _context = context;
        _storageService = storageService;
        _emailService = emailService;
        _notificationService = notificationService;
        _assuranceService = assuranceService;
        _logger = logger;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Récupérer les statistiques du dashboard laborantin
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var stats = new LaborantinStatsDto
            {
                ExamensEnAttente = await _context.BulletinsExamen
                    .CountAsync(b => b.Statut == "prescrit"),
                ExamensEnCours = await _context.BulletinsExamen
                    .CountAsync(b => b.Statut == "en_cours"),
                ExamensTerminesAujourdhui = await _context.BulletinsExamen
                    .CountAsync(b => b.Statut == "termine" && b.DateResultat >= today && b.DateResultat < tomorrow),
                Urgences = await _context.BulletinsExamen
                    .CountAsync(b => b.Urgence && (b.Statut == "prescrit" || b.Statut == "en_cours")),
                TotalExamensJour = await _context.BulletinsExamen
                    .CountAsync(b => b.DateDemande >= today && b.DateDemande < tomorrow)
            };

            return Ok(new { success = true, data = stats });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des statistiques laborantin");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer la liste des examens avec filtres et pagination
    /// </summary>
    [HttpGet("examens")]
    public async Task<IActionResult> GetExamens(
        [FromQuery] string? statut = null,
        [FromQuery] bool? urgence = null,
        [FromQuery] string? recherche = null,
        [FromQuery] DateTime? dateDebut = null,
        [FromQuery] DateTime? dateFin = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = _context.BulletinsExamen
                .Include(b => b.Examen)
                    .ThenInclude(e => e!.Specialite)
                        .ThenInclude(s => s!.Categorie)
                .Include(b => b.Laboratoire)
                .Include(b => b.Consultation)
                    .ThenInclude(c => c!.Patient)
                        .ThenInclude(p => p!.Utilisateur)
                .Include(b => b.Consultation)
                    .ThenInclude(c => c!.Medecin)
                        .ThenInclude(m => m!.Utilisateur)
                .Include(b => b.Hospitalisation)
                    .ThenInclude(h => h!.Patient)
                        .ThenInclude(p => p!.Utilisateur)
                .AsQueryable();

            // Filtres
            if (!string.IsNullOrEmpty(statut))
            {
                query = query.Where(b => b.Statut == statut);
            }

            if (urgence.HasValue)
            {
                query = query.Where(b => b.Urgence == urgence.Value);
            }

            if (dateDebut.HasValue)
            {
                query = query.Where(b => b.DateDemande >= dateDebut.Value);
            }

            if (dateFin.HasValue)
            {
                query = query.Where(b => b.DateDemande <= dateFin.Value);
            }

            if (!string.IsNullOrEmpty(recherche))
            {
                var searchLower = recherche.ToLower();
                query = query.Where(b =>
                    (b.Examen != null && b.Examen.NomExamen.ToLower().Contains(searchLower)) ||
                    (b.Instructions != null && b.Instructions.ToLower().Contains(searchLower)) ||
                    (b.Consultation != null && b.Consultation.Patient != null && b.Consultation.Patient.Utilisateur != null &&
                        (b.Consultation.Patient.Utilisateur.Nom.ToLower().Contains(searchLower) ||
                         b.Consultation.Patient.Utilisateur.Prenom.ToLower().Contains(searchLower))) ||
                    (b.Hospitalisation != null && b.Hospitalisation.Patient != null && b.Hospitalisation.Patient.Utilisateur != null &&
                        (b.Hospitalisation.Patient.Utilisateur.Nom.ToLower().Contains(searchLower) ||
                         b.Hospitalisation.Patient.Utilisateur.Prenom.ToLower().Contains(searchLower)))
                );
            }

            // Tri: urgences d'abord, puis par date
            query = query.OrderByDescending(b => b.Urgence)
                         .ThenByDescending(b => b.DateDemande);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var examens = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => MapToExamenDto(b))
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = new ExamensListResponse
                {
                    Examens = examens,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalPages
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des examens");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer les examens en attente pour le dashboard
    /// </summary>
    [HttpGet("examens/en-attente")]
    public async Task<IActionResult> GetExamensEnAttente([FromQuery] int limit = 10)
    {
        try
        {
            var examens = await _context.BulletinsExamen
                .Include(b => b.Examen)
                    .ThenInclude(e => e!.Specialite)
                .Include(b => b.Laboratoire)
                .Include(b => b.Consultation)
                    .ThenInclude(c => c!.Patient)
                        .ThenInclude(p => p!.Utilisateur)
                .Include(b => b.Consultation)
                    .ThenInclude(c => c!.Medecin)
                        .ThenInclude(m => m!.Utilisateur)
                .Include(b => b.Hospitalisation)
                    .ThenInclude(h => h!.Patient)
                        .ThenInclude(p => p!.Utilisateur)
                .Where(b => b.Statut == "prescrit" || b.Statut == "en_cours")
                .OrderByDescending(b => b.Urgence)
                .ThenByDescending(b => b.DateDemande)
                .Take(limit)
                .Select(b => MapToExamenDto(b))
                .ToListAsync();

            return Ok(new { success = true, data = examens });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des examens en attente");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer les détails complets d'un examen
    /// </summary>
    [HttpGet("examens/{idBulletin}")]
    public async Task<IActionResult> GetExamenDetails(int idBulletin)
    {
        try
        {
            var bulletin = await _context.BulletinsExamen
                .Include(b => b.Examen)
                    .ThenInclude(e => e!.Specialite)
                        .ThenInclude(s => s!.Categorie)
                .Include(b => b.Laboratoire)
                .Include(b => b.Consultation)
                    .ThenInclude(c => c!.Patient)
                        .ThenInclude(p => p!.Utilisateur)
                .Include(b => b.Consultation)
                    .ThenInclude(c => c!.Patient)
                        .ThenInclude(p => p!.Assurance)
                .Include(b => b.Consultation)
                    .ThenInclude(c => c!.Medecin)
                        .ThenInclude(m => m!.Utilisateur)
                .Include(b => b.Consultation)
                    .ThenInclude(c => c!.Medecin)
                        .ThenInclude(m => m!.Specialite)
                .Include(b => b.Hospitalisation)
                    .ThenInclude(h => h!.Patient)
                        .ThenInclude(p => p!.Utilisateur)
                .Include(b => b.Hospitalisation)
                    .ThenInclude(h => h!.Medecin)
                        .ThenInclude(m => m!.Utilisateur)
                .FirstOrDefaultAsync(b => b.IdBulletinExamen == idBulletin);

            if (bulletin == null)
            {
                return NotFound(new { success = false, message = "Examen non trouvé" });
            }

            // Récupérer le patient
            var patient = bulletin.Consultation?.Patient ?? bulletin.Hospitalisation?.Patient;
            var medecin = bulletin.Consultation?.Medecin ?? bulletin.Hospitalisation?.Medecin;

            // Récupérer les documents liés à cet examen
            var documentsRaw = await _context.Set<DocumentMedical>()
                .Where(d => d.IdBulletinExamen == idBulletin && d.Statut == "actif")
                .Select(d => new
                {
                    d.Uuid,
                    d.NomFichierOriginal,
                    d.MimeType,
                    d.TailleOctets,
                    d.CreatedAt
                })
                .ToListAsync();

            var documents = documentsRaw.Select(d => new DocumentResultatDto
            {
                Uuid = d.Uuid,
                NomFichier = d.NomFichierOriginal,
                MimeType = d.MimeType,
                TailleOctets = (long)d.TailleOctets,
                DateUpload = d.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList();

            // Récupérer le laborantin qui a validé (si applicable)
            string? laborantinNom = null;
            if (bulletin.IdBiologiste.HasValue)
            {
                var laborantin = await _context.Utilisateurs
                    .FirstOrDefaultAsync(u => u.IdUser == bulletin.IdBiologiste.Value);
                if (laborantin != null)
                {
                    laborantinNom = $"{laborantin.Prenom} {laborantin.Nom}";
                }
            }

            // Récupérer l'historique des examens similaires du patient
            var historique = new List<HistoriqueExamenDto>();
            if (patient != null)
            {
                historique = await _context.BulletinsExamen
                    .Include(b => b.Examen)
                    .Include(b => b.Consultation)
                    .Include(b => b.Hospitalisation)
                    .Where(b => b.IdBulletinExamen != idBulletin &&
                        ((b.Consultation != null && b.Consultation.IdPatient == patient.IdUser) ||
                         (b.Hospitalisation != null && b.Hospitalisation.IdPatient == patient.IdUser)) &&
                        (bulletin.IdExamen == null || b.IdExamen == bulletin.IdExamen))
                    .OrderByDescending(b => b.DateDemande)
                    .Take(5)
                    .Select(b => new HistoriqueExamenDto
                    {
                        IdBulletinExamen = b.IdBulletinExamen,
                        DateDemande = b.DateDemande,
                        NomExamen = b.Examen != null ? b.Examen.NomExamen : "Examen",
                        Statut = b.Statut ?? "prescrit",
                        DateResultat = b.DateResultat,
                        HasResultat = b.ResultatTexte != null || b.ResultatFichier != null
                    })
                    .ToListAsync();
            }

            var details = new ExamenDetailsDto
            {
                IdBulletinExamen = bulletin.IdBulletinExamen,
                DateDemande = bulletin.DateDemande,
                TypeExamen = bulletin.Examen?.Specialite?.Categorie?.Nom,
                NomExamen = bulletin.Examen?.NomExamen ?? ExtractExamenName(bulletin.Instructions),
                Description = bulletin.Examen?.Description,
                Categorie = bulletin.Examen?.Specialite?.Categorie?.Nom,
                Specialite = bulletin.Examen?.Specialite?.Nom,
                Instructions = bulletin.Instructions,
                Urgence = bulletin.Urgence,
                Statut = bulletin.Statut ?? "prescrit",
                Prix = bulletin.Examen?.PrixUnitaire,
                IdConsultation = bulletin.IdConsultation,
                IdHospitalisation = bulletin.IdHospitalisation,
                DateConsultation = bulletin.Consultation?.DateHeure,
                Patient = patient != null ? new PatientExamenDto
                {
                    IdPatient = patient.IdUser,
                    Nom = patient.Utilisateur?.Nom ?? "",
                    Prenom = patient.Utilisateur?.Prenom ?? "",
                    NumeroDossier = patient.NumeroDossier,
                    DateNaissance = patient.Utilisateur?.Naissance,
                    Sexe = patient.Utilisateur?.Sexe,
                    Telephone = patient.Utilisateur?.Telephone,
                    GroupeSanguin = patient.GroupeSanguin,
                    Allergies = patient.AllergiesDetails
                } : null,
                Medecin = medecin != null ? new MedecinExamenDto
                {
                    IdMedecin = medecin.IdUser,
                    Nom = medecin.Utilisateur?.Nom ?? "",
                    Prenom = medecin.Utilisateur?.Prenom ?? "",
                    Specialite = medecin.Specialite?.NomSpecialite,
                    Telephone = medecin.Utilisateur?.Telephone
                } : null,
                Laboratoire = bulletin.Laboratoire != null ? new LaboratoireDto
                {
                    IdLabo = bulletin.Laboratoire.IdLabo,
                    NomLabo = bulletin.Laboratoire.NomLabo,
                    Contact = bulletin.Laboratoire.Contact,
                    Adresse = bulletin.Laboratoire.Adresse,
                    Telephone = bulletin.Laboratoire.Telephone,
                    Type = bulletin.Laboratoire.Type
                } : null,
                Resultat = bulletin.DateResultat.HasValue ? new ResultatExamenDto
                {
                    DateResultat = bulletin.DateResultat,
                    ResultatTexte = bulletin.ResultatTexte,
                    CommentaireLabo = bulletin.CommentaireLabo,
                    IdLaborantin = bulletin.IdBiologiste,
                    LaborantinNom = laborantinNom,
                    Documents = documents
                } : null,
                Historique = historique
            };

            return Ok(new { success = true, data = details });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des détails de l'examen {IdBulletin}", idBulletin);
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Démarrer un examen (passer en statut "en_cours") et créer/compléter la facture
    /// </summary>
    [HttpPost("examens/{idBulletin}/demarrer")]
    public async Task<IActionResult> DemarrerExamen(int idBulletin)
    {
        try
        {
            var bulletin = await _context.BulletinsExamen
                .Include(b => b.Examen)
                .Include(b => b.Consultation).ThenInclude(c => c!.Patient).ThenInclude(p => p!.Utilisateur)
                .Include(b => b.Consultation).ThenInclude(c => c!.Patient).ThenInclude(p => p!.Assurance)
                .Include(b => b.Hospitalisation).ThenInclude(h => h!.Patient).ThenInclude(p => p!.Utilisateur)
                .Include(b => b.Hospitalisation).ThenInclude(h => h!.Patient).ThenInclude(p => p!.Assurance)
                .FirstOrDefaultAsync(b => b.IdBulletinExamen == idBulletin);

            if (bulletin == null)
            {
                return NotFound(new { success = false, message = "Examen non trouvé" });
            }

            // Valider la transition de statut
            var nouveauStatut = "en_cours";
            if (!Core.Services.StatutTransitionValidator.IsExamenTransitionValid(bulletin.Statut, nouveauStatut))
            {
                return BadRequest(new { 
                    success = false, 
                    message = Core.Services.StatutTransitionValidator.GetTransitionErrorMessage("examen", bulletin.Statut, nouveauStatut)
                });
            }

            bulletin.Statut = nouveauStatut;
            bulletin.DateRealisation = DateTime.UtcNow;

            // ==================== FACTURATION EXAMEN ====================
            var patient = bulletin.Consultation?.Patient ?? bulletin.Hospitalisation?.Patient;
            var prixExamen = bulletin.Examen?.PrixUnitaire ?? 0;
            var nomExamen = bulletin.Examen?.NomExamen ?? ExtractExamenName(bulletin.Instructions);

            if (patient != null && prixExamen > 0)
            {
                var now = DateTime.UtcNow;
                var couverture = await _assuranceService.CalculerCouvertureAsync(patient, "examen", prixExamen);

                // Chercher une facture examen en_attente existante pour ce patient (même jour)
                var aujourdhui = now.Date;
                var demain = aujourdhui.AddDays(1);
                var factureExistante = await _context.Factures
                    .Include(f => f.Lignes)
                    .FirstOrDefaultAsync(f => f.IdPatient == patient.IdUser
                        && f.TypeFacture == "examen"
                        && f.Statut == "en_attente"
                        && f.DateCreation >= aujourdhui && f.DateCreation < demain);

                if (factureExistante != null)
                {
                    // Ajouter une ligne à la facture existante
                    var ligneFacture = new LigneFacture
                    {
                        IdFacture = factureExistante.IdFacture,
                        Description = nomExamen,
                        Code = bulletin.Examen?.IdExamen.ToString(),
                        Quantite = 1,
                        PrixUnitaire = prixExamen,
                        Categorie = "examen"
                    };
                    _context.LignesFacture.Add(ligneFacture);

                    // Recalculer les totaux
                    factureExistante.MontantTotal += prixExamen;
                    if (couverture.EstAssure)
                    {
                        factureExistante.MontantAssurance = (factureExistante.MontantAssurance ?? 0) + couverture.MontantAssurance;
                    }
                    factureExistante.MontantRestant = factureExistante.MontantTotal 
                        - factureExistante.MontantPaye 
                        - (factureExistante.MontantAssurance ?? 0);
                }
                else
                {
                    // Créer une nouvelle facture examen
                    var numeroFacture = $"EXA-{now:yyyyMMdd}-{now:HHmmss}-{patient.IdUser}";
                    var facture = new Facture
                    {
                        NumeroFacture = numeroFacture,
                        IdPatient = patient.IdUser,
                        MontantTotal = prixExamen,
                        MontantPaye = 0,
                        MontantRestant = couverture.MontantPatient,
                        Statut = "en_attente",
                        TypeFacture = "examen",
                        DateCreation = now,
                        DateEcheance = now.AddDays(30),
                        CouvertureAssurance = couverture.EstAssure,
                        IdAssurance = couverture.IdAssurance,
                        TauxCouverture = couverture.EstAssure ? couverture.TauxCouverture : (decimal?)null,
                        MontantAssurance = couverture.EstAssure ? couverture.MontantAssurance : (decimal?)null,
                        Notes = $"Examens médicaux - {nomExamen}"
                    };

                    _context.Factures.Add(facture);
                    await _context.SaveChangesAsync();

                    var ligneFacture = new LigneFacture
                    {
                        IdFacture = facture.IdFacture,
                        Description = nomExamen,
                        Code = bulletin.Examen?.IdExamen.ToString(),
                        Quantite = 1,
                        PrixUnitaire = prixExamen,
                        Categorie = "examen"
                    };
                    _context.LignesFacture.Add(ligneFacture);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Examen {IdBulletin} démarré, facture générée", idBulletin);

            return Ok(new { success = true, message = "Examen démarré" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du démarrage de l'examen {IdBulletin}", idBulletin);
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Enregistrer le résultat d'un examen (texte uniquement)
    /// </summary>
    [HttpPost("examens/{idBulletin}/resultat")]
    public async Task<IActionResult> EnregistrerResultat(int idBulletin, [FromBody] EnregistrerResultatRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { success = false, message = "Non authentifié" });
            }

            var bulletin = await _context.BulletinsExamen
                .Include(b => b.Examen)
                .Include(b => b.Consultation)
                    .ThenInclude(c => c!.Patient)
                        .ThenInclude(p => p!.Utilisateur)
                .Include(b => b.Consultation)
                    .ThenInclude(c => c!.Medecin)
                        .ThenInclude(m => m!.Utilisateur)
                .Include(b => b.Hospitalisation)
                    .ThenInclude(h => h!.Patient)
                        .ThenInclude(p => p!.Utilisateur)
                .Include(b => b.Hospitalisation)
                    .ThenInclude(h => h!.Medecin)
                        .ThenInclude(m => m!.Utilisateur)
                .FirstOrDefaultAsync(b => b.IdBulletinExamen == idBulletin);

            if (bulletin == null)
            {
                return NotFound(new { success = false, message = "Examen non trouvé" });
            }

            // Valider la transition de statut
            var nouveauStatut = "termine";
            if (!Core.Services.StatutTransitionValidator.IsExamenTransitionValid(bulletin.Statut, nouveauStatut))
            {
                return BadRequest(new { 
                    success = false, 
                    message = Core.Services.StatutTransitionValidator.GetTransitionErrorMessage("examen", bulletin.Statut, nouveauStatut)
                });
            }

            bulletin.ResultatTexte = request.ResultatTexte;
            bulletin.CommentaireLabo = request.Commentaire;
            bulletin.DateResultat = DateTime.UtcNow;
            bulletin.IdBiologiste = userId.Value;
            bulletin.Statut = nouveauStatut;

            await _context.SaveChangesAsync();

            // Envoyer les notifications email au patient et au médecin
            var nomExamen = bulletin.Examen?.NomExamen ?? "Examen médical";
            _ = Task.Run(() => SendResultNotificationsAsync(bulletin, nomExamen));

            _logger.LogInformation("Résultat enregistré pour examen {IdBulletin} par laborantin {UserId}", idBulletin, userId);

            return Ok(new { success = true, message = "Résultat enregistré avec succès" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'enregistrement du résultat pour l'examen {IdBulletin}", idBulletin);
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Enregistrer le résultat avec fichiers
    /// </summary>
    [HttpPost("examens/{idBulletin}/resultat-complet")]
    public async Task<IActionResult> EnregistrerResultatComplet(
        int idBulletin,
        [FromForm] string resultatTexte,
        [FromForm] string? commentaire,
        [FromForm] List<IFormFile>? fichiers)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { success = false, message = "Non authentifié" });
            }

            var bulletin = await _context.BulletinsExamen
                .Include(b => b.Examen)
                .Include(b => b.Consultation)
                    .ThenInclude(c => c!.Patient)
                        .ThenInclude(p => p!.Utilisateur)
                .Include(b => b.Consultation)
                    .ThenInclude(c => c!.Medecin)
                        .ThenInclude(m => m!.Utilisateur)
                .Include(b => b.Hospitalisation)
                    .ThenInclude(h => h!.Patient)
                        .ThenInclude(p => p!.Utilisateur)
                .Include(b => b.Hospitalisation)
                    .ThenInclude(h => h!.Medecin)
                        .ThenInclude(m => m!.Utilisateur)
                .FirstOrDefaultAsync(b => b.IdBulletinExamen == idBulletin);

            if (bulletin == null)
            {
                return NotFound(new { success = false, message = "Examen non trouvé" });
            }

            // Valider la transition de statut
            var nouveauStatut = "termine";
            if (!Core.Services.StatutTransitionValidator.IsExamenTransitionValid(bulletin.Statut, nouveauStatut))
            {
                return BadRequest(new { 
                    success = false, 
                    message = Core.Services.StatutTransitionValidator.GetTransitionErrorMessage("examen", bulletin.Statut, nouveauStatut)
                });
            }

            // Récupérer l'ID du patient
            var idPatient = bulletin.Consultation?.IdPatient ?? bulletin.Hospitalisation?.IdPatient;
            if (!idPatient.HasValue)
            {
                return BadRequest(new { success = false, message = "Patient non trouvé pour cet examen" });
            }

            var documentsUuids = new List<string>();

            // Traiter les fichiers uploadés
            if (fichiers != null && fichiers.Any())
            {
                foreach (var fichier in fichiers)
                {
                    // Valider le fichier
                    var validation = _storageService.ValidateFile(fichier);
                    if (!validation.IsValid)
                    {
                        return BadRequest(new { success = false, message = $"Fichier invalide: {string.Join(", ", validation.Errors)}" });
                    }

                    // Générer UUID et sauvegarder avec méthode transactionnelle
                    var uuid = Guid.NewGuid().ToString();
                    var saveResult = await _storageService.SaveFileTransactionalAsync(fichier, uuid, idPatient.Value);

                    if (!saveResult.Success)
                    {
                        _logger.LogError("Erreur sauvegarde fichier: {Error}", saveResult.ErrorMessage);
                        continue;
                    }

                    // Vérifier si c'est un doublon par hash
                    if (saveResult.IsDuplicate && !string.IsNullOrEmpty(saveResult.HashSha256))
                    {
                        var existingDoc = await _context.Set<DocumentMedical>()
                            .FirstOrDefaultAsync(d => d.HashSha256 == saveResult.HashSha256 && d.Statut == "actif");
                        
                        if (existingDoc != null)
                        {
                            _logger.LogInformation(
                                "Doublon détecté: fichier {FileName} identique à {ExistingUuid}",
                                fichier.FileName, existingDoc.Uuid);
                            
                            // Réutiliser le document existant
                            documentsUuids.Add(existingDoc.Uuid);
                            continue;
                        }
                    }

                    // Créer l'entrée dans documents_medicaux
                    var document = new DocumentMedical
                    {
                        Uuid = uuid,
                        CheminRelatif = saveResult.RelativePath ?? "",
                        NomFichierOriginal = fichier.FileName,
                        NomFichierStockage = saveResult.StorageFileName ?? $"{uuid}{Path.GetExtension(fichier.FileName)}",
                        Extension = Path.GetExtension(fichier.FileName),
                        MimeType = saveResult.MimeType ?? fichier.ContentType,
                        TailleOctets = (ulong)saveResult.FileSize,
                        HashSha256 = saveResult.HashSha256,
                        HashCalculeAt = DateTime.UtcNow,
                        TypeDocument = "resultat_examen",
                        SousType = bulletin.Examen?.NomExamen ?? "Examen",
                        NiveauConfidentialite = "normal",
                        AccesPatient = true,
                        IdPatient = idPatient.Value,
                        IdBulletinExamen = idBulletin,
                        IdConsultation = bulletin.IdConsultation,
                        IdHospitalisation = bulletin.IdHospitalisation,
                        IdCreateur = userId.Value,
                        DateDocument = DateTime.UtcNow,
                        Description = $"Résultat d'examen: {bulletin.Examen?.NomExamen ?? "Examen"}",
                        Statut = "actif"
                    };

                    _context.Set<DocumentMedical>().Add(document);
                    documentsUuids.Add(uuid);
                }
            }

            // Mettre à jour le bulletin
            bulletin.ResultatTexte = resultatTexte;
            bulletin.CommentaireLabo = commentaire;
            bulletin.DateResultat = DateTime.UtcNow;
            bulletin.IdBiologiste = userId.Value;
            bulletin.Statut = nouveauStatut;

            // Lier le premier document au bulletin (pour compatibilité)
            if (documentsUuids.Any())
            {
                bulletin.ResultatFichier = documentsUuids.First();
            }

            await _context.SaveChangesAsync();

            // Envoyer les notifications email au patient et au médecin
            var nomExamen = bulletin.Examen?.NomExamen ?? "Examen médical";
            _ = Task.Run(() => SendResultNotificationsAsync(bulletin, nomExamen));

            _logger.LogInformation(
                "Résultat complet enregistré pour examen {IdBulletin} par laborantin {UserId} avec {NbFichiers} fichier(s)",
                idBulletin, userId, documentsUuids.Count);

            return Ok(new
            {
                success = true,
                message = "Résultat enregistré avec succès",
                documentsUuids = documentsUuids
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'enregistrement du résultat complet pour l'examen {IdBulletin}", idBulletin);
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer la liste des laboratoires
    /// </summary>
    [HttpGet("laboratoires")]
    public async Task<IActionResult> GetLaboratoires()
    {
        try
        {
            var laboratoires = await _context.Laboratoires
                .Where(l => l.Actif)
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

            return Ok(new { success = true, data = laboratoires });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des laboratoires");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    // Helper methods
    private static ExamenLaborantinDto MapToExamenDto(BulletinExamen b)
    {
        var patient = b.Consultation?.Patient ?? b.Hospitalisation?.Patient;
        var medecin = b.Consultation?.Medecin ?? b.Hospitalisation?.Medecin;

        return new ExamenLaborantinDto
        {
            IdBulletinExamen = b.IdBulletinExamen,
            DateDemande = b.DateDemande,
            TypeExamen = b.Examen?.Specialite?.Categorie?.Nom,
            NomExamen = b.Examen?.NomExamen ?? ExtractExamenName(b.Instructions),
            Categorie = b.Examen?.Specialite?.Categorie?.Nom,
            Specialite = b.Examen?.Specialite?.Nom,
            Instructions = b.Instructions,
            Urgence = b.Urgence,
            Statut = b.Statut ?? "prescrit",
            IdPatient = patient?.IdUser,
            PatientNom = patient?.Utilisateur?.Nom,
            PatientPrenom = patient?.Utilisateur?.Prenom,
            PatientNumeroDossier = patient?.NumeroDossier,
            PatientDateNaissance = patient?.Utilisateur?.Naissance,
            PatientSexe = patient?.Utilisateur?.Sexe,
            IdMedecin = medecin?.IdUser,
            MedecinNom = medecin?.Utilisateur?.Nom,
            MedecinPrenom = medecin?.Utilisateur?.Prenom,
            MedecinSpecialite = medecin?.Specialite?.NomSpecialite,
            IdLabo = b.IdLabo,
            NomLabo = b.Laboratoire?.NomLabo,
            DateResultat = b.DateResultat,
            ResultatTexte = b.ResultatTexte,
            HasResultat = b.ResultatTexte != null || b.ResultatFichier != null,
            DocumentResultatUuid = b.ResultatFichier
        };
    }

    private static string? ExtractExamenName(string? instructions)
    {
        if (string.IsNullOrEmpty(instructions)) return null;
        
        // Format: [Type] NomExamen - Notes
        if (instructions.StartsWith("["))
        {
            var endBracket = instructions.IndexOf(']');
            if (endBracket > 0 && endBracket < instructions.Length - 1)
            {
                var rest = instructions.Substring(endBracket + 1).Trim();
                var dashIndex = rest.IndexOf('-');
                return dashIndex > 0 ? rest.Substring(0, dashIndex).Trim() : rest;
            }
        }
        return instructions.Length > 50 ? instructions.Substring(0, 50) + "..." : instructions;
    }

    /// <summary>
    /// Envoie les notifications email et in-app au patient et au médecin prescripteur
    /// </summary>
    private async Task SendResultNotificationsAsync(BulletinExamen bulletin, string nomExamen)
    {
        try
        {
            // Récupérer les informations du patient et du médecin
            var patient = bulletin.Consultation?.Patient ?? bulletin.Hospitalisation?.Patient;
            var medecin = bulletin.Consultation?.Medecin ?? bulletin.Hospitalisation?.Medecin;

            // Notification au patient
            if (patient?.Utilisateur != null)
            {
                var patientName = $"{patient.Utilisateur.Prenom} {patient.Utilisateur.Nom}";
                
                // Notification in-app
                try
                {
                    await _notificationService.CreateAsync(new CreateNotificationRequest
                    {
                        IdUser = patient.IdUser,
                        Type = "resultat_examen",
                        Titre = "Résultats d'examen disponibles",
                        Message = $"Les résultats de votre examen \"{nomExamen}\" sont maintenant disponibles.",
                        Lien = $"/patient/examens/{bulletin.IdBulletinExamen}",
                        Icone = "flask",
                        Priorite = "haute",
                        SendRealTime = true
                    });
                }
                catch (Exception notifEx)
                {
                    _logger.LogWarning("Erreur notification in-app patient: {Error}", notifEx.Message);
                }

                // Notification email
                if (!string.IsNullOrEmpty(patient.Utilisateur.Email))
                {
                    var patientSubject = "Résultats d'examen disponibles - MediConnect";
                    var patientHtml = GetPatientResultNotificationTemplate(patientName, nomExamen);

                    var patientEmailSent = await _emailService.SendEmailAsync(
                        patient.Utilisateur.Email,
                        patientSubject,
                        patientHtml
                    );

                    if (patientEmailSent)
                    {
                        _logger.LogInformation("Notification email envoyée au patient {PatientId} pour examen {IdBulletin}",
                            patient.IdUser, bulletin.IdBulletinExamen);
                    }
                    else
                    {
                        _logger.LogWarning("Échec envoi notification email au patient {PatientId}", patient.IdUser);
                    }
                }
            }

            // Notification au médecin prescripteur
            if (medecin?.Utilisateur != null)
            {
                var medecinName = $"{medecin.Utilisateur.Prenom} {medecin.Utilisateur.Nom}";
                var patientName = patient?.Utilisateur != null 
                    ? $"{patient.Utilisateur.Prenom} {patient.Utilisateur.Nom}" 
                    : "Patient";

                // Notification in-app
                try
                {
                    await _notificationService.CreateAsync(new CreateNotificationRequest
                    {
                        IdUser = medecin.IdUser,
                        Type = "resultat_examen",
                        Titre = "Résultats d'examen disponibles",
                        Message = $"Les résultats de l'examen \"{nomExamen}\" pour {patientName} sont disponibles.",
                        Lien = $"/medecin/examens/{bulletin.IdBulletinExamen}",
                        Icone = "flask",
                        Priorite = "normale",
                        SendRealTime = true
                    });
                }
                catch (Exception notifEx)
                {
                    _logger.LogWarning("Erreur notification in-app médecin: {Error}", notifEx.Message);
                }

                // Notification email
                if (!string.IsNullOrEmpty(medecin.Utilisateur.Email))
                {
                    var medecinSubject = "Résultats d'examen disponibles pour votre patient - MediConnect";
                    var medecinHtml = GetMedecinResultNotificationTemplate(medecinName, patientName, nomExamen);

                    var medecinEmailSent = await _emailService.SendEmailAsync(
                        medecin.Utilisateur.Email,
                        medecinSubject,
                        medecinHtml
                    );

                    if (medecinEmailSent)
                    {
                        _logger.LogInformation("Notification email envoyée au médecin {MedecinId} pour examen {IdBulletin}",
                            medecin.IdUser, bulletin.IdBulletinExamen);
                    }
                    else
                    {
                        _logger.LogWarning("Échec envoi notification email au médecin {MedecinId}", medecin.IdUser);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'envoi des notifications pour l'examen {IdBulletin}", bulletin.IdBulletinExamen);
        }
    }

    private static string GetPatientResultNotificationTemplate(string patientName, string nomExamen)
    {
        return $@"
<!DOCTYPE html>
<html lang=""fr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7fa;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #0e7490 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 12px 12px 0 0;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px;"">🏥 MediConnect</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #1f2937; margin: 0 0 20px;"">Résultats d'examen disponibles</h2>
                            <p style=""color: #4b5563; font-size: 16px; line-height: 1.6;"">
                                Bonjour {patientName},<br><br>
                                Les résultats de votre examen médical <strong>{nomExamen}</strong> sont désormais disponibles sur MediConnect.<br><br>
                                Vous pouvez les consulter depuis votre espace patient.
                            </p>
                            <div style=""text-align: center; margin: 35px 0;"">
                                <a href=""http://localhost:4200/patient/dashboard"" 
                                   style=""background: linear-gradient(135deg, #0e7490 0%, #0891b2 100%); 
                                          color: #ffffff; 
                                          text-decoration: none; 
                                          padding: 16px 40px; 
                                          border-radius: 8px; 
                                          font-size: 16px; 
                                          font-weight: 600;
                                          display: inline-block;"">
                                    📋 Consulter mes résultats
                                </a>
                            </div>
                            <p style=""color: #6b7280; font-size: 14px; line-height: 1.5; margin-top: 30px;"">
                                Cordialement,<br>
                                L'équipe MediConnect
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f9fafb; padding: 25px 30px; text-align: center; border-radius: 0 0 12px 12px;"">
                            <p style=""color: #9ca3af; font-size: 13px; margin: 0;"">© {DateTime.Now.Year} MediConnect</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private static string GetMedecinResultNotificationTemplate(string medecinName, string patientName, string nomExamen)
    {
        return $@"
<!DOCTYPE html>
<html lang=""fr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7fa;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #0e7490 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 12px 12px 0 0;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px;"">🏥 MediConnect</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #1f2937; margin: 0 0 20px;"">Résultats d'examen disponibles</h2>
                            <p style=""color: #4b5563; font-size: 16px; line-height: 1.6;"">
                                Bonjour Dr {medecinName},<br><br>
                                Les résultats de l'examen <strong>{nomExamen}</strong> du patient <strong>{patientName}</strong> sont maintenant disponibles.<br><br>
                                Connectez-vous à MediConnect pour les consulter.
                            </p>
                            <div style=""text-align: center; margin: 35px 0;"">
                                <a href=""http://localhost:4200/medecin/dashboard"" 
                                   style=""background: linear-gradient(135deg, #0e7490 0%, #0891b2 100%); 
                                          color: #ffffff; 
                                          text-decoration: none; 
                                          padding: 16px 40px; 
                                          border-radius: 8px; 
                                          font-size: 16px; 
                                          font-weight: 600;
                                          display: inline-block;"">
                                    📋 Consulter les résultats
                                </a>
                            </div>
                            <p style=""color: #6b7280; font-size: 14px; line-height: 1.5; margin-top: 30px;"">
                                Cordialement,<br>
                                L'équipe MediConnect
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f9fafb; padding: 25px 30px; text-align: center; border-radius: 0 0 12px 12px;"">
                            <p style=""color: #9ca3af; font-size: 13px; margin: 0;"">© {DateTime.Now.Year} MediConnect</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }
}
