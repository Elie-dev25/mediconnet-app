namespace Mediconnet_Backend.Core.Configuration;

/// <summary>
/// Configuration du stockage des documents médicaux
/// Structure: /storage/mediConnect/{annee}/{mois}/patient_{id_patient}/{uuid_part1}/{uuid_part2}/{uuid}.{ext}
/// </summary>
public class DocumentStorageSettings
{
    public const string SectionName = "DocumentStorage";
    
    /// <summary>
    /// Chemin racine du stockage (ex: /storage/mediConnect)
    /// </summary>
    public string RootPath { get; set; } = "/storage/mediConnect";
    
    /// <summary>
    /// Taille maximale d'un fichier en octets (par défaut 50 Mo)
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 52_428_800;
    
    /// <summary>
    /// Types MIME autorisés
    /// </summary>
    public List<string> AllowedMimeTypes { get; set; } = new()
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/bmp",
        "image/webp",
        "image/tiff",
        "application/dicom",
        "text/plain",
        "application/xml",
        "application/json"
    };
    
    /// <summary>
    /// Extensions autorisées
    /// </summary>
    public List<string> AllowedExtensions { get; set; } = new()
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif",
        ".dcm", ".dicom", ".txt", ".xml", ".json"
    };
    
    /// <summary>
    /// Permissions des dossiers (Unix: 750)
    /// </summary>
    public string DirectoryPermissions { get; set; } = "750";
    
    /// <summary>
    /// Permissions des fichiers (Unix: 640)
    /// </summary>
    public string FilePermissions { get; set; } = "640";
}
