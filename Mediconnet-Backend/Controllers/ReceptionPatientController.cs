using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mediconnet_Backend.Controllers.Base;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.DTOs.Accueil;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// Contrôleur pour la gestion des patients créés par l'accueil
/// </summary>
[ApiController]
[Route("api/reception")]
public class ReceptionPatientController : BaseApiController
{
    private readonly IReceptionPatientService _receptionPatientService;
    private readonly ILogger<ReceptionPatientController> _logger;

    public ReceptionPatientController(
        IReceptionPatientService receptionPatientService,
        ILogger<ReceptionPatientController> logger)
    {
        _receptionPatientService = receptionPatientService;
        _logger = logger;
    }

    /// <summary>
    /// Crée un nouveau patient avec toutes ses informations
    /// Génère un mot de passe temporaire et retourne les instructions de connexion
    /// </summary>
    [HttpPost("patients")]
    [Authorize(Roles = "accueil,administrateur")]
    public async Task<IActionResult> CreatePatient([FromBody] CreatePatientByReceptionRequest request)
    {
        var authCheck = CheckAuthentication();
        if (authCheck != null) return authCheck;

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Utilisateur non authentifié" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new CreatePatientByReceptionResponse
            {
                Success = false,
                Message = "Données invalides"
            });
        }

        var result = await _receptionPatientService.CreatePatientAsync(request, userId.Value);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        _logger.LogInformation($"Patient créé par accueil: {result.NumeroDossier}");
        return Ok(result);
    }

    /// <summary>
    /// Récupère les informations du patient connecté pour la page de première connexion
    /// </summary>
    [HttpGet("first-login/info")]
    [Authorize(Roles = "patient")]
    public async Task<IActionResult> GetFirstLoginInfo()
    {
        var authCheck = CheckAuthentication();
        if (authCheck != null) return authCheck;

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Utilisateur non authentifié" });
        }

        var result = await _receptionPatientService.GetFirstLoginInfoAsync(userId.Value);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Valide la première connexion du patient
    /// (déclaration sur l'honneur + changement de mot de passe)
    /// </summary>
    [HttpPost("first-login/validate")]
    [Authorize(Roles = "patient")]
    public async Task<IActionResult> ValidateFirstLogin([FromBody] FirstLoginValidationRequest request)
    {
        var authCheck = CheckAuthentication();
        if (authCheck != null) return authCheck;

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Utilisateur non authentifié" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new FirstLoginValidationResponse
            {
                Success = false,
                Message = "Données invalides"
            });
        }

        var result = await _receptionPatientService.ValidateFirstLoginAsync(userId.Value, request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        _logger.LogInformation($"Première connexion validée pour utilisateur {userId}");
        return Ok(result);
    }

    /// <summary>
    /// Accepte uniquement la déclaration sur l'honneur (sans changement de mot de passe)
    /// </summary>
    [HttpPost("first-login/accept-declaration")]
    [Authorize(Roles = "patient")]
    public async Task<IActionResult> AcceptDeclaration([FromBody] AcceptDeclarationRequest request)
    {
        var authCheck = CheckAuthentication();
        if (authCheck != null) return authCheck;

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Utilisateur non authentifié" });
        }

        var result = await _receptionPatientService.AcceptDeclarationAsync(userId.Value, request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        _logger.LogInformation($"Déclaration acceptée pour utilisateur {userId}");
        return Ok(result);
    }

    /// <summary>
    /// Vérifie si le patient connecté doit compléter sa première connexion
    /// </summary>
    [HttpGet("first-login/check")]
    [Authorize(Roles = "patient")]
    public async Task<IActionResult> CheckFirstLoginRequired()
    {
        var authCheck = CheckAuthentication();
        if (authCheck != null) return authCheck;

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Utilisateur non authentifié" });
        }

        var requiresFirstLogin = await _receptionPatientService.RequiresFirstLoginValidationAsync(userId.Value);

        return Ok(new { requiresFirstLogin });
    }
}
