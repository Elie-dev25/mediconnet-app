using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Core.Entities.Documents;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Documents;
using System.Security.Claims;
using System.Text.Json;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// Controller pour la gestion des documents médicaux
/// Upload, téléchargement, vérification d'intégrité
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IDocumentStorageService _storageService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        ApplicationDbContext context,
        IDocumentStorageService storageService,
        ILogger<DocumentsController> logger)
    {
        _context = context;
        _storageService = storageService;
        _logger = logger;
    }

    /// <summary>
    /// Upload d'un document médical
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(52_428_800)] // 50 Mo
    public async Task<IActionResult> UploadDocument([FromForm] IFormFile file, [FromForm] UploadDocumentRequest request)
    {
        try
        {
            // Valider le fichier
            var validation = _storageService.ValidateFile(file);
            if (!validation.IsValid)
            {
                return BadRequest(new { errors = validation.Errors });
            }

            // Générer l'UUID
            var uuid = Guid.NewGuid().ToString();

            // Obtenir l'ID de l'utilisateur connecté
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "Utilisateur non authentifié" });
            }

            // Sauvegarder le fichier physiquement
            var uploadResult = await _storageService.SaveFileAsync(file, uuid, request.IdPatient);
            if (!uploadResult.Success)
            {
                return StatusCode(500, new { message = uploadResult.ErrorMessage });
            }

            // Créer l'entrée en base de données
            var document = new DocumentMedical
            {
                Uuid = uuid,
                NomFichierOriginal = file.FileName,
                NomFichierStockage = uploadResult.StorageFileName!,
                CheminRelatif = uploadResult.RelativePath!,
                Extension = uploadResult.Extension,
                MimeType = uploadResult.MimeType!,
                TailleOctets = (ulong)uploadResult.FileSize,
                HashSha256 = uploadResult.HashSha256,
                HashCalculeAt = DateTime.UtcNow,
                TypeDocument = request.TypeDocument,
                SousType = request.SousType,
                NiveauConfidentialite = request.NiveauConfidentialite,
                AccesPatient = request.AccesPatient,
                IdPatient = request.IdPatient,
                IdConsultation = request.IdConsultation,
                IdBulletinExamen = request.IdBulletinExamen,
                IdHospitalisation = request.IdHospitalisation,
                IdDmp = request.IdDmp,
                IdCreateur = userId.Value,
                DateDocument = request.DateDocument ?? DateTime.UtcNow,
                Description = request.Description,
                Tags = request.Tags != null ? JsonSerializer.Serialize(request.Tags) : null,
                Statut = "actif",
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<DocumentMedical>().Add(document);
            await _context.SaveChangesAsync();

            // Enregistrer l'audit
            await LogAuditAsync(uuid, userId.Value, "creation", true);

            _logger.LogInformation(
                "Document uploadé: UUID={Uuid}, Patient={PatientId}, Createur={CreateurId}",
                uuid, request.IdPatient, userId);

            return Ok(new DocumentMedicalDto
            {
                Uuid = document.Uuid,
                NomFichierOriginal = document.NomFichierOriginal,
                Extension = document.Extension ?? "",
                MimeType = document.MimeType,
                TailleOctets = (long)document.TailleOctets,
                TypeDocument = document.TypeDocument,
                SousType = document.SousType,
                NiveauConfidentialite = document.NiveauConfidentialite,
                AccesPatient = document.AccesPatient,
                IdPatient = document.IdPatient,
                IdConsultation = document.IdConsultation,
                IdBulletinExamen = document.IdBulletinExamen,
                IdHospitalisation = document.IdHospitalisation,
                IdDmp = document.IdDmp,
                IdCreateur = document.IdCreateur,
                DateDocument = document.DateDocument,
                Description = document.Description,
                Statut = document.Statut,
                CreatedAt = document.CreatedAt,
                HashPresent = !string.IsNullOrEmpty(document.HashSha256)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'upload du document");
            return StatusCode(500, new { message = "Erreur lors de l'upload du document" });
        }
    }

    /// <summary>
    /// Télécharger un document par UUID
    /// </summary>
    [HttpGet("{uuid}/download")]
    public async Task<IActionResult> DownloadDocument(string uuid)
    {
        try
        {
            var document = await _context.Set<DocumentMedical>()
                .FirstOrDefaultAsync(d => d.Uuid == uuid && d.Statut == "actif");

            if (document == null)
            {
                return NotFound(new { message = "Document non trouvé" });
            }

            // Vérifier l'accès
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            
            if (!await CanAccessDocumentAsync(document, userId, userRole))
            {
                await LogAuditAsync(uuid, userId ?? 0, "telechargement", false, "Accès non autorisé");
                return Forbid();
            }

            // Vérifier que le fichier existe
            if (!_storageService.FileExists(document.CheminRelatif))
            {
                _logger.LogWarning("Fichier physique non trouvé: {Path}", document.CheminRelatif);
                return NotFound(new { message = "Fichier physique non trouvé" });
            }

            // Lire le fichier
            var stream = await _storageService.OpenReadStreamAsync(document.CheminRelatif);

            // Enregistrer l'audit
            await LogAuditAsync(uuid, userId ?? 0, "telechargement", true);

            return File(stream, document.MimeType, document.NomFichierOriginal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du téléchargement du document {Uuid}", uuid);
            return StatusCode(500, new { message = "Erreur lors du téléchargement" });
        }
    }

    /// <summary>
    /// Obtenir les métadonnées d'un document
    /// </summary>
    [HttpGet("{uuid}")]
    public async Task<IActionResult> GetDocument(string uuid)
    {
        try
        {
            var document = await _context.Set<DocumentMedical>()
                .Include(d => d.Patient)
                    .ThenInclude(p => p!.Utilisateur)
                .Include(d => d.Createur)
                .FirstOrDefaultAsync(d => d.Uuid == uuid);

            if (document == null)
            {
                return NotFound(new { message = "Document non trouvé" });
            }

            // Vérifier l'accès
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            
            if (!await CanAccessDocumentAsync(document, userId, userRole))
            {
                await LogAuditAsync(uuid, userId ?? 0, "consultation", false, "Accès non autorisé");
                return Forbid();
            }

            await LogAuditAsync(uuid, userId ?? 0, "consultation", true);

            return Ok(new DocumentMedicalDto
            {
                Uuid = document.Uuid,
                NomFichierOriginal = document.NomFichierOriginal,
                Extension = document.Extension ?? "",
                MimeType = document.MimeType,
                TailleOctets = (long)document.TailleOctets,
                TypeDocument = document.TypeDocument,
                SousType = document.SousType,
                NiveauConfidentialite = document.NiveauConfidentialite,
                AccesPatient = document.AccesPatient,
                IdPatient = document.IdPatient,
                PatientNom = document.Patient?.Utilisateur != null ? $"{document.Patient.Utilisateur.Prenom} {document.Patient.Utilisateur.Nom}" : null,
                IdConsultation = document.IdConsultation,
                IdBulletinExamen = document.IdBulletinExamen,
                IdHospitalisation = document.IdHospitalisation,
                IdDmp = document.IdDmp,
                IdCreateur = document.IdCreateur,
                CreateurNom = document.Createur != null ? $"{document.Createur.Prenom} {document.Createur.Nom}" : null,
                DateDocument = document.DateDocument,
                Description = document.Description,
                Statut = document.Statut,
                CreatedAt = document.CreatedAt,
                HashPresent = !string.IsNullOrEmpty(document.HashSha256)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération du document {Uuid}", uuid);
            return StatusCode(500, new { message = "Erreur lors de la récupération" });
        }
    }

    /// <summary>
    /// Lister les documents d'un patient
    /// </summary>
    [HttpGet("patient/{idPatient}")]
    public async Task<IActionResult> GetPatientDocuments(
        int idPatient,
        [FromQuery] string? typeDocument = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = _context.Set<DocumentMedical>()
                .Where(d => d.IdPatient == idPatient && d.Statut == "actif" && d.EstVersionCourante);

            if (!string.IsNullOrEmpty(typeDocument))
            {
                query = query.Where(d => d.TypeDocument == typeDocument);
            }

            var totalCount = await query.CountAsync();

            var documents = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DocumentMedicalDto
                {
                    Uuid = d.Uuid,
                    NomFichierOriginal = d.NomFichierOriginal,
                    Extension = d.Extension ?? "",
                    MimeType = d.MimeType,
                    TailleOctets = (long)d.TailleOctets,
                    TypeDocument = d.TypeDocument,
                    SousType = d.SousType,
                    NiveauConfidentialite = d.NiveauConfidentialite,
                    AccesPatient = d.AccesPatient,
                    IdPatient = d.IdPatient,
                    IdConsultation = d.IdConsultation,
                    IdBulletinExamen = d.IdBulletinExamen,
                    IdCreateur = d.IdCreateur,
                    DateDocument = d.DateDocument,
                    Description = d.Description,
                    Statut = d.Statut,
                    CreatedAt = d.CreatedAt,
                    HashPresent = d.HashSha256 != null
                })
                .ToListAsync();

            return Ok(new DocumentListResponse
            {
                Documents = documents,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des documents du patient {PatientId}", idPatient);
            return StatusCode(500, new { message = "Erreur lors de la récupération" });
        }
    }

    /// <summary>
    /// Vérifier l'intégrité d'un document
    /// </summary>
    [HttpPost("{uuid}/verify-integrity")]
    public async Task<IActionResult> VerifyIntegrity(string uuid)
    {
        try
        {
            var document = await _context.Set<DocumentMedical>()
                .FirstOrDefaultAsync(d => d.Uuid == uuid);

            if (document == null)
            {
                return NotFound(new { message = "Document non trouvé" });
            }

            var result = await _storageService.VerifyIntegrityAsync(
                document.CheminRelatif,
                document.HashSha256,
                document.TailleOctets);

            // Enregistrer la vérification
            var verification = new VerificationIntegrite
            {
                DocumentUuid = uuid,
                StatutVerification = result.Status,
                HashAttendu = result.ExpectedHash,
                HashCalcule = result.CalculatedHash,
                TailleAttendue = result.ExpectedSize,
                TailleReelle = result.ActualSize,
                TypeVerification = "manuelle",
                IdDeclencheur = GetCurrentUserId(),
                Timestamp = DateTime.UtcNow
            };

            _context.Set<VerificationIntegrite>().Add(verification);

            // Si le hash n'était pas présent, le mettre à jour
            if (result.IsValid && string.IsNullOrEmpty(document.HashSha256) && !string.IsNullOrEmpty(result.CalculatedHash))
            {
                document.HashSha256 = result.CalculatedHash;
                document.HashCalculeAt = DateTime.UtcNow;
            }

            // Si problème, mettre en quarantaine
            if (!result.IsValid && result.Status != "hash_non_calcule")
            {
                document.Statut = "quarantaine";
            }

            await _context.SaveChangesAsync();

            await LogAuditAsync(uuid, GetCurrentUserId() ?? 0, "verification", true);

            return Ok(new IntegrityVerificationDto
            {
                DocumentUuid = uuid,
                Statut = result.Status,
                HashAttendu = result.ExpectedHash,
                HashCalcule = result.CalculatedHash,
                TailleAttendue = result.ExpectedSize,
                TailleReelle = result.ActualSize,
                Timestamp = DateTime.UtcNow,
                Message = result.IsValid ? "Intégrité vérifiée" : result.ErrorMessage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la vérification d'intégrité du document {Uuid}", uuid);
            return StatusCode(500, new { message = "Erreur lors de la vérification" });
        }
    }

    /// <summary>
    /// Supprimer un document (soft delete)
    /// </summary>
    [HttpDelete("{uuid}")]
    public async Task<IActionResult> DeleteDocument(string uuid, [FromQuery] string? motif = null)
    {
        try
        {
            var document = await _context.Set<DocumentMedical>()
                .FirstOrDefaultAsync(d => d.Uuid == uuid && d.Statut == "actif");

            if (document == null)
            {
                return NotFound(new { message = "Document non trouvé" });
            }

            // Soft delete en base
            document.Statut = "supprime";
            document.DateArchivage = DateTime.UtcNow;
            document.MotifArchivage = motif ?? "Suppression manuelle";
            document.UpdatedAt = DateTime.UtcNow;

            // Déplacer le fichier vers quarantaine
            await _storageService.DeleteFileAsync(document.CheminRelatif);

            await _context.SaveChangesAsync();

            await LogAuditAsync(uuid, GetCurrentUserId() ?? 0, "suppression", true);

            _logger.LogInformation("Document supprimé: UUID={Uuid}", uuid);

            return Ok(new { message = "Document supprimé avec succès" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la suppression du document {Uuid}", uuid);
            return StatusCode(500, new { message = "Erreur lors de la suppression" });
        }
    }

    /// <summary>
    /// Obtenir les statistiques des documents
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Roles = "administrateur,medecin")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var stats = await _context.Set<DocumentMedical>()
                .Where(d => d.EstVersionCourante)
                .GroupBy(d => d.TypeDocument)
                .Select(g => new DocumentStatsDto
                {
                    TypeDocument = g.Key,
                    NombreDocuments = g.Count(),
                    TailleTotaleOctets = (long)g.Sum(d => (decimal)d.TailleOctets),
                    AvecHash = g.Count(d => d.HashSha256 != null),
                    SansHash = g.Count(d => d.HashSha256 == null),
                    Actifs = g.Count(d => d.Statut == "actif"),
                    Archives = g.Count(d => d.Statut == "archive"),
                    EnQuarantaine = g.Count(d => d.Statut == "quarantaine")
                })
                .ToListAsync();

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des statistiques");
            return StatusCode(500, new { message = "Erreur lors de la récupération des statistiques" });
        }
    }

    /// <summary>
    /// Initialiser la structure de stockage (admin only)
    /// </summary>
    [HttpPost("initialize-storage")]
    [Authorize(Roles = "administrateur")]
    public async Task<IActionResult> InitializeStorage()
    {
        try
        {
            await _storageService.InitializeStorageAsync();
            return Ok(new { message = "Structure de stockage initialisée avec succès" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'initialisation du stockage");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Valider l'intégrité de plusieurs documents (batch)
    /// </summary>
    [HttpPost("verify-batch")]
    [Authorize(Roles = "administrateur")]
    public async Task<IActionResult> VerifyBatch([FromQuery] int limit = 10)
    {
        try
        {
            var results = new List<object>();
            var documents = await _context.Set<DocumentMedical>()
                .Where(d => d.Statut == "actif")
                .OrderBy(d => Guid.NewGuid()) // Random
                .Take(limit)
                .ToListAsync();

            foreach (var document in documents)
            {
                var result = await _storageService.VerifyIntegrityAsync(
                    document.CheminRelatif,
                    document.HashSha256,
                    document.TailleOctets);

                results.Add(new
                {
                    uuid = document.Uuid,
                    nomFichier = document.NomFichierOriginal,
                    statut = result.Status,
                    isValid = result.IsValid,
                    hashPresent = !string.IsNullOrEmpty(document.HashSha256),
                    fichierPresent = _storageService.FileExists(document.CheminRelatif),
                    message = result.ErrorMessage
                });

                // Enregistrer la vérification
                var verification = new VerificationIntegrite
                {
                    DocumentUuid = document.Uuid,
                    StatutVerification = result.Status,
                    HashAttendu = result.ExpectedHash,
                    HashCalcule = result.CalculatedHash,
                    TailleAttendue = result.ExpectedSize,
                    TailleReelle = result.ActualSize,
                    TypeVerification = "automatique",
                    IdDeclencheur = GetCurrentUserId(),
                    Timestamp = DateTime.UtcNow
                };
                _context.Set<VerificationIntegrite>().Add(verification);
            }

            await _context.SaveChangesAsync();

            var summary = new
            {
                total = results.Count,
                valides = results.Count(r => ((dynamic)r).isValid),
                invalides = results.Count(r => !((dynamic)r).isValid),
                fichierManquant = results.Count(r => !((dynamic)r).fichierPresent),
                details = results
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la vérification batch");
            return StatusCode(500, new { message = "Erreur lors de la vérification batch" });
        }
    }

    /// <summary>
    /// Recalculer les hash SHA-256 manquants
    /// </summary>
    [HttpPost("recalculate-hashes")]
    [Authorize(Roles = "administrateur")]
    public async Task<IActionResult> RecalculateHashes([FromQuery] int limit = 50)
    {
        try
        {
            var documents = await _context.Set<DocumentMedical>()
                .Where(d => d.HashSha256 == null && d.Statut == "actif")
                .Take(limit)
                .ToListAsync();

            var results = new List<object>();
            var updated = 0;
            var errors = 0;

            foreach (var document in documents)
            {
                try
                {
                    if (!_storageService.FileExists(document.CheminRelatif))
                    {
                        results.Add(new { uuid = document.Uuid, status = "fichier_absent" });
                        errors++;
                        continue;
                    }

                    var hash = await _storageService.CalculateHashAsync(document.CheminRelatif);
                    document.HashSha256 = hash;
                    document.HashCalculeAt = DateTime.UtcNow;
                    updated++;

                    results.Add(new { uuid = document.Uuid, status = "ok", hash });
                }
                catch (Exception ex)
                {
                    results.Add(new { uuid = document.Uuid, status = "erreur", message = ex.Message });
                    errors++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Hash recalculés pour {updated} documents",
                total = documents.Count,
                updated,
                errors,
                details = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du recalcul des hash");
            return StatusCode(500, new { message = "Erreur lors du recalcul des hash" });
        }
    }

    /// <summary>
    /// Vérifier l'espace disque disponible
    /// </summary>
    [HttpGet("disk-space")]
    [Authorize(Roles = "administrateur")]
    public IActionResult GetDiskSpace()
    {
        try
        {
            var hasSufficientSpace = _storageService.HasSufficientDiskSpace(0);
            return Ok(new
            {
                hasSufficientSpace,
                message = hasSufficientSpace ? "Espace disque suffisant" : "Espace disque insuffisant"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la vérification de l'espace disque");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    #region Private Methods

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string? GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }

    private async Task<bool> CanAccessDocumentAsync(DocumentMedical document, int? userId, string? userRole)
    {
        if (userRole == "administrateur")
            return true;

        if (userRole == "patient")
            return document.AccesPatient && document.IdPatient == userId;

        if (userRole is "medecin" or "infirmier" or "laborantin" or "pharmacien")
            return true;

        return false;
    }

    private async Task LogAuditAsync(string documentUuid, int userId, string action, bool autorise, string? motifRefus = null)
    {
        try
        {
            var audit = new AuditAccesDocument
            {
                DocumentUuid = documentUuid,
                IdUtilisateur = userId,
                RoleUtilisateur = GetCurrentUserRole() ?? "unknown",
                TypeAction = action,
                Autorise = autorise,
                MotifRefus = motifRefus,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString(),
                EndpointApi = $"{Request.Method} {Request.Path}",
                Timestamp = DateTime.UtcNow
            };

            _context.Set<AuditAccesDocument>().Add(audit);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'enregistrement de l'audit");
        }
    }

    #endregion
}
