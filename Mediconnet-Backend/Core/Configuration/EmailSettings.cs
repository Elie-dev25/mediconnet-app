namespace Mediconnet_Backend.Core.Configuration;

/// <summary>
/// Configuration SMTP pour l'envoi d'emails
/// </summary>
public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    /// <summary>Serveur SMTP (ex: smtp.gmail.com, localhost pour dev)</summary>
    public string SmtpServer { get; set; } = "localhost";

    /// <summary>Port SMTP (587 pour TLS, 465 pour SSL, 1025 pour MailHog)</summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>Utiliser SSL/TLS</summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>Email de l'expéditeur</summary>
    public string SenderEmail { get; set; } = string.Empty;

    /// <summary>Nom affiché de l'expéditeur</summary>
    public string SenderName { get; set; } = "MediConnect";

    /// <summary>Nom d'utilisateur SMTP (souvent l'email)</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Mot de passe SMTP ou App Password</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Activer la confirmation d'email obligatoire</summary>
    public bool EnableEmailConfirmation { get; set; } = true;

    /// <summary>Durée de validité du token en heures</summary>
    public int TokenExpirationHours { get; set; } = 24;

    /// <summary>Mode développement (logs les emails au lieu de les envoyer)</summary>
    public bool DevMode { get; set; } = false;
}

/// <summary>
/// Configuration générale de l'application
/// </summary>
public class AppSettings
{
    public const string SectionName = "AppSettings";

    /// <summary>URL du frontend (pour les liens dans les emails)</summary>
    public string FrontendUrl { get; set; } = "http://localhost:4200";

    /// <summary>URL de l'API</summary>
    public string ApiUrl { get; set; } = "http://localhost:5000";
}
