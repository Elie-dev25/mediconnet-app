using Mediconnet_Backend.Controllers.Base;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.DTOs.Admin;
using Mediconnet_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// Contrôleur pour la gestion des affectations de service (médecins et infirmiers)
/// </summary>
[Route("api/affectations-service")]
[ApiController]
[Authorize]
public class AffectationServiceController : BaseAdminController
{
    private readonly IAffectationServiceService _affectationService;
    private readonly ILogger<AffectationServiceController> _logger;

    public AffectationServiceController(
        IAffectationServiceService affectationService,
        ILogger<AffectationServiceController> logger)
    {
        _affectationService = affectationService;
        _logger = logger;
    }

    /// <summary>
    /// Récupère l'historique des affectations d'un médecin
    /// </summary>
    [HttpGet("medecin/{userId}/historique")]
    public async Task<IActionResult> GetHistoriqueMedecin(int userId)
    {
        try
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            var historique = await _affectationService.GetHistoriqueAffectationsAsync(userId, TypeUserAffectation.Medecin);
            if (historique == null)
                return NotFound(new { message = "Médecin non trouvé" });

            return Ok(historique);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de l'historique du médecin {UserId}", userId);
            return StatusCode(500, new { message = "Erreur lors de la récupération de l'historique" });
        }
    }

    /// <summary>
    /// Récupère l'historique des affectations d'un infirmier
    /// </summary>
    [HttpGet("infirmier/{userId}/historique")]
    public async Task<IActionResult> GetHistoriqueInfirmier(int userId)
    {
        try
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            var historique = await _affectationService.GetHistoriqueAffectationsAsync(userId, TypeUserAffectation.Infirmier);
            if (historique == null)
                return NotFound(new { message = "Infirmier non trouvé" });

            return Ok(historique);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de l'historique de l'infirmier {UserId}", userId);
            return StatusCode(500, new { message = "Erreur lors de la récupération de l'historique" });
        }
    }

    /// <summary>
    /// Change le service d'un médecin
    /// </summary>
    [HttpPut("medecin/{userId}/changer-service")]
    public async Task<IActionResult> ChangerServiceMedecin(int userId, [FromBody] ChangerServiceRequest request)
    {
        try
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminId = GetCurrentUserId();
            if (adminId == null)
                return Unauthorized(new { message = "Utilisateur non authentifié" });

            var result = await _affectationService.ChangerServiceAsync(
                userId, 
                TypeUserAffectation.Medecin, 
                request, 
                adminId.Value);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du changement de service du médecin {UserId}", userId);
            return StatusCode(500, new { message = "Erreur lors du changement de service" });
        }
    }

    /// <summary>
    /// Change le service d'un infirmier
    /// </summary>
    [HttpPut("infirmier/{userId}/changer-service")]
    public async Task<IActionResult> ChangerServiceInfirmier(int userId, [FromBody] ChangerServiceRequest request)
    {
        try
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminId = GetCurrentUserId();
            if (adminId == null)
                return Unauthorized(new { message = "Utilisateur non authentifié" });

            var result = await _affectationService.ChangerServiceAsync(
                userId, 
                TypeUserAffectation.Infirmier, 
                request, 
                adminId.Value);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du changement de service de l'infirmier {UserId}", userId);
            return StatusCode(500, new { message = "Erreur lors du changement de service" });
        }
    }

    /// <summary>
    /// Récupère toutes les affectations actives d'un service
    /// </summary>
    [HttpGet("service/{serviceId}")]
    public async Task<IActionResult> GetAffectationsParService(int serviceId)
    {
        try
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            var affectations = await _affectationService.GetAffectationsParServiceAsync(serviceId);
            return Ok(affectations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des affectations du service {ServiceId}", serviceId);
            return StatusCode(500, new { message = "Erreur lors de la récupération des affectations" });
        }
    }
}
