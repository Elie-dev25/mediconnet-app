namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Interface pour le service d'envoi d'emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Envoie un email de confirmation d'adresse email
    /// </summary>
    /// <param name="toEmail">Adresse email du destinataire</param>
    /// <param name="userName">Nom de l'utilisateur</param>
    /// <param name="confirmationLink">Lien de confirmation</param>
    Task<bool> SendEmailConfirmationAsync(string toEmail, string userName, string confirmationLink);

    /// <summary>
    /// Envoie un email générique
    /// </summary>
    /// <param name="toEmail">Adresse email du destinataire</param>
    /// <param name="subject">Sujet de l'email</param>
    /// <param name="htmlBody">Corps de l'email en HTML</param>
    /// <param name="textBody">Corps de l'email en texte brut (optionnel)</param>
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null);

    /// <summary>
    /// Envoie un email de réinitialisation de mot de passe
    /// </summary>
    Task<bool> SendPasswordResetAsync(string toEmail, string userName, string resetLink);

    /// <summary>
    /// Envoie un email de bienvenue après confirmation
    /// </summary>
    Task<bool> SendWelcomeEmailAsync(string toEmail, string userName);
}
