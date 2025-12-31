using Mediconnet_Backend.Services;

namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Interface pour le service de confirmation d'email
/// </summary>
public interface IEmailConfirmationService
{
    /// <summary>
    /// Génère un token de confirmation pour un utilisateur
    /// </summary>
    /// <param name="userId">ID de l'utilisateur</param>
    /// <returns>Le token généré</returns>
    Task<string> GenerateConfirmationTokenAsync(int userId);

    /// <summary>
    /// Envoie un email de confirmation à l'utilisateur
    /// </summary>
    /// <param name="userId">ID de l'utilisateur</param>
    /// <param name="email">Adresse email</param>
    /// <param name="userName">Nom de l'utilisateur</param>
    /// <returns>True si l'email a été envoyé</returns>
    Task<bool> SendConfirmationEmailAsync(int userId, string email, string userName);

    /// <summary>
    /// Confirme l'email avec le token fourni
    /// </summary>
    /// <param name="token">Token de confirmation</param>
    /// <param name="ipAddress">Adresse IP (optionnel)</param>
    /// <returns>Résultat de la confirmation</returns>
    Task<EmailConfirmationResult> ConfirmEmailAsync(string token, string? ipAddress = null);

    /// <summary>
    /// Renvoie un email de confirmation
    /// </summary>
    /// <param name="email">Adresse email</param>
    /// <returns>True si l'email a été renvoyé</returns>
    Task<bool> ResendConfirmationEmailAsync(string email);

    /// <summary>
    /// Vérifie si l'email d'un utilisateur est confirmé
    /// </summary>
    /// <param name="userId">ID de l'utilisateur</param>
    /// <returns>True si confirmé</returns>
    Task<bool> IsEmailConfirmedAsync(int userId);
}
