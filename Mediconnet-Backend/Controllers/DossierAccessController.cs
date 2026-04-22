using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Controllers.Base;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Medecin;
using Mediconnet_Backend.Core.Interfaces.Services;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// Contrôleur pour la validation d'accès au dossier patient par code email
/// </summary>
[Route("api/medecin/dossier-access")]
[Authorize(Roles = "medecin")]
public class DossierAccessController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<DossierAccessController> _logger;
    
    // Stockage temporaire des codes (en production, utiliser Redis ou BD)
    private static readonly ConcurrentDictionary<string, (string Code, DateTime ExpiresAt, int MedecinId)> _validationCodes = new();

    public DossierAccessController(
        ApplicationDbContext context, 
        IEmailService emailService,
        ILogger<DossierAccessController> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Envoie un code de validation à 5 chiffres par email au patient
    /// </summary>
    [HttpPost("send-code")]
    public async Task<IActionResult> SendValidationCode([FromBody] SendCodeRequest request)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            // Vérifier que le patient existe et que le médecin a accès
            var patient = await _context.Patients
                .Include(p => p.Utilisateur)
                .FirstOrDefaultAsync(p => p.IdUser == request.IdPatient);

            if (patient == null)
                return NotFound(new SendCodeResponse { Success = false, Message = "Patient non trouvé" });

            // Vérifier que le médecin a déjà eu un RDV avec ce patient
            var hasRdv = await _context.RendezVous
                .AnyAsync(r => r.IdMedecin == medecinId.Value && r.IdPatient == request.IdPatient);

            if (!hasRdv)
                return BadRequest(new SendCodeResponse { Success = false, Message = "Vous n'êtes pas autorisé à accéder à ce dossier" });

            var email = patient.Utilisateur?.Email;
            if (string.IsNullOrEmpty(email))
                return BadRequest(new SendCodeResponse { Success = false, Message = "Le patient n'a pas d'adresse email enregistrée" });

            // Générer un code à 5 chiffres avec un RNG cryptographique
            var code = RandomNumberGenerator.GetInt32(10000, 100000).ToString();
            var expiresAt = DateTime.UtcNow.AddMinutes(10);

            // Stocker le code
            var key = $"{medecinId}_{request.IdPatient}";
            _validationCodes[key] = (code, expiresAt, medecinId.Value);

            // Envoyer l'email
            var patientName = $"{patient.Utilisateur?.Prenom} {patient.Utilisateur?.Nom}";
            var medecinInfo = await _context.Medecins
                .Include(m => m.Utilisateur)
                .FirstOrDefaultAsync(m => m.IdUser == medecinId.Value);
            var medecinName = medecinInfo?.Utilisateur != null 
                ? $"Dr. {medecinInfo.Utilisateur.Prenom} {medecinInfo.Utilisateur.Nom}" 
                : "Votre médecin";

            var emailBody = GenerateCodeEmailBody(patientName, medecinName, code);
            
            await _emailService.SendEmailAsync(
                email,
                "Code de validation - Accès à votre dossier médical",
                emailBody
            );

            _logger.LogInformation("Code de validation envoyé au patient {IdPatient} par le médecin {MedecinId}", request.IdPatient, medecinId);

            return Ok(new SendCodeResponse 
            { 
                Success = true, 
                Message = "Code envoyé avec succès",
                ExpiresAt = expiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur SendValidationCode");
            return StatusCode(500, new SendCodeResponse { Success = false, Message = "Erreur lors de l'envoi du code" });
        }
    }

    /// <summary>
    /// Vérifie le code saisi par le médecin
    /// </summary>
    [HttpPost("verify-code")]
    public async Task<IActionResult> VerifyCode([FromBody] VerifyCodeRequest request)
    {
        try
        {
            var medecinId = GetCurrentUserId();
            if (!medecinId.HasValue) return Unauthorized();

            var key = $"{medecinId}_{request.IdPatient}";

            if (!_validationCodes.TryGetValue(key, out var storedData))
            {
                return BadRequest(new VerifyCodeResponse { Success = false, Message = "Aucun code en attente. Veuillez en demander un nouveau." });
            }

            // Vérifier l'expiration
            if (DateTime.UtcNow > storedData.ExpiresAt)
            {
                _validationCodes.TryRemove(key, out _);
                return BadRequest(new VerifyCodeResponse { Success = false, Message = "Le code a expiré. Veuillez en demander un nouveau." });
            }

            // Vérifier le code
            if (storedData.Code != request.Code)
            {
                return BadRequest(new VerifyCodeResponse { Success = false, Message = "Code incorrect" });
            }

            // Code valide - supprimer et autoriser l'accès
            _validationCodes.TryRemove(key, out _);

            _logger.LogInformation("Accès au dossier patient {IdPatient} autorisé pour le médecin {MedecinId}", request.IdPatient, medecinId);

            return Ok(new VerifyCodeResponse 
            { 
                Success = true, 
                Message = "Code validé avec succès"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur VerifyCode");
            return StatusCode(500, new VerifyCodeResponse { Success = false, Message = "Erreur lors de la vérification" });
        }
    }

    private string GenerateCodeEmailBody(string patientName, string medecinName, string code)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #7c3aed 0%, #5b21b6 100%); color: white; padding: 30px; border-radius: 12px 12px 0 0; text-align: center; }}
        .content {{ background: #f9fafb; padding: 30px; border-radius: 0 0 12px 12px; }}
        .code-box {{ background: white; border: 2px dashed #7c3aed; border-radius: 12px; padding: 20px; text-align: center; margin: 20px 0; }}
        .code {{ font-size: 36px; font-weight: bold; color: #7c3aed; letter-spacing: 8px; }}
        .warning {{ background: #fef3c7; border-left: 4px solid #f59e0b; padding: 12px; margin-top: 20px; border-radius: 4px; }}
        .footer {{ text-align: center; margin-top: 20px; color: #6b7280; font-size: 14px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🔐 Code de validation</h1>
        </div>
        <div class=""content"">
            <p>Bonjour <strong>{patientName}</strong>,</p>
            <p><strong>{medecinName}</strong> souhaite accéder à votre dossier médical. Pour autoriser cet accès, veuillez lui communiquer le code suivant :</p>
            
            <div class=""code-box"">
                <div class=""code"">{code}</div>
                <p style=""color: #6b7280; margin-top: 10px; font-size: 14px;"">Ce code expire dans 10 minutes</p>
            </div>
            
            <div class=""warning"">
                <strong>⚠️ Important :</strong> Ne communiquez ce code qu'à votre médecin traitant. Ne le partagez jamais par email ou SMS.
            </div>
            
            <div class=""footer"">
                <p>Si vous n'avez pas sollicité cet accès, ignorez ce message.</p>
                <p>— L'équipe MediConnet</p>
            </div>
        </div>
    </div>
</body>
</html>";
    }
}
