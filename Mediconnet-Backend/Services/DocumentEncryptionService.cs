using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Configuration pour le chiffrement des documents
/// </summary>
public class EncryptionSettings
{
    public const string SectionName = "Encryption";
    
    /// <summary>
    /// Clé de chiffrement AES-256 (32 bytes en base64)
    /// IMPORTANT: Cette clé doit être stockée de manière sécurisée (variable d'environnement, Azure Key Vault, etc.)
    /// </summary>
    public string MasterKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Activer le chiffrement des documents
    /// </summary>
    public bool Enabled { get; set; } = false;
    
    /// <summary>
    /// Algorithme de chiffrement (AES-256-GCM recommandé)
    /// </summary>
    public string Algorithm { get; set; } = "AES-256-GCM";
}

/// <summary>
/// Interface pour le service de chiffrement des documents
/// </summary>
public interface IDocumentEncryptionService
{
    /// <summary>
    /// Chiffre un flux de données
    /// </summary>
    Task<EncryptedData> EncryptAsync(Stream inputStream);
    
    /// <summary>
    /// Déchiffre un flux de données
    /// </summary>
    Task<Stream> DecryptAsync(Stream encryptedStream, byte[] iv, byte[] tag);
    
    /// <summary>
    /// Chiffre un fichier et retourne le chemin du fichier chiffré
    /// </summary>
    Task<EncryptionResult> EncryptFileAsync(string sourcePath, string destinationPath);
    
    /// <summary>
    /// Déchiffre un fichier vers un flux
    /// </summary>
    Task<Stream> DecryptFileAsync(string encryptedPath, byte[] iv, byte[] tag);
    
    /// <summary>
    /// Vérifie si le chiffrement est activé
    /// </summary>
    bool IsEnabled { get; }
}

/// <summary>
/// Résultat du chiffrement
/// </summary>
public class EncryptedData
{
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public byte[] IV { get; set; } = Array.Empty<byte>();
    public byte[] Tag { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// Résultat du chiffrement d'un fichier
/// </summary>
public class EncryptionResult
{
    public bool Success { get; set; }
    public string? EncryptedPath { get; set; }
    public byte[] IV { get; set; } = Array.Empty<byte>();
    public byte[] Tag { get; set; } = Array.Empty<byte>();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Service de chiffrement AES-256-GCM pour les documents médicaux
/// Conforme aux exigences de sécurité du milieu médical
/// </summary>
public class DocumentEncryptionService : IDocumentEncryptionService
{
    private readonly EncryptionSettings _settings;
    private readonly ILogger<DocumentEncryptionService> _logger;
    private readonly byte[] _masterKey;
    
    // Taille du tag d'authentification GCM (128 bits)
    private const int GcmTagSize = 16;
    // Taille du nonce/IV pour GCM (96 bits recommandé)
    private const int GcmNonceSize = 12;
    
    public DocumentEncryptionService(
        IOptions<EncryptionSettings> settings,
        ILogger<DocumentEncryptionService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        
        if (_settings.Enabled)
        {
            if (string.IsNullOrEmpty(_settings.MasterKey))
            {
                throw new InvalidOperationException(
                    "La clé de chiffrement (Encryption:MasterKey) doit être configurée lorsque le chiffrement est activé");
            }
            
            try
            {
                _masterKey = Convert.FromBase64String(_settings.MasterKey);
                if (_masterKey.Length != 32)
                {
                    throw new InvalidOperationException(
                        "La clé de chiffrement doit être de 256 bits (32 bytes)");
                }
            }
            catch (FormatException)
            {
                throw new InvalidOperationException(
                    "La clé de chiffrement doit être encodée en base64");
            }
            
            _logger.LogInformation("Service de chiffrement initialisé avec AES-256-GCM");
        }
        else
        {
            _masterKey = Array.Empty<byte>();
            _logger.LogWarning("Le chiffrement des documents est DÉSACTIVÉ");
        }
    }
    
    public bool IsEnabled => _settings.Enabled;
    
    /// <inheritdoc />
    public async Task<EncryptedData> EncryptAsync(Stream inputStream)
    {
        if (!_settings.Enabled)
        {
            throw new InvalidOperationException("Le chiffrement n'est pas activé");
        }
        
        // Lire tout le flux en mémoire
        using var memoryStream = new MemoryStream();
        await inputStream.CopyToAsync(memoryStream);
        var plaintext = memoryStream.ToArray();
        
        // Générer un nonce aléatoire
        var nonce = new byte[GcmNonceSize];
        RandomNumberGenerator.Fill(nonce);
        
        // Préparer les buffers
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[GcmTagSize];
        
        // Chiffrer avec AES-GCM
        using var aesGcm = new AesGcm(_masterKey, GcmTagSize);
        aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
        
        _logger.LogDebug("Document chiffré: {Size} bytes", plaintext.Length);
        
        return new EncryptedData
        {
            Data = ciphertext,
            IV = nonce,
            Tag = tag
        };
    }
    
    /// <inheritdoc />
    public Task<Stream> DecryptAsync(Stream encryptedStream, byte[] iv, byte[] tag)
    {
        if (!_settings.Enabled)
        {
            throw new InvalidOperationException("Le chiffrement n'est pas activé");
        }
        
        if (iv.Length != GcmNonceSize)
        {
            throw new ArgumentException($"L'IV doit être de {GcmNonceSize} bytes", nameof(iv));
        }
        
        if (tag.Length != GcmTagSize)
        {
            throw new ArgumentException($"Le tag doit être de {GcmTagSize} bytes", nameof(tag));
        }
        
        // Lire le flux chiffré
        using var memoryStream = new MemoryStream();
        encryptedStream.CopyTo(memoryStream);
        var ciphertext = memoryStream.ToArray();
        
        // Préparer le buffer pour le texte clair
        var plaintext = new byte[ciphertext.Length];
        
        // Déchiffrer avec AES-GCM
        using var aesGcm = new AesGcm(_masterKey, GcmTagSize);
        aesGcm.Decrypt(iv, ciphertext, tag, plaintext);
        
        _logger.LogDebug("Document déchiffré: {Size} bytes", plaintext.Length);
        
        return Task.FromResult<Stream>(new MemoryStream(plaintext));
    }
    
    /// <inheritdoc />
    public async Task<EncryptionResult> EncryptFileAsync(string sourcePath, string destinationPath)
    {
        if (!_settings.Enabled)
        {
            return new EncryptionResult
            {
                Success = false,
                ErrorMessage = "Le chiffrement n'est pas activé"
            };
        }
        
        try
        {
            // Lire le fichier source
            var plaintext = await File.ReadAllBytesAsync(sourcePath);
            
            // Générer un nonce aléatoire
            var nonce = new byte[GcmNonceSize];
            RandomNumberGenerator.Fill(nonce);
            
            // Préparer les buffers
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[GcmTagSize];
            
            // Chiffrer avec AES-GCM
            using var aesGcm = new AesGcm(_masterKey, GcmTagSize);
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
            
            // Écrire le fichier chiffré (format: [nonce][tag][ciphertext])
            await using var outputStream = new FileStream(destinationPath, FileMode.Create);
            await outputStream.WriteAsync(nonce);
            await outputStream.WriteAsync(tag);
            await outputStream.WriteAsync(ciphertext);
            
            _logger.LogInformation(
                "Fichier chiffré: {Source} -> {Dest} ({Size} bytes)",
                sourcePath, destinationPath, plaintext.Length);
            
            return new EncryptionResult
            {
                Success = true,
                EncryptedPath = destinationPath,
                IV = nonce,
                Tag = tag
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du chiffrement du fichier: {Path}", sourcePath);
            return new EncryptionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
    
    /// <inheritdoc />
    public async Task<Stream> DecryptFileAsync(string encryptedPath, byte[] iv, byte[] tag)
    {
        if (!_settings.Enabled)
        {
            // Si le chiffrement n'est pas activé, retourner le fichier tel quel
            return new FileStream(encryptedPath, FileMode.Open, FileAccess.Read);
        }
        
        try
        {
            // Lire le fichier chiffré
            var encryptedData = await File.ReadAllBytesAsync(encryptedPath);
            
            // Si IV et tag sont fournis, les utiliser
            // Sinon, les extraire du début du fichier
            byte[] nonce, authTag, ciphertext;
            
            if (iv.Length > 0 && tag.Length > 0)
            {
                nonce = iv;
                authTag = tag;
                ciphertext = encryptedData;
            }
            else
            {
                // Format: [nonce][tag][ciphertext]
                nonce = encryptedData[..GcmNonceSize];
                authTag = encryptedData[GcmNonceSize..(GcmNonceSize + GcmTagSize)];
                ciphertext = encryptedData[(GcmNonceSize + GcmTagSize)..];
            }
            
            // Préparer le buffer pour le texte clair
            var plaintext = new byte[ciphertext.Length];
            
            // Déchiffrer avec AES-GCM
            using var aesGcm = new AesGcm(_masterKey, GcmTagSize);
            aesGcm.Decrypt(nonce, ciphertext, authTag, plaintext);
            
            _logger.LogDebug("Fichier déchiffré: {Path} ({Size} bytes)", encryptedPath, plaintext.Length);
            
            return new MemoryStream(plaintext);
        }
        catch (AuthenticationTagMismatchException)
        {
            _logger.LogError("Échec de l'authentification lors du déchiffrement: {Path}", encryptedPath);
            throw new CryptographicException("Le fichier a été altéré ou la clé est incorrecte");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du déchiffrement du fichier: {Path}", encryptedPath);
            throw;
        }
    }
    
    /// <summary>
    /// Génère une nouvelle clé de chiffrement AES-256
    /// Utilitaire pour la configuration initiale
    /// </summary>
    public static string GenerateNewKey()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }
}
