using MailKit.Net.Smtp;
using MimeKit;
using Mediconnet_Backend.Core.Entities;

namespace Mediconnet_Backend.Services;

public interface IFactureEmailService
{
    Task<bool> EnvoyerFactureAssuranceAsync(Facture facture, byte[] pdfContent);
}

public class FactureEmailService : IFactureEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FactureEmailService> _logger;

    public FactureEmailService(IConfiguration configuration, ILogger<FactureEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> EnvoyerFactureAssuranceAsync(Facture facture, byte[] pdfContent)
    {
        if (facture.Assurance == null || string.IsNullOrEmpty(facture.Assurance.EmailFacturation))
        {
            _logger.LogWarning("Impossible d'envoyer la facture {NumeroFacture}: email assurance manquant", facture.NumeroFacture);
            return false;
        }

        try
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var host = smtpSettings["Host"] ?? "smtp.gmail.com";
            var port = int.Parse(smtpSettings["Port"] ?? "587");
            var username = smtpSettings["Username"] ?? "";
            var password = smtpSettings["Password"] ?? "";
            var fromEmail = smtpSettings["FromEmail"] ?? "noreply@mediconnect.cm";
            var fromName = smtpSettings["FromName"] ?? "MediConnect - Facturation";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress(facture.Assurance.Nom, facture.Assurance.EmailFacturation));
            message.Subject = $"Facture {facture.NumeroFacture} - Patient {facture.Patient?.Utilisateur?.Nom} {facture.Patient?.Utilisateur?.Prenom}";

            var builder = new BodyBuilder();

            // Corps de l'email en HTML
            builder.HtmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .header {{ background-color: #1a56db; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .info-box {{ background-color: #f3f4f6; padding: 15px; border-radius: 8px; margin: 15px 0; }}
        .amount {{ font-size: 24px; color: #1a56db; font-weight: bold; }}
        .footer {{ background-color: #f9fafb; padding: 15px; text-align: center; font-size: 12px; color: #6b7280; }}
        table {{ width: 100%; border-collapse: collapse; margin: 15px 0; }}
        th, td {{ padding: 10px; text-align: left; border-bottom: 1px solid #e5e7eb; }}
        th {{ background-color: #f3f4f6; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>MediConnect</h1>
        <p>Facture Assurance</p>
    </div>
    
    <div class='content'>
        <p>Bonjour,</p>
        
        <p>Veuillez trouver ci-joint la facture <strong>{facture.NumeroFacture}</strong> concernant les soins prodigués à votre assuré(e).</p>
        
        <div class='info-box'>
            <h3>Informations Patient</h3>
            <table>
                <tr><td><strong>Nom:</strong></td><td>{facture.Patient?.Utilisateur?.Nom} {facture.Patient?.Utilisateur?.Prenom}</td></tr>
                <tr><td><strong>N° Carte Assurance:</strong></td><td>{facture.Patient?.NumeroCarteAssurance ?? "N/A"}</td></tr>
                <tr><td><strong>Date des soins:</strong></td><td>{facture.DateFacture:dd/MM/yyyy}</td></tr>
                <tr><td><strong>Type de prestation:</strong></td><td>{GetTypeLabel(facture.TypeFacture)}</td></tr>
            </table>
        </div>
        
        <div class='info-box'>
            <h3>Détails Financiers</h3>
            <table>
                <tr><td><strong>Montant total des soins:</strong></td><td>{facture.MontantTotal:N0} FCFA</td></tr>
                <tr><td><strong>Taux de couverture:</strong></td><td>{facture.TauxCouverture ?? 0:N0}%</td></tr>
                <tr><td><strong>Part patient:</strong></td><td>{facture.MontantPatient ?? 0:N0} FCFA</td></tr>
                <tr style='background-color: #dbeafe;'>
                    <td><strong>Montant à régler:</strong></td>
                    <td class='amount'>{facture.MontantAssurance ?? 0:N0} FCFA</td>
                </tr>
            </table>
        </div>
        
        <p>Merci de procéder au règlement dans les <strong>30 jours</strong> suivant la réception de cette facture.</p>
        
        <p><strong>Référence à mentionner:</strong> {facture.NumeroFacture}</p>
        
        <p>Pour toute question, n'hésitez pas à nous contacter.</p>
        
        <p>Cordialement,<br/>
        <strong>Service Facturation</strong><br/>
        MediConnect</p>
    </div>
    
    <div class='footer'>
        <p>Ce message a été envoyé automatiquement par le système MediConnect.</p>
        <p>© {DateTime.Now.Year} MediConnect - Tous droits réservés</p>
    </div>
</body>
</html>";

            // Version texte simple
            builder.TextBody = $@"
MediConnect - Facture Assurance

Bonjour,

Veuillez trouver ci-joint la facture {facture.NumeroFacture} concernant les soins prodigués à votre assuré(e).

INFORMATIONS PATIENT
- Nom: {facture.Patient?.Utilisateur?.Nom} {facture.Patient?.Utilisateur?.Prenom}
- N° Carte Assurance: {facture.Patient?.NumeroCarteAssurance ?? "N/A"}
- Date des soins: {facture.DateFacture:dd/MM/yyyy}
- Type de prestation: {GetTypeLabel(facture.TypeFacture)}

DÉTAILS FINANCIERS
- Montant total des soins: {facture.MontantTotal:N0} FCFA
- Taux de couverture: {facture.TauxCouverture ?? 0:N0}%
- Part patient: {facture.MontantPatient ?? 0:N0} FCFA
- MONTANT À RÉGLER: {facture.MontantAssurance ?? 0:N0} FCFA

Merci de procéder au règlement dans les 30 jours suivant la réception de cette facture.

Référence à mentionner: {facture.NumeroFacture}

Cordialement,
Service Facturation
MediConnect
";

            // Pièce jointe PDF
            builder.Attachments.Add($"Facture_{facture.NumeroFacture}.pdf", pdfContent, new ContentType("application", "pdf"));

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
            
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                await client.AuthenticateAsync(username, password);
            }
            
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Facture {NumeroFacture} envoyée avec succès à {Email}", 
                facture.NumeroFacture, facture.Assurance.EmailFacturation);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'envoi de la facture {NumeroFacture} à {Email}", 
                facture.NumeroFacture, facture.Assurance?.EmailFacturation);
            return false;
        }
    }

    private string GetTypeLabel(string? type) => type?.ToLower() switch
    {
        "consultation" => "Consultation médicale",
        "hospitalisation" => "Hospitalisation",
        "examen" => "Examens médicaux",
        "pharmacie" => "Pharmacie / Médicaments",
        _ => type ?? "Soins médicaux"
    };
}
