namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Interface pour le chiffrement/déchiffrement des données sensibles
/// Utilisé pour protéger les données médicales conformément au RGPD et HDS
/// </summary>
public interface IDataProtectionService
{
    /// <summary>
    /// Chiffre une chaîne de caractères sensible
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Déchiffre une chaîne de caractères
    /// </summary>
    string Decrypt(string encryptedText);

    /// <summary>
    /// Chiffre des données médicales (avec un purpose spécifique)
    /// </summary>
    string EncryptMedicalData(string plainText);

    /// <summary>
    /// Déchiffre des données médicales
    /// </summary>
    string DecryptMedicalData(string encryptedText);

    /// <summary>
    /// Vérifie si une chaîne est chiffrée
    /// </summary>
    bool IsEncrypted(string text);
}
