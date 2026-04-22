using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;
using Mediconnet_Backend.Core.Configuration;
using Mediconnet_Backend.Core.Interfaces.Services;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service d'envoi d'emails via SMTP (MailKit)
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly AppSettings _appSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailSettings> emailSettings,
        IOptions<AppSettings> appSettings,
        ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _appSettings = appSettings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            message.To.Add(new MailboxAddress(toEmail, toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = textBody ?? StripHtml(htmlBody)
            };

            message.Body = builder.ToMessageBody();

            // Mode développement : logger l'email au lieu de l'envoyer
            if (_emailSettings.DevMode)
            {
                _logger.LogInformation("=== EMAIL (DEV MODE) ===");
                _logger.LogInformation("To: {ToEmail}", toEmail);
                _logger.LogInformation("Subject: {Subject}", subject);
                _logger.LogInformation($"Body: {textBody ?? htmlBody}");
                _logger.LogInformation("========================");
                return true;
            }

            using var client = new SmtpClient();

            // Configurer la sécurité
            var secureSocketOptions = _emailSettings.UseSsl 
                ? SecureSocketOptions.StartTls 
                : SecureSocketOptions.None;

            await client.ConnectAsync(
                _emailSettings.SmtpServer, 
                _emailSettings.SmtpPort, 
                secureSocketOptions);

            // Authentification si credentials fournis
            if (!string.IsNullOrEmpty(_emailSettings.Username) && 
                !string.IsNullOrEmpty(_emailSettings.Password))
            {
                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to send email to {ToEmail}: {Message}", toEmail, ex.Message);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SendEmailConfirmationAsync(string toEmail, string userName, string confirmationLink)
    {
        var subject = "Confirmez votre adresse email - MediConnect";
        var htmlBody = GetEmailConfirmationTemplate(userName, confirmationLink);
        
        return await SendEmailAsync(toEmail, subject, htmlBody);
    }

    /// <inheritdoc />
    public async Task<bool> SendPasswordResetAsync(string toEmail, string userName, string resetLink)
    {
        var subject = "Réinitialisation de votre mot de passe - MediConnect";
        var htmlBody = GetPasswordResetTemplate(userName, resetLink);
        
        return await SendEmailAsync(toEmail, subject, htmlBody);
    }

    /// <inheritdoc />
    public async Task<bool> SendWelcomeEmailAsync(string toEmail, string userName)
    {
        var subject = "Bienvenue sur MediConnect !";
        var htmlBody = GetWelcomeTemplate(userName);
        
        return await SendEmailAsync(toEmail, subject, htmlBody);
    }

    #region Email Templates

    private string GetEmailConfirmationTemplate(string userName, string confirmationLink)
    {
        return $@"
<!DOCTYPE html>
<html lang=""fr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Confirmation d'email</title>
</head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7fa;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.1);"">
                    <!-- Header -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #0e7490 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 12px 12px 0 0;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: 600;"">
                                🏥 MediConnect
                            </h1>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #1f2937; margin: 0 0 20px; font-size: 24px;"">
                                Bonjour {userName} 👋
                            </h2>
                            
                            <p style=""color: #4b5563; font-size: 16px; line-height: 1.6; margin: 0 0 25px;"">
                                Merci de vous être inscrit sur <strong>MediConnect</strong> ! 
                                Pour activer votre compte et accéder à tous nos services, veuillez confirmer votre adresse email en cliquant sur le bouton ci-dessous.
                            </p>
                            
                            <div style=""text-align: center; margin: 35px 0;"">
                                <a href=""{confirmationLink}"" 
                                   style=""background: linear-gradient(135deg, #0e7490 0%, #0891b2 100%); 
                                          color: #ffffff; 
                                          text-decoration: none; 
                                          padding: 16px 40px; 
                                          border-radius: 8px; 
                                          font-size: 16px; 
                                          font-weight: 600;
                                          display: inline-block;
                                          box-shadow: 0 4px 12px rgba(14, 116, 144, 0.3);"">
                                    ✉️ Confirmer mon email
                                </a>
                            </div>
                            
                            <p style=""color: #6b7280; font-size: 14px; line-height: 1.5; margin: 25px 0 0;"">
                                Si le bouton ne fonctionne pas, copiez et collez ce lien dans votre navigateur :
                            </p>
                            <p style=""color: #0e7490; font-size: 13px; word-break: break-all; background-color: #f3f4f6; padding: 12px; border-radius: 6px; margin: 10px 0 0;"">
                                {confirmationLink}
                            </p>
                            
                            <div style=""margin-top: 30px; padding: 20px; background-color: #fef3c7; border-radius: 8px; border-left: 4px solid #d97706;"">
                                <p style=""color: #92400e; font-size: 14px; margin: 0;"">
                                    ⏰ <strong>Ce lien expire dans 24 heures.</strong><br>
                                    Si vous n'avez pas créé de compte, ignorez simplement cet email.
                                </p>
                            </div>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #f9fafb; padding: 25px 30px; text-align: center; border-radius: 0 0 12px 12px; border-top: 1px solid #e5e7eb;"">
                            <p style=""color: #9ca3af; font-size: 13px; margin: 0;"">
                                © {DateTime.Now.Year} MediConnect. Tous droits réservés.
                            </p>
                            <p style=""color: #9ca3af; font-size: 12px; margin: 10px 0 0;"">
                                Cet email a été envoyé automatiquement, merci de ne pas y répondre.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private string GetPasswordResetTemplate(string userName, string resetLink)
    {
        return $@"
<!DOCTYPE html>
<html lang=""fr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7fa;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #0e7490 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 12px 12px 0 0;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px;"">🏥 MediConnect</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #1f2937; margin: 0 0 20px;"">Réinitialisation du mot de passe</h2>
                            <p style=""color: #4b5563; font-size: 16px; line-height: 1.6;"">
                                Bonjour {userName},<br><br>
                                Vous avez demandé la réinitialisation de votre mot de passe. Cliquez sur le bouton ci-dessous pour créer un nouveau mot de passe.
                            </p>
                            <div style=""text-align: center; margin: 35px 0;"">
                                <a href=""{resetLink}"" 
                                   style=""background: linear-gradient(135deg, #dc2626 0%, #ef4444 100%); 
                                          color: #ffffff; 
                                          text-decoration: none; 
                                          padding: 16px 40px; 
                                          border-radius: 8px; 
                                          font-size: 16px; 
                                          font-weight: 600;
                                          display: inline-block;"">
                                    🔐 Réinitialiser mon mot de passe
                                </a>
                            </div>
                            <div style=""margin-top: 30px; padding: 20px; background-color: #fee2e2; border-radius: 8px; border-left: 4px solid #dc2626;"">
                                <p style=""color: #991b1b; font-size: 14px; margin: 0;"">
                                    ⚠️ Si vous n'avez pas demandé cette réinitialisation, ignorez cet email ou contactez notre support.
                                </p>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f9fafb; padding: 25px 30px; text-align: center; border-radius: 0 0 12px 12px;"">
                            <p style=""color: #9ca3af; font-size: 13px; margin: 0;"">© {DateTime.Now.Year} MediConnect</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private string GetWelcomeTemplate(string userName)
    {
        var dashboardLink = $"{_appSettings.FrontendUrl}/patient/dashboard";
        
        return $@"
<!DOCTYPE html>
<html lang=""fr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7fa;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #059669 0%, #10b981 100%); padding: 30px; text-align: center; border-radius: 12px 12px 0 0;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px;"">🎉 Bienvenue !</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #1f2937; margin: 0 0 20px;"">Félicitations {userName} !</h2>
                            <p style=""color: #4b5563; font-size: 16px; line-height: 1.6;"">
                                Votre adresse email a été confirmée avec succès. Votre compte MediConnect est maintenant actif !
                            </p>
                            <div style=""margin: 30px 0; padding: 20px; background-color: #d1fae5; border-radius: 8px;"">
                                <h3 style=""color: #065f46; margin: 0 0 15px;"">✅ Ce que vous pouvez faire maintenant :</h3>
                                <ul style=""color: #047857; margin: 0; padding-left: 20px;"">
                                    <li style=""margin-bottom: 8px;"">Prendre des rendez-vous avec nos médecins</li>
                                    <li style=""margin-bottom: 8px;"">Consulter votre dossier médical</li>
                                    <li style=""margin-bottom: 8px;"">Gérer vos informations personnelles</li>
                                </ul>
                            </div>
                            <div style=""text-align: center; margin: 35px 0;"">
                                <a href=""{dashboardLink}"" 
                                   style=""background: linear-gradient(135deg, #0e7490 0%, #0891b2 100%); 
                                          color: #ffffff; 
                                          text-decoration: none; 
                                          padding: 16px 40px; 
                                          border-radius: 8px; 
                                          font-size: 16px; 
                                          font-weight: 600;
                                          display: inline-block;"">
                                    🚀 Accéder à mon espace
                                </a>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f9fafb; padding: 25px 30px; text-align: center; border-radius: 0 0 12px 12px;"">
                            <p style=""color: #9ca3af; font-size: 13px; margin: 0;"">© {DateTime.Now.Year} MediConnect</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    #endregion

    #region Coordination Intervention Templates

    /// <inheritdoc />
    public async Task<bool> SendCoordinationDemandeAsync(string toEmail, string nomAnesthesiste, string nomChirurgien,
        string nomPatient, string dateIntervention, string heureIntervention, string indication)
    {
        var subject = "🔔 Nouvelle demande de coordination d'intervention - MediConnect";
        var htmlBody = GetCoordinationDemandeTemplate(nomAnesthesiste, nomChirurgien, nomPatient, 
            dateIntervention, heureIntervention, indication);
        return await SendEmailAsync(toEmail, subject, htmlBody);
    }

    /// <inheritdoc />
    public async Task<bool> SendCoordinationValideeAsync(string toEmail, string nomChirurgien, string nomAnesthesiste,
        string nomPatient, string dateIntervention, string heureIntervention)
    {
        var subject = "✅ Coordination validée - Intervention confirmée - MediConnect";
        var htmlBody = GetCoordinationValideeTemplate(nomChirurgien, nomAnesthesiste, nomPatient,
            dateIntervention, heureIntervention);
        return await SendEmailAsync(toEmail, subject, htmlBody);
    }

    /// <inheritdoc />
    public async Task<bool> SendCoordinationModifieeAsync(string toEmail, string nomChirurgien, string nomAnesthesiste,
        string nomPatient, string nouvelleDateIntervention, string nouvelleHeureIntervention, string commentaire)
    {
        var subject = "📝 Contre-proposition de l'anesthésiste - MediConnect";
        var htmlBody = GetCoordinationModifieeTemplate(nomChirurgien, nomAnesthesiste, nomPatient,
            nouvelleDateIntervention, nouvelleHeureIntervention, commentaire);
        return await SendEmailAsync(toEmail, subject, htmlBody);
    }

    /// <inheritdoc />
    public async Task<bool> SendCoordinationRefuseeAsync(string toEmail, string nomChirurgien, string nomAnesthesiste,
        string nomPatient, string motifRefus)
    {
        var subject = "❌ Coordination refusée - MediConnect";
        var htmlBody = GetCoordinationRefuseeTemplate(nomChirurgien, nomAnesthesiste, nomPatient, motifRefus);
        return await SendEmailAsync(toEmail, subject, htmlBody);
    }

    /// <inheritdoc />
    public async Task<bool> SendInterventionPlanifieePatientAsync(string toEmail, string nomPatient, string nomChirurgien,
        string nomAnesthesiste, string dateIntervention, string heureIntervention, string? dateRdvPreop)
    {
        var subject = "📅 Votre intervention chirurgicale est programmée - MediConnect";
        var htmlBody = GetInterventionPlanifieePatientTemplate(nomPatient, nomChirurgien, nomAnesthesiste,
            dateIntervention, heureIntervention, dateRdvPreop);
        return await SendEmailAsync(toEmail, subject, htmlBody);
    }

    private string GetCoordinationDemandeTemplate(string nomAnesthesiste, string nomChirurgien,
        string nomPatient, string dateIntervention, string heureIntervention, string indication)
    {
        var dashboardLink = $"{_appSettings.FrontendUrl}/medecin/interventions";
        return $@"
<!DOCTYPE html>
<html lang=""fr"">
<head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""></head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7fa;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); padding: 30px; text-align: center; border-radius: 12px 12px 0 0;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 24px;"">🔔 Nouvelle demande de coordination</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #1f2937; margin: 0 0 20px;"">Bonjour Dr. {nomAnesthesiste},</h2>
                            <p style=""color: #4b5563; font-size: 16px; line-height: 1.6;"">
                                Le <strong>Dr. {nomChirurgien}</strong> vous sollicite pour une intervention chirurgicale.
                            </p>
                            
                            <div style=""margin: 25px 0; padding: 20px; background-color: #fef3c7; border-radius: 8px; border-left: 4px solid #f59e0b;"">
                                <h3 style=""color: #92400e; margin: 0 0 15px;"">📋 Détails de l'intervention</h3>
                                <table style=""width: 100%; color: #78350f;"">
                                    <tr><td style=""padding: 5px 0;""><strong>Patient:</strong></td><td>{nomPatient}</td></tr>
                                    <tr><td style=""padding: 5px 0;""><strong>Date proposée:</strong></td><td>{dateIntervention}</td></tr>
                                    <tr><td style=""padding: 5px 0;""><strong>Heure:</strong></td><td>{heureIntervention}</td></tr>
                                    <tr><td style=""padding: 5px 0;""><strong>Indication:</strong></td><td>{indication}</td></tr>
                                </table>
                            </div>
                            
                            <p style=""color: #4b5563; font-size: 16px; line-height: 1.6;"">
                                Veuillez vous connecter à votre espace pour <strong>accepter</strong>, <strong>proposer une modification</strong> ou <strong>refuser</strong> cette demande.
                            </p>
                            
                            <div style=""text-align: center; margin: 35px 0;"">
                                <a href=""{dashboardLink}"" style=""background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); color: #ffffff; text-decoration: none; padding: 16px 40px; border-radius: 8px; font-size: 16px; font-weight: 600; display: inline-block;"">
                                    📋 Voir la demande
                                </a>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f9fafb; padding: 25px 30px; text-align: center; border-radius: 0 0 12px 12px;"">
                            <p style=""color: #9ca3af; font-size: 13px; margin: 0;"">© {DateTime.Now.Year} MediConnect</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private string GetCoordinationValideeTemplate(string nomChirurgien, string nomAnesthesiste,
        string nomPatient, string dateIntervention, string heureIntervention)
    {
        return $@"
<!DOCTYPE html>
<html lang=""fr"">
<head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""></head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7fa;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #059669 0%, #10b981 100%); padding: 30px; text-align: center; border-radius: 12px 12px 0 0;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 24px;"">✅ Coordination validée</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #1f2937; margin: 0 0 20px;"">Bonjour Dr. {nomChirurgien},</h2>
                            <p style=""color: #4b5563; font-size: 16px; line-height: 1.6;"">
                                Excellente nouvelle ! Le <strong>Dr. {nomAnesthesiste}</strong> a validé votre demande de coordination.
                            </p>
                            
                            <div style=""margin: 25px 0; padding: 20px; background-color: #d1fae5; border-radius: 8px; border-left: 4px solid #10b981;"">
                                <h3 style=""color: #065f46; margin: 0 0 15px;"">📅 Intervention confirmée</h3>
                                <table style=""width: 100%; color: #047857;"">
                                    <tr><td style=""padding: 5px 0;""><strong>Patient:</strong></td><td>{nomPatient}</td></tr>
                                    <tr><td style=""padding: 5px 0;""><strong>Date:</strong></td><td>{dateIntervention}</td></tr>
                                    <tr><td style=""padding: 5px 0;""><strong>Heure:</strong></td><td>{heureIntervention}</td></tr>
                                    <tr><td style=""padding: 5px 0;""><strong>Anesthésiste:</strong></td><td>Dr. {nomAnesthesiste}</td></tr>
                                </table>
                            </div>
                            
                            <p style=""color: #4b5563; font-size: 16px; line-height: 1.6;"">
                                L'intervention est maintenant programmée. Le patient sera notifié automatiquement.
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f9fafb; padding: 25px 30px; text-align: center; border-radius: 0 0 12px 12px;"">
                            <p style=""color: #9ca3af; font-size: 13px; margin: 0;"">© {DateTime.Now.Year} MediConnect</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private string GetCoordinationModifieeTemplate(string nomChirurgien, string nomAnesthesiste,
        string nomPatient, string nouvelleDateIntervention, string nouvelleHeureIntervention, string commentaire)
    {
        var dashboardLink = $"{_appSettings.FrontendUrl}/medecin/consultations";
        return $@"
<!DOCTYPE html>
<html lang=""fr"">
<head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""></head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7fa;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%); padding: 30px; text-align: center; border-radius: 12px 12px 0 0;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 24px;"">📝 Contre-proposition</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #1f2937; margin: 0 0 20px;"">Bonjour Dr. {nomChirurgien},</h2>
                            <p style=""color: #4b5563; font-size: 16px; line-height: 1.6;"">
                                Le <strong>Dr. {nomAnesthesiste}</strong> propose une modification du créneau pour l'intervention de <strong>{nomPatient}</strong>.
                            </p>
                            
                            <div style=""margin: 25px 0; padding: 20px; background-color: #dbeafe; border-radius: 8px; border-left: 4px solid #3b82f6;"">
                                <h3 style=""color: #1e40af; margin: 0 0 15px;"">📅 Nouveau créneau proposé</h3>
                                <table style=""width: 100%; color: #1e3a8a;"">
                                    <tr><td style=""padding: 5px 0;""><strong>Date:</strong></td><td>{nouvelleDateIntervention}</td></tr>
                                    <tr><td style=""padding: 5px 0;""><strong>Heure:</strong></td><td>{nouvelleHeureIntervention}</td></tr>
                                </table>
                            </div>
                            
                            <div style=""margin: 25px 0; padding: 15px; background-color: #f3f4f6; border-radius: 8px;"">
                                <p style=""color: #374151; font-size: 14px; margin: 0;""><strong>Commentaire:</strong><br>{commentaire}</p>
                            </div>
                            
                            <div style=""text-align: center; margin: 35px 0;"">
                                <a href=""{dashboardLink}"" style=""background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%); color: #ffffff; text-decoration: none; padding: 16px 40px; border-radius: 8px; font-size: 16px; font-weight: 600; display: inline-block;"">
                                    Voir la contre-proposition
                                </a>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f9fafb; padding: 25px 30px; text-align: center; border-radius: 0 0 12px 12px;"">
                            <p style=""color: #9ca3af; font-size: 13px; margin: 0;"">© {DateTime.Now.Year} MediConnect</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private string GetCoordinationRefuseeTemplate(string nomChirurgien, string nomAnesthesiste,
        string nomPatient, string motifRefus)
    {
        return $@"
<!DOCTYPE html>
<html lang=""fr"">
<head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""></head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7fa;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #dc2626 0%, #ef4444 100%); padding: 30px; text-align: center; border-radius: 12px 12px 0 0;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 24px;"">❌ Coordination refusée</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #1f2937; margin: 0 0 20px;"">Bonjour Dr. {nomChirurgien},</h2>
                            <p style=""color: #4b5563; font-size: 16px; line-height: 1.6;"">
                                Le <strong>Dr. {nomAnesthesiste}</strong> ne peut malheureusement pas participer à l'intervention prévue pour <strong>{nomPatient}</strong>.
                            </p>
                            
                            <div style=""margin: 25px 0; padding: 20px; background-color: #fee2e2; border-radius: 8px; border-left: 4px solid #dc2626;"">
                                <h3 style=""color: #991b1b; margin: 0 0 15px;"">📋 Motif du refus</h3>
                                <p style=""color: #7f1d1d; margin: 0;"">{motifRefus}</p>
                            </div>
                            
                            <p style=""color: #4b5563; font-size: 16px; line-height: 1.6;"">
                                Vous pouvez sélectionner un autre anesthésiste disponible pour cette intervention.
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f9fafb; padding: 25px 30px; text-align: center; border-radius: 0 0 12px 12px;"">
                            <p style=""color: #9ca3af; font-size: 13px; margin: 0;"">© {DateTime.Now.Year} MediConnect</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private string GetInterventionPlanifieePatientTemplate(string nomPatient, string nomChirurgien,
        string nomAnesthesiste, string dateIntervention, string heureIntervention, string? dateRdvPreop)
    {
        var rdvSection = !string.IsNullOrEmpty(dateRdvPreop) ? $@"
                            <div style=""margin: 25px 0; padding: 20px; background-color: #fef3c7; border-radius: 8px; border-left: 4px solid #f59e0b;"">
                                <h3 style=""color: #92400e; margin: 0 0 10px;"">📋 Consultation pré-opératoire</h3>
                                <p style=""color: #78350f; margin: 0;"">Un rendez-vous de consultation pré-opératoire est prévu le <strong>{dateRdvPreop}</strong> avec l'anesthésiste.</p>
                            </div>" : "";

        return $@"
<!DOCTYPE html>
<html lang=""fr"">
<head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""></head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7fa;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #0e7490 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 12px 12px 0 0;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 24px;"">📅 Intervention programmée</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #1f2937; margin: 0 0 20px;"">Bonjour {nomPatient},</h2>
                            <p style=""color: #4b5563; font-size: 16px; line-height: 1.6;"">
                                Votre intervention chirurgicale a été confirmée et programmée.
                            </p>
                            
                            <div style=""margin: 25px 0; padding: 20px; background-color: #d1fae5; border-radius: 8px; border-left: 4px solid #10b981;"">
                                <h3 style=""color: #065f46; margin: 0 0 15px;"">🏥 Détails de l'intervention</h3>
                                <table style=""width: 100%; color: #047857;"">
                                    <tr><td style=""padding: 5px 0;""><strong>Date:</strong></td><td>{dateIntervention}</td></tr>
                                    <tr><td style=""padding: 5px 0;""><strong>Heure:</strong></td><td>{heureIntervention}</td></tr>
                                    <tr><td style=""padding: 5px 0;""><strong>Chirurgien:</strong></td><td>Dr. {nomChirurgien}</td></tr>
                                    <tr><td style=""padding: 5px 0;""><strong>Anesthésiste:</strong></td><td>Dr. {nomAnesthesiste}</td></tr>
                                </table>
                            </div>
                            {rdvSection}
                            <div style=""margin: 25px 0; padding: 15px; background-color: #f3f4f6; border-radius: 8px;"">
                                <p style=""color: #374151; font-size: 14px; margin: 0;"">
                                    <strong>⚠️ Important:</strong> Veuillez vous présenter à jeun le jour de l'intervention. 
                                    En cas d'empêchement, contactez-nous au plus vite.
                                </p>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f9fafb; padding: 25px 30px; text-align: center; border-radius: 0 0 12px 12px;"">
                            <p style=""color: #9ca3af; font-size: 13px; margin: 0;"">© {DateTime.Now.Year} MediConnect</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    /// <summary>
    /// Envoie une confirmation d'intervention à l'anesthésiste
    /// </summary>
    public async Task<bool> SendInterventionConfirmeeAnesthesisteAsync(string toEmail, string nomAnesthesiste, 
        string nomChirurgien, string nomPatient, string dateIntervention, string heureIntervention, string? dateRdvPreop)
    {
        var subject = $"✅ Intervention confirmée - {nomPatient} le {dateIntervention}";
        var htmlBody = GetInterventionConfirmeeAnesthesisteTemplate(nomAnesthesiste, nomChirurgien, nomPatient, 
            dateIntervention, heureIntervention, dateRdvPreop);
        return await SendEmailAsync(toEmail, subject, htmlBody);
    }

    private string GetInterventionConfirmeeAnesthesisteTemplate(string nomAnesthesiste, string nomChirurgien, 
        string nomPatient, string dateIntervention, string heureIntervention, string? dateRdvPreop)
    {
        var rdvSection = !string.IsNullOrEmpty(dateRdvPreop) ? 
            $@"<div style=""margin: 20px 0; padding: 15px; background-color: #fef3c7; border-radius: 8px; border-left: 4px solid #f59e0b;"">
                <h3 style=""color: #92400e; margin: 0 0 10px;"">📋 Consultation pré-opératoire</h3>
                <p style=""color: #78350f; margin: 0;"">Un rendez-vous de consultation pré-opératoire est programmé le <strong>{dateRdvPreop}</strong> avec le patient.</p>
            </div>" : "";

        return $@"
<!DOCTYPE html>
<html lang=""fr"">
<head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""></head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7fa;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 30px; text-align: center; border-radius: 12px 12px 0 0;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 24px;"">✅ Intervention confirmée</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #1f2937; margin: 0 0 20px;"">Bonjour Dr. {nomAnesthesiste},</h2>
                            <p style=""color: #4b5563; font-size: 16px; line-height: 1.6;"">
                                L'intervention chirurgicale a été confirmée par le chirurgien. Votre participation est validée.
                            </p>
                            
                            <div style=""margin: 25px 0; padding: 20px; background-color: #d1fae5; border-radius: 8px; border-left: 4px solid #10b981;"">
                                <h3 style=""color: #065f46; margin: 0 0 15px;"">🏥 Détails de l'intervention</h3>
                                <table style=""width: 100%; color: #047857;"">
                                    <tr><td style=""padding: 5px 0;""><strong>Patient:</strong></td><td>{nomPatient}</td></tr>
                                    <tr><td style=""padding: 5px 0;""><strong>Date:</strong></td><td>{dateIntervention}</td></tr>
                                    <tr><td style=""padding: 5px 0;""><strong>Heure:</strong></td><td>{heureIntervention}</td></tr>
                                    <tr><td style=""padding: 5px 0;""><strong>Chirurgien:</strong></td><td>Dr. {nomChirurgien}</td></tr>
                                </table>
                            </div>
                            {rdvSection}
                            <p style=""color: #4b5563; font-size: 14px; line-height: 1.6;"">
                                Vous pouvez consulter les détails complets de l'intervention dans votre espace MediConnect.
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f9fafb; padding: 25px 30px; text-align: center; border-radius: 0 0 12px 12px;"">
                            <p style=""color: #9ca3af; font-size: 13px; margin: 0;"">© {DateTime.Now.Year} MediConnect</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    #endregion

    /// <summary>
    /// Supprime les tags HTML pour créer une version texte
    /// </summary>
    private static string StripHtml(string html)
    {
        return System.Text.RegularExpressions.Regex.Replace(
                html,
                "<[^>]*>",
                " ",
                System.Text.RegularExpressions.RegexOptions.None,
                TimeSpan.FromMilliseconds(200))
            .Replace("&nbsp;", " ")
            .Replace("  ", " ")
            .Trim();
    }
}
