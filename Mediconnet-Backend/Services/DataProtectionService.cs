using Microsoft.AspNetCore.DataProtection;
using Mediconnet_Backend.Core.Interfaces.Services;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service de chiffrement des données sensibles
/// Utilise ASP.NET Core Data Protection API pour le chiffrement
/// Conformité RGPD et HDS (Hébergeur de Données de Santé)
/// </summary>
public class DataProtectionService : IDataProtectionService
{
    private readonly IDataProtector _generalProtector;
    private readonly IDataProtector _medicalProtector;
    private readonly ILogger<DataProtectionService> _logger;
    private const string EncryptedPrefix = "ENC:";

    public DataProtectionService(
        IDataProtectionProvider dataProtectionProvider,
        ILogger<DataProtectionService> logger)
    {
        // Créer des protecteurs avec des purposes différents pour isolation
        _generalProtector = dataProtectionProvider.CreateProtector("Mediconnet.General.v1");
        _medicalProtector = dataProtectionProvider.CreateProtector("Mediconnet.MedicalData.v1");
        _logger = logger;
    }

    /// <inheritdoc />
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            var encrypted = _generalProtector.Protect(plainText);
            return $"{EncryptedPrefix}{encrypted}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du chiffrement des données");
            throw new InvalidOperationException("Impossible de chiffrer les données", ex);
        }
    }

    /// <inheritdoc />
    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return encryptedText;

        if (!IsEncrypted(encryptedText))
            return encryptedText;

        try
        {
            var cipherText = encryptedText.Substring(EncryptedPrefix.Length);
            return _generalProtector.Unprotect(cipherText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du déchiffrement des données");
            throw new InvalidOperationException("Impossible de déchiffrer les données", ex);
        }
    }

    /// <inheritdoc />
    public string EncryptMedicalData(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            var encrypted = _medicalProtector.Protect(plainText);
            return $"{EncryptedPrefix}{encrypted}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du chiffrement des données médicales");
            throw new InvalidOperationException("Impossible de chiffrer les données médicales", ex);
        }
    }

    /// <inheritdoc />
    public string DecryptMedicalData(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return encryptedText;

        if (!IsEncrypted(encryptedText))
            return encryptedText;

        try
        {
            var cipherText = encryptedText.Substring(EncryptedPrefix.Length);
            return _medicalProtector.Unprotect(cipherText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du déchiffrement des données médicales");
            throw new InvalidOperationException("Impossible de déchiffrer les données médicales", ex);
        }
    }

    /// <inheritdoc />
    public bool IsEncrypted(string text)
    {
        return !string.IsNullOrEmpty(text) && text.StartsWith(EncryptedPrefix);
    }
}
