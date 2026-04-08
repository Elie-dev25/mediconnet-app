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

    /// <summary>
    /// Envoie une notification de nouvelle demande de coordination à l'anesthésiste
    /// </summary>
    Task<bool> SendCoordinationDemandeAsync(string toEmail, string nomAnesthesiste, string nomChirurgien, 
        string nomPatient, string dateIntervention, string heureIntervention, string indication);

    /// <summary>
    /// Envoie une notification de validation de coordination au chirurgien
    /// </summary>
    Task<bool> SendCoordinationValideeAsync(string toEmail, string nomChirurgien, string nomAnesthesiste,
        string nomPatient, string dateIntervention, string heureIntervention);

    /// <summary>
    /// Envoie une notification de contre-proposition au chirurgien
    /// </summary>
    Task<bool> SendCoordinationModifieeAsync(string toEmail, string nomChirurgien, string nomAnesthesiste,
        string nomPatient, string nouvelleDateIntervention, string nouvelleHeureIntervention, string commentaire);

    /// <summary>
    /// Envoie une notification de refus de coordination au chirurgien
    /// </summary>
    Task<bool> SendCoordinationRefuseeAsync(string toEmail, string nomChirurgien, string nomAnesthesiste,
        string nomPatient, string motifRefus);

    /// <summary>
    /// Envoie une notification d'intervention planifiée au patient
    /// </summary>
    Task<bool> SendInterventionPlanifieePatientAsync(string toEmail, string nomPatient, string nomChirurgien,
        string nomAnesthesiste, string dateIntervention, string heureIntervention, string? dateRdvPreop);

    /// <summary>
    /// Envoie une confirmation d'intervention à l'anesthésiste
    /// </summary>
    Task<bool> SendInterventionConfirmeeAnesthesisteAsync(string toEmail, string nomAnesthesiste, string nomChirurgien,
        string nomPatient, string dateIntervention, string heureIntervention, string? dateRdvPreop);
}
