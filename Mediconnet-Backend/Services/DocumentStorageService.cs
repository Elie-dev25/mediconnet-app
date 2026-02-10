using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mediconnet_Backend.Core.Configuration;
using Mediconnet_Backend.Core.Interfaces.Services;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service de stockage des documents médicaux
/// Structure: /storage/mediConnect/{annee}/{mois}/patient_{id_patient}/{uuid_part1}/{uuid_part2}/{uuid}.{ext}
/// 
/// IMPORTANT: Ce service est la SEULE source de vérité pour la génération des chemins de stockage.
/// Ne jamais créer de chemins manuellement ailleurs dans le code.
/// </summary>
public class DocumentStorageService : IDocumentStorageService
{
    private readonly DocumentStorageSettings _settings;
    private readonly ILogger<DocumentStorageService> _logger;
    
    private static readonly Dictionary<string, string> MimeToExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        { "application/pdf", ".pdf" },
        { "image/jpeg", ".jpg" },
        { "image/png", ".png" },
        { "image/gif", ".gif" },
        { "image/bmp", ".bmp" },
        { "image/webp", ".webp" },
        { "image/tiff", ".tiff" },
        { "application/dicom", ".dcm" },
        { "text/plain", ".txt" },
        { "application/xml", ".xml" },
        { "text/xml", ".xml" },
        { "application/json", ".json" }
    };
    
    private static readonly Dictionary<string, string> ExtensionToMime = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".pdf", "application/pdf" },
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".gif", "image/gif" },
        { ".bmp", "image/bmp" },
        { ".webp", "image/webp" },
        { ".tiff", "image/tiff" },
        { ".tif", "image/tiff" },
        { ".dcm", "application/dicom" },
        { ".dicom", "application/dicom" },
        { ".txt", "text/plain" },
        { ".xml", "application/xml" },
        { ".json", "application/json" }
    };

    public DocumentStorageService(
        IOptions<DocumentStorageSettings> settings,
        ILogger<DocumentStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public string GenerateStoragePath(string uuid, int idPatient, string extension)
    {
        if (string.IsNullOrWhiteSpace(uuid))
            throw new ArgumentException("UUID ne peut pas être vide", nameof(uuid));
        
        if (idPatient <= 0)
            throw new ArgumentException("ID patient invalide", nameof(idPatient));
        
        // Normaliser l'extension
        extension = NormalizeExtension(extension);
        
        // Extraire les parties de l'UUID pour la structure de dossiers
        var uuidClean = uuid.Replace("-", "");
        var uuidPart1 = uuidClean.Substring(0, 2).ToLowerInvariant();
        var uuidPart2 = uuidClean.Substring(2, 2).ToLowerInvariant();
        
        // Date courante pour l'organisation par année/mois
        var now = DateTime.UtcNow;
        var year = now.Year.ToString();
        var month = now.Month.ToString("D2");
        
        // Construire le chemin relatif
        // Format: {annee}/{mois}/patient_{id_patient}/{uuid_part1}/{uuid_part2}/{uuid}.{ext}
        var relativePath = Path.Combine(
            year,
            month,
            $"patient_{idPatient}",
            uuidPart1,
            uuidPart2,
            $"{uuid}{extension}"
        );
        
        // Normaliser les séparateurs de chemin pour Linux
        return relativePath.Replace("\\", "/");
    }

    /// <inheritdoc />
    public string GetAbsolutePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("Chemin relatif ne peut pas être vide", nameof(relativePath));
        
        return Path.Combine(_settings.RootPath, relativePath);
    }

    /// <inheritdoc />
    public async Task<DocumentUploadResult> SaveFileAsync(IFormFile file, string uuid, int idPatient)
    {
        try
        {
            // Valider le fichier
            var validation = ValidateFile(file);
            if (!validation.IsValid)
            {
                return new DocumentUploadResult
                {
                    Success = false,
                    ErrorMessage = string.Join("; ", validation.Errors)
                };
            }
            
            // Déterminer l'extension
            var extension = GetExtensionFromMimeType(file.ContentType);
            if (string.IsNullOrEmpty(extension))
            {
                extension = Path.GetExtension(file.FileName);
            }
            
            // Générer le chemin
            var relativePath = GenerateStoragePath(uuid, idPatient, extension);
            var absolutePath = GetAbsolutePath(relativePath);
            
            // Créer les dossiers si nécessaire
            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Dossier créé: {Directory}", directory);
            }
            
            // Sauvegarder le fichier
            await using var stream = new FileStream(absolutePath, FileMode.Create);
            await file.CopyToAsync(stream);
            
            // Calculer le hash
            stream.Position = 0;
            var hash = await CalculateHashFromStreamAsync(stream);
            
            _logger.LogInformation(
                "Document sauvegardé: UUID={Uuid}, Patient={PatientId}, Path={Path}, Size={Size}",
                uuid, idPatient, relativePath, file.Length);
            
            return new DocumentUploadResult
            {
                Success = true,
                RelativePath = relativePath,
                AbsolutePath = absolutePath,
                StorageFileName = $"{uuid}{extension}",
                Extension = extension,
                MimeType = file.ContentType,
                FileSize = file.Length,
                HashSha256 = hash
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la sauvegarde du document UUID={Uuid}", uuid);
            return new DocumentUploadResult
            {
                Success = false,
                ErrorMessage = $"Erreur lors de la sauvegarde: {ex.Message}"
            };
        }
    }

    /// <inheritdoc />
    public async Task<DocumentUploadResult> SaveFileAsync(byte[] content, string uuid, int idPatient, string originalFileName, string mimeType)
    {
        try
        {
            if (content == null || content.Length == 0)
            {
                return new DocumentUploadResult
                {
                    Success = false,
                    ErrorMessage = "Contenu du fichier vide"
                };
            }
            
            if (content.Length > _settings.MaxFileSizeBytes)
            {
                return new DocumentUploadResult
                {
                    Success = false,
                    ErrorMessage = $"Fichier trop volumineux. Maximum: {_settings.MaxFileSizeBytes / 1024 / 1024} Mo"
                };
            }
            
            // Déterminer l'extension
            var extension = GetExtensionFromMimeType(mimeType);
            if (string.IsNullOrEmpty(extension))
            {
                extension = Path.GetExtension(originalFileName);
            }
            
            // Générer le chemin
            var relativePath = GenerateStoragePath(uuid, idPatient, extension);
            var absolutePath = GetAbsolutePath(relativePath);
            
            // Créer les dossiers si nécessaire
            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Dossier créé: {Directory}", directory);
            }
            
            // Sauvegarder le fichier
            await File.WriteAllBytesAsync(absolutePath, content);
            
            // Calculer le hash
            var hash = CalculateHashFromBytes(content);
            
            _logger.LogInformation(
                "Document sauvegardé: UUID={Uuid}, Patient={PatientId}, Path={Path}, Size={Size}",
                uuid, idPatient, relativePath, content.Length);
            
            return new DocumentUploadResult
            {
                Success = true,
                RelativePath = relativePath,
                AbsolutePath = absolutePath,
                StorageFileName = $"{uuid}{extension}",
                Extension = extension,
                MimeType = mimeType,
                FileSize = content.Length,
                HashSha256 = hash
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la sauvegarde du document UUID={Uuid}", uuid);
            return new DocumentUploadResult
            {
                Success = false,
                ErrorMessage = $"Erreur lors de la sauvegarde: {ex.Message}"
            };
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> ReadFileAsync(string relativePath)
    {
        var absolutePath = GetAbsolutePath(relativePath);
        
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException($"Fichier non trouvé: {relativePath}");
        }
        
        return await File.ReadAllBytesAsync(absolutePath);
    }

    /// <inheritdoc />
    public Task<Stream> OpenReadStreamAsync(string relativePath)
    {
        var absolutePath = GetAbsolutePath(relativePath);
        
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException($"Fichier non trouvé: {relativePath}");
        }
        
        Stream stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    /// <inheritdoc />
    public bool FileExists(string relativePath)
    {
        var absolutePath = GetAbsolutePath(relativePath);
        return File.Exists(absolutePath);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteFileAsync(string relativePath)
    {
        try
        {
            var absolutePath = GetAbsolutePath(relativePath);
            
            if (!File.Exists(absolutePath))
            {
                _logger.LogWarning("Fichier à supprimer non trouvé: {Path}", relativePath);
                return false;
            }
            
            // Déplacer vers quarantaine au lieu de supprimer définitivement
            var quarantinePath = Path.Combine(_settings.RootPath, "quarantine", relativePath);
            var quarantineDir = Path.GetDirectoryName(quarantinePath);
            
            if (!string.IsNullOrEmpty(quarantineDir) && !Directory.Exists(quarantineDir))
            {
                Directory.CreateDirectory(quarantineDir);
            }
            
            File.Move(absolutePath, quarantinePath);
            
            _logger.LogInformation("Document déplacé vers quarantaine: {Path}", relativePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la suppression du document: {Path}", relativePath);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<string> CalculateHashAsync(string relativePath)
    {
        var absolutePath = GetAbsolutePath(relativePath);
        
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException($"Fichier non trouvé: {relativePath}");
        }
        
        await using var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return await CalculateHashFromStreamAsync(stream);
    }

    /// <inheritdoc />
    public async Task<IntegrityCheckResult> VerifyIntegrityAsync(string relativePath, string? expectedHash, ulong? expectedSize)
    {
        var absolutePath = GetAbsolutePath(relativePath);
        
        // Vérifier l'existence du fichier
        if (!File.Exists(absolutePath))
        {
            return new IntegrityCheckResult
            {
                IsValid = false,
                Status = "fichier_absent",
                ExpectedHash = expectedHash,
                ExpectedSize = expectedSize,
                ErrorMessage = "Fichier introuvable sur le disque"
            };
        }
        
        try
        {
            var fileInfo = new FileInfo(absolutePath);
            var actualSize = (ulong)fileInfo.Length;
            
            // Calculer le hash
            await using var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var calculatedHash = await CalculateHashFromStreamAsync(stream);
            
            // Vérifier le hash si fourni
            if (!string.IsNullOrEmpty(expectedHash) && 
                !string.Equals(calculatedHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                return new IntegrityCheckResult
                {
                    IsValid = false,
                    Status = "hash_invalide",
                    CalculatedHash = calculatedHash,
                    ExpectedHash = expectedHash,
                    ActualSize = actualSize,
                    ExpectedSize = expectedSize,
                    ErrorMessage = "Hash SHA-256 ne correspond pas"
                };
            }
            
            // Vérifier la taille si fournie
            if (expectedSize.HasValue && actualSize != expectedSize.Value)
            {
                return new IntegrityCheckResult
                {
                    IsValid = false,
                    Status = "hash_invalide",
                    CalculatedHash = calculatedHash,
                    ExpectedHash = expectedHash,
                    ActualSize = actualSize,
                    ExpectedSize = expectedSize,
                    ErrorMessage = "Taille du fichier ne correspond pas"
                };
            }
            
            return new IntegrityCheckResult
            {
                IsValid = true,
                Status = "ok",
                CalculatedHash = calculatedHash,
                ExpectedHash = expectedHash,
                ActualSize = actualSize,
                ExpectedSize = expectedSize
            };
        }
        catch (Exception ex)
        {
            return new IntegrityCheckResult
            {
                IsValid = false,
                Status = "erreur_lecture",
                ExpectedHash = expectedHash,
                ExpectedSize = expectedSize,
                ErrorMessage = $"Erreur de lecture: {ex.Message}"
            };
        }
    }

    /// <inheritdoc />
    public string GetExtensionFromMimeType(string mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
            return ".bin";
        
        return MimeToExtension.TryGetValue(mimeType, out var ext) ? ext : ".bin";
    }

    /// <inheritdoc />
    public string GetMimeTypeFromExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return "application/octet-stream";
        
        extension = NormalizeExtension(extension);
        return ExtensionToMime.TryGetValue(extension, out var mime) ? mime : "application/octet-stream";
    }

    /// <inheritdoc />
    public FileValidationResult ValidateFile(IFormFile file)
    {
        var result = new FileValidationResult { IsValid = true };
        
        if (file == null || file.Length == 0)
        {
            result.IsValid = false;
            result.Errors.Add("Fichier vide ou non fourni");
            return result;
        }
        
        // Vérifier la taille
        if (file.Length > _settings.MaxFileSizeBytes)
        {
            result.IsValid = false;
            result.Errors.Add($"Fichier trop volumineux. Maximum: {_settings.MaxFileSizeBytes / 1024 / 1024} Mo");
        }
        
        // Vérifier le type MIME
        if (!_settings.AllowedMimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            result.IsValid = false;
            result.Errors.Add($"Type de fichier non autorisé: {file.ContentType}");
        }
        
        // Vérifier l'extension
        var extension = Path.GetExtension(file.FileName);
        if (!_settings.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            result.IsValid = false;
            result.Errors.Add($"Extension non autorisée: {extension}");
        }
        
        return result;
    }

    /// <inheritdoc />
    public Task InitializeStorageAsync()
    {
        try
        {
            // Créer le dossier racine
            if (!Directory.Exists(_settings.RootPath))
            {
                Directory.CreateDirectory(_settings.RootPath);
                _logger.LogInformation("Dossier racine de stockage créé: {Path}", _settings.RootPath);
            }
            
            // Créer les sous-dossiers système
            var systemDirs = new[] { "quarantine", "temp", "backup" };
            foreach (var dir in systemDirs)
            {
                var path = Path.Combine(_settings.RootPath, dir);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    _logger.LogInformation("Dossier système créé: {Path}", path);
                }
            }
            
            _logger.LogInformation("Structure de stockage initialisée: {Path}", _settings.RootPath);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'initialisation du stockage");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentUploadResult> SaveFileTransactionalAsync(IFormFile file, string uuid, int idPatient)
    {
        string? tempFilePath = null;
        
        try
        {
            // 1. Valider le fichier
            var validation = ValidateFile(file);
            if (!validation.IsValid)
            {
                return new DocumentUploadResult
                {
                    Success = false,
                    ErrorMessage = string.Join("; ", validation.Errors)
                };
            }
            
            // 2. Vérifier l'espace disque
            if (!HasSufficientDiskSpace(file.Length))
            {
                return new DocumentUploadResult
                {
                    Success = false,
                    ErrorMessage = "Espace disque insuffisant"
                };
            }
            
            // 3. Upload vers dossier temporaire
            var tempDir = Path.Combine(_settings.RootPath, "temp");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }
            
            tempFilePath = Path.Combine(tempDir, $"{uuid}_temp{Path.GetExtension(file.FileName)}");
            
            await using (var tempStream = new FileStream(tempFilePath, FileMode.Create))
            {
                await file.CopyToAsync(tempStream);
            }
            
            _logger.LogInformation("Fichier temporaire créé: {Path}", tempFilePath);
            
            // 4. Calculer le hash SHA-256
            string hash;
            await using (var hashStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
            {
                hash = await CalculateHashFromStreamAsync(hashStream);
            }
            
            // 5. Vérifier les doublons par hash
            var existingPath = await FindExistingFileByHashAsync(hash);
            if (!string.IsNullOrEmpty(existingPath))
            {
                // Supprimer le fichier temporaire
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
                
                _logger.LogInformation("Doublon détecté par hash: {Hash}, fichier existant: {Path}", hash, existingPath);
                
                return new DocumentUploadResult
                {
                    Success = true,
                    RelativePath = existingPath,
                    AbsolutePath = GetAbsolutePath(existingPath),
                    HashSha256 = hash,
                    FileSize = file.Length,
                    MimeType = file.ContentType,
                    IsDuplicate = true
                };
            }
            
            // 6. Déterminer l'extension et générer le chemin final
            var extension = GetExtensionFromMimeType(file.ContentType);
            if (string.IsNullOrEmpty(extension))
            {
                extension = Path.GetExtension(file.FileName);
            }
            
            var relativePath = GenerateStoragePath(uuid, idPatient, extension);
            var absolutePath = GetAbsolutePath(relativePath);
            
            // 7. Créer les dossiers si nécessaire
            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // 8. Déplacement atomique vers le chemin final
            File.Move(tempFilePath, absolutePath);
            tempFilePath = null; // Marquer comme déplacé
            
            _logger.LogInformation(
                "Document sauvegardé (transactionnel): UUID={Uuid}, Patient={PatientId}, Path={Path}, Size={Size}, Hash={Hash}",
                uuid, idPatient, relativePath, file.Length, hash);
            
            return new DocumentUploadResult
            {
                Success = true,
                RelativePath = relativePath,
                AbsolutePath = absolutePath,
                StorageFileName = $"{uuid}{extension}",
                Extension = extension,
                MimeType = file.ContentType,
                FileSize = file.Length,
                HashSha256 = hash,
                IsDuplicate = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la sauvegarde transactionnelle du document UUID={Uuid}", uuid);
            
            // Nettoyer le fichier temporaire en cas d'erreur
            if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Impossible de supprimer le fichier temporaire: {Path}", tempFilePath);
                }
            }
            
            return new DocumentUploadResult
            {
                Success = false,
                ErrorMessage = $"Erreur lors de la sauvegarde: {ex.Message}"
            };
        }
    }

    /// <inheritdoc />
    public Task<string?> FindExistingFileByHashAsync(string hash)
    {
        // Cette méthode doit être appelée avec le contexte DB
        // Pour l'instant, retourne null - sera implémenté via le contrôleur
        // qui a accès au DbContext pour chercher dans documents_medicaux
        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc />
    public bool HasSufficientDiskSpace(long requiredBytes)
    {
        try
        {
            var rootPath = _settings.RootPath;
            
            // Obtenir les informations du disque
            var driveInfo = new DriveInfo(Path.GetPathRoot(rootPath) ?? rootPath);
            
            // Vérifier qu'il reste au moins 100 Mo + la taille requise
            var minFreeSpace = requiredBytes + (100 * 1024 * 1024); // 100 Mo de marge
            
            if (driveInfo.AvailableFreeSpace < minFreeSpace)
            {
                _logger.LogWarning(
                    "Espace disque insuffisant. Requis: {Required} Mo, Disponible: {Available} Mo",
                    minFreeSpace / 1024 / 1024,
                    driveInfo.AvailableFreeSpace / 1024 / 1024);
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossible de vérifier l'espace disque, on continue");
            return true; // En cas d'erreur, on laisse passer
        }
    }

    #region Private Methods
    
    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return ".bin";
        
        extension = extension.Trim().ToLowerInvariant();
        if (!extension.StartsWith("."))
            extension = "." + extension;
        
        return extension;
    }
    
    private static async Task<string> CalculateHashFromStreamAsync(Stream stream)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
    
    private static string CalculateHashFromBytes(byte[] content)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(content);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
    
    #endregion
}
