using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Entities.Documents;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.DTOs.Laborantin;
using System.Security.Claims;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// Contrôleur pour la consultation des résultats d'examens
/// Accessible par les médecins et les patients concernés
/// </summary>
[ApiController]
[Route("api/examens/resultats")]
[Authorize]
public class ExamenResultatsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IDocumentStorageService _storageService;
    private readonly ILogger<ExamenResultatsController> _logger;

    public ExamenResultatsController(
        ApplicationDbContext context,
        IDocumentStorageService storageService,
        ILogger<ExamenResultatsController> logger)
    {
        _context = context;
        _storageService = storageService;
        _logger = logger;
    }

    /// <summary>
    /// Récupérer les détails d'un résultat d'examen avec ses documents
    /// </summary>
    [HttpGet("{idBulletin}")]
    public async Task<IActionResult> GetResultatExamen(int idBulletin)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            if (!userId.HasValue)
            {
                return Unauthorized(new { success = false, message = "Non authentifié" });
            }

            var bulletin = await _context.BulletinsExamen
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
                .Include(b => b.Hospitalisation)
                    .ThenInclude(h => h!.Medecin)
                        .ThenInclude(m => m!.Utilisateur)
                .FirstOrDefaultAsync(b => b.IdBulletinExamen == idBulletin);

            if (bulletin == null)
            {
                return NotFound(new { success = false, message = "Examen non trouvé" });
            }

            // Vérifier l'accès
            var patient = bulletin.Consultation?.Patient ?? bulletin.Hospitalisation?.Patient;
            var medecin = bulletin.Consultation?.Medecin ?? bulletin.Hospitalisation?.Medecin;

            if (!CanAccessExamen(userId.Value, userRole, patient?.IdUser, medecin?.IdUser))
            {
                _logger.LogWarning("Accès refusé à l'examen {IdBulletin} pour utilisateur {UserId} ({Role})",
                    idBulletin, userId, userRole);
                return Forbid();
            }

            // Récupérer les documents associés - charger les entités complètes pour éviter le problème de conversion Guid
            var documentsEntities = await _context.Set<DocumentMedical>()
                .Where(d => d.IdBulletinExamen == idBulletin && d.Statut == "actif")
                .AsNoTracking()
                .ToListAsync();

            var documents = documentsEntities.Select(d => new DocumentResultatDto
            {
                Uuid = d.Uuid,
                NomFichier = d.NomFichierOriginal ?? "",
                MimeType = d.MimeType,
                TailleOctets = (long)d.TailleOctets,
                DateUpload = d.DateDocument?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                Description = d.Description
            }).ToList();

            var result = new ResultatExamenDetailDto
            {
                IdBulletinExamen = bulletin.IdBulletinExamen,
                DateDemande = bulletin.DateDemande,
                DateResultat = bulletin.DateResultat,
                Statut = bulletin.Statut ?? "prescrit",
                Urgence = bulletin.Urgence,
                NomExamen = bulletin.Examen?.NomExamen ?? "Examen",
                Categorie = bulletin.Examen?.Specialite?.Categorie?.Nom,
                Specialite = bulletin.Examen?.Specialite?.Nom,
                Description = bulletin.Examen?.Description,
                Instructions = bulletin.Instructions,
                ResultatTexte = bulletin.ResultatTexte,
                CommentaireLabo = bulletin.CommentaireLabo,
                Laboratoire = bulletin.Laboratoire != null ? new LaboratoireInfoDto
                {
                    IdLabo = bulletin.Laboratoire.IdLabo,
                    NomLabo = bulletin.Laboratoire.NomLabo,
                    Telephone = bulletin.Laboratoire.Telephone
                } : null,
                Patient = patient?.Utilisateur != null ? new PersonneInfoDto
                {
                    IdUser = patient.IdUser,
                    Nom = patient.Utilisateur.Nom,
                    Prenom = patient.Utilisateur.Prenom,
                    DateNaissance = patient.Utilisateur.Naissance
                } : null,
                Medecin = medecin?.Utilisateur != null ? new PersonneInfoDto
                {
                    IdUser = medecin.IdUser,
                    Nom = medecin.Utilisateur.Nom,
                    Prenom = medecin.Utilisateur.Prenom
                } : null,
                Documents = documents
            };

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération du résultat d'examen {IdBulletin}", idBulletin);
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Télécharger un document de résultat d'examen
    /// </summary>
    [HttpGet("{idBulletin}/documents/{uuid}/download")]
    public async Task<IActionResult> DownloadDocumentResultat(int idBulletin, string uuid)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            if (!userId.HasValue)
            {
                return Unauthorized(new { success = false, message = "Non authentifié" });
            }

            // Vérifier que le document appartient bien à cet examen
            var document = await _context.Set<DocumentMedical>()
                .FirstOrDefaultAsync(d => d.Uuid == uuid && d.IdBulletinExamen == idBulletin && d.Statut == "actif");

            if (document == null)
            {
                return NotFound(new { success = false, message = "Document non trouvé" });
            }

            // Récupérer le bulletin pour vérifier l'accès
            var bulletin = await _context.BulletinsExamen
                .Include(b => b.Consultation)
                    .ThenInclude(c => c!.Patient)
                .Include(b => b.Hospitalisation)
                    .ThenInclude(h => h!.Patient)
                .Include(b => b.Consultation)
                    .ThenInclude(c => c!.Medecin)
                .Include(b => b.Hospitalisation)
                    .ThenInclude(h => h!.Medecin)
                .FirstOrDefaultAsync(b => b.IdBulletinExamen == idBulletin);

            if (bulletin == null)
            {
                return NotFound(new { success = false, message = "Examen non trouvé" });
            }

            var patient = bulletin.Consultation?.Patient ?? bulletin.Hospitalisation?.Patient;
            var medecin = bulletin.Consultation?.Medecin ?? bulletin.Hospitalisation?.Medecin;

            if (!CanAccessExamen(userId.Value, userRole, patient?.IdUser, medecin?.IdUser))
            {
                _logger.LogWarning("Accès refusé au document {Uuid} de l'examen {IdBulletin} pour utilisateur {UserId}",
                    uuid, idBulletin, userId);
                return Forbid();
            }

            // Vérifier que le fichier existe
            if (!_storageService.FileExists(document.CheminRelatif))
            {
                _logger.LogWarning("Fichier physique non trouvé: {Path}", document.CheminRelatif);
                return NotFound(new { success = false, message = "Fichier non trouvé" });
            }

            // Lire le fichier
            var stream = await _storageService.OpenReadStreamAsync(document.CheminRelatif);

            _logger.LogInformation("Document {Uuid} téléchargé par utilisateur {UserId} ({Role})",
                uuid, userId, userRole);

            return File(stream, document.MimeType, document.NomFichierOriginal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du téléchargement du document {Uuid}", uuid);
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer la liste des examens terminés pour le patient connecté
    /// </summary>
    [HttpGet("patient/mes-resultats")]
    [Authorize(Roles = "patient")]
    public async Task<IActionResult> GetMesResultats([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { success = false, message = "Non authentifié" });
            }

            var query = _context.BulletinsExamen
                .Include(b => b.Examen)
                    .ThenInclude(e => e!.Specialite)
                .Include(b => b.Laboratoire)
                .Include(b => b.Consultation)
                .Include(b => b.Hospitalisation)
                .Where(b => b.Statut == "termine" &&
                    ((b.Consultation != null && b.Consultation.IdPatient == userId) ||
                     (b.Hospitalisation != null && b.Hospitalisation.IdPatient == userId)))
                .OrderByDescending(b => b.DateResultat);

            var totalCount = await query.CountAsync();
            var examens = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new ResultatExamenListDto
                {
                    IdBulletinExamen = b.IdBulletinExamen,
                    DateDemande = b.DateDemande,
                    DateResultat = b.DateResultat,
                    NomExamen = b.Examen != null ? b.Examen.NomExamen : "Examen",
                    Specialite = b.Examen != null && b.Examen.Specialite != null ? b.Examen.Specialite.Nom : null,
                    NomLabo = b.Laboratoire != null ? b.Laboratoire.NomLabo : null,
                    HasDocuments = _context.Set<DocumentMedical>().Any(d => d.IdBulletinExamen == b.IdBulletinExamen && d.Statut == "actif")
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    examens,
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des résultats du patient");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupérer la liste des examens terminés pour un patient (médecin)
    /// </summary>
    [HttpGet("medecin/patient/{idPatient}")]
    [Authorize(Roles = "medecin,administrateur")]
    public async Task<IActionResult> GetResultatsPatient(int idPatient, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = _context.BulletinsExamen
                .Include(b => b.Examen)
                    .ThenInclude(e => e!.Specialite)
                .Include(b => b.Laboratoire)
                .Include(b => b.Consultation)
                .Include(b => b.Hospitalisation)
                .Where(b => b.Statut == "termine" &&
                    ((b.Consultation != null && b.Consultation.IdPatient == idPatient) ||
                     (b.Hospitalisation != null && b.Hospitalisation.IdPatient == idPatient)))
                .OrderByDescending(b => b.DateResultat);

            var totalCount = await query.CountAsync();
            var examens = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new ResultatExamenListDto
                {
                    IdBulletinExamen = b.IdBulletinExamen,
                    DateDemande = b.DateDemande,
                    DateResultat = b.DateResultat,
                    NomExamen = b.Examen != null ? b.Examen.NomExamen : "Examen",
                    Specialite = b.Examen != null && b.Examen.Specialite != null ? b.Examen.Specialite.Nom : null,
                    NomLabo = b.Laboratoire != null ? b.Laboratoire.NomLabo : null,
                    HasDocuments = _context.Set<DocumentMedical>().Any(d => d.IdBulletinExamen == b.IdBulletinExamen && d.Statut == "actif")
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    examens,
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des résultats du patient {IdPatient}", idPatient);
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    #region Helpers

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string? GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }

    private bool CanAccessExamen(int userId, string? userRole, int? patientId, int? medecinId)
    {
        // Administrateur a accès à tout
        if (userRole == "administrateur")
            return true;

        // Patient peut accéder à ses propres examens
        if (userRole == "patient" && patientId.HasValue && patientId.Value == userId)
            return true;

        // Médecin peut accéder aux examens de ses patients
        if (userRole == "medecin")
            return true; // Pour simplifier, on autorise tous les médecins

        // Laborantin peut accéder aux examens
        if (userRole == "laborantin")
            return true;

        return false;
    }

    #endregion
}
