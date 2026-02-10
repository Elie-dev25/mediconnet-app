using Microsoft.AspNetCore.Http;

namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Service de stockage des documents médicaux
/// Gère la création des chemins, l'upload et le téléchargement des fichiers
/// Structure: /storage/mediConnect/{annee}/{mois}/patient_{id_patient}/{uuid_part1}/{uuid_part2}/{uuid}.{ext}
/// </summary>
public interface IDocumentStorageService
{
    /// <summary>
    /// Génère le chemin de stockage pour un nouveau document
    /// </summary>
    /// <param name="uuid">UUID du document</param>
    /// <param name="idPatient">ID du patient</param>
    /// <param name="extension">Extension du fichier (avec ou sans point)</param>
    /// <returns>Chemin relatif depuis la racine de stockage</returns>
    string GenerateStoragePath(string uuid, int idPatient, string extension);
    
    /// <summary>
    /// Obtient le chemin absolu complet d'un document
    /// </summary>
    /// <param name="relativePath">Chemin relatif stocké en base</param>
    /// <returns>Chemin absolu sur le système de fichiers</returns>
    string GetAbsolutePath(string relativePath);
    
    /// <summary>
    /// Sauvegarde un fichier sur le disque
    /// </summary>
    /// <param name="file">Fichier uploadé</param>
    /// <param name="uuid">UUID du document</param>
    /// <param name="idPatient">ID du patient</param>
    /// <returns>Résultat de l'upload avec chemin et hash</returns>
    Task<DocumentUploadResult> SaveFileAsync(IFormFile file, string uuid, int idPatient);
    
    /// <summary>
    /// Sauvegarde un fichier à partir d'un tableau de bytes
    /// </summary>
    /// <param name="content">Contenu du fichier</param>
    /// <param name="uuid">UUID du document</param>
    /// <param name="idPatient">ID du patient</param>
    /// <param name="originalFileName">Nom original du fichier</param>
    /// <param name="mimeType">Type MIME</param>
    /// <returns>Résultat de l'upload avec chemin et hash</returns>
    Task<DocumentUploadResult> SaveFileAsync(byte[] content, string uuid, int idPatient, string originalFileName, string mimeType);
    
    /// <summary>
    /// Lit le contenu d'un fichier
    /// </summary>
    /// <param name="relativePath">Chemin relatif du fichier</param>
    /// <returns>Contenu du fichier</returns>
    Task<byte[]> ReadFileAsync(string relativePath);
    
    /// <summary>
    /// Ouvre un stream de lecture sur un fichier
    /// </summary>
    /// <param name="relativePath">Chemin relatif du fichier</param>
    /// <returns>Stream de lecture</returns>
    Task<Stream> OpenReadStreamAsync(string relativePath);
    
    /// <summary>
    /// Vérifie si un fichier existe
    /// </summary>
    /// <param name="relativePath">Chemin relatif du fichier</param>
    /// <returns>True si le fichier existe</returns>
    bool FileExists(string relativePath);
    
    /// <summary>
    /// Supprime un fichier (soft delete - déplace vers quarantaine)
    /// </summary>
    /// <param name="relativePath">Chemin relatif du fichier</param>
    /// <returns>True si supprimé avec succès</returns>
    Task<bool> DeleteFileAsync(string relativePath);
    
    /// <summary>
    /// Calcule le hash SHA-256 d'un fichier
    /// </summary>
    /// <param name="relativePath">Chemin relatif du fichier</param>
    /// <returns>Hash SHA-256 en hexadécimal</returns>
    Task<string> CalculateHashAsync(string relativePath);
    
    /// <summary>
    /// Vérifie l'intégrité d'un fichier
    /// </summary>
    /// <param name="relativePath">Chemin relatif du fichier</param>
    /// <param name="expectedHash">Hash attendu</param>
    /// <param name="expectedSize">Taille attendue</param>
    /// <returns>Résultat de la vérification</returns>
    Task<IntegrityCheckResult> VerifyIntegrityAsync(string relativePath, string? expectedHash, ulong? expectedSize);
    
    /// <summary>
    /// Obtient l'extension à partir du type MIME
    /// </summary>
    /// <param name="mimeType">Type MIME</param>
    /// <returns>Extension avec point (ex: .pdf)</returns>
    string GetExtensionFromMimeType(string mimeType);
    
    /// <summary>
    /// Obtient le type MIME à partir de l'extension
    /// </summary>
    /// <param name="extension">Extension du fichier</param>
    /// <returns>Type MIME</returns>
    string GetMimeTypeFromExtension(string extension);
    
    /// <summary>
    /// Valide un fichier avant upload
    /// </summary>
    /// <param name="file">Fichier à valider</param>
    /// <returns>Résultat de la validation</returns>
    FileValidationResult ValidateFile(IFormFile file);
    
    /// <summary>
    /// Initialise la structure de stockage (crée le dossier racine si nécessaire)
    /// </summary>
    Task InitializeStorageAsync();
    
    /// <summary>
    /// Upload transactionnel sécurisé: temp → validation → déplacement atomique
    /// </summary>
    /// <param name="file">Fichier uploadé</param>
    /// <param name="uuid">UUID du document</param>
    /// <param name="idPatient">ID du patient</param>
    /// <returns>Résultat de l'upload avec chemin et hash</returns>
    Task<DocumentUploadResult> SaveFileTransactionalAsync(IFormFile file, string uuid, int idPatient);
    
    /// <summary>
    /// Vérifie si un fichier avec le même hash existe déjà
    /// </summary>
    /// <param name="hash">Hash SHA-256 du fichier</param>
    /// <returns>Chemin relatif du fichier existant ou null</returns>
    Task<string?> FindExistingFileByHashAsync(string hash);
    
    /// <summary>
    /// Vérifie l'espace disque disponible
    /// </summary>
    /// <param name="requiredBytes">Espace requis en octets</param>
    /// <returns>True si l'espace est suffisant</returns>
    bool HasSufficientDiskSpace(long requiredBytes);
}

/// <summary>
/// Résultat d'un upload de document
/// </summary>
public class DocumentUploadResult
{
    public bool Success { get; set; }
    public string? RelativePath { get; set; }
    public string? AbsolutePath { get; set; }
    public string? StorageFileName { get; set; }
    public string? Extension { get; set; }
    public string? MimeType { get; set; }
    public long FileSize { get; set; }
    public string? HashSha256 { get; set; }
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Indique si le fichier existait déjà (doublon par hash)
    /// </summary>
    public bool IsDuplicate { get; set; }
    
    /// <summary>
    /// UUID du document existant si doublon
    /// </summary>
    public string? ExistingDocumentUuid { get; set; }
}

/// <summary>
/// Résultat de vérification d'intégrité
/// </summary>
public class IntegrityCheckResult
{
    public bool IsValid { get; set; }
    public string Status { get; set; } = "ok"; // ok, hash_invalide, fichier_absent, erreur_lecture
    public string? CalculatedHash { get; set; }
    public string? ExpectedHash { get; set; }
    public ulong? ActualSize { get; set; }
    public ulong? ExpectedSize { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Résultat de validation de fichier
/// </summary>
public class FileValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}
