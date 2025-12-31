using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.DTOs.Consultation;
using System.Security.Claims;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// Controller pour la gestion des paramètres vitaux
/// Routes protégées par rôle
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ParametreController : ControllerBase
{
    private readonly IParametreService _parametreService;
    private readonly ILogger<ParametreController> _logger;

    public ParametreController(IParametreService parametreService, ILogger<ParametreController> logger)
    {
        _parametreService = parametreService;
        _logger = logger;
    }

    /// <summary>
    /// Récupère les paramètres d'une consultation
    /// Accessible par: infirmier, accueil, medecin, admin
    /// </summary>
    [HttpGet("consultation/{consultationId}")]
    public async Task<IActionResult> GetByConsultation(int consultationId)
    {
        try
        {
            var role = GetUserRole();
            if (!_parametreService.CanViewParametres(role))
            {
                return Forbid();
            }

            var parametre = await _parametreService.GetByConsultationIdAsync(consultationId);
            if (parametre == null)
            {
                return Ok(new { success = true, data = (object?)null, message = "Aucun paramètre enregistré" });
            }

            return Ok(new { success = true, data = parametre });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetByConsultation: {ex.Message}");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Récupère l'historique des paramètres d'un patient
    /// Accessible par: infirmier, accueil, medecin, admin
    /// </summary>
    [HttpGet("patient/{patientId}/historique")]
    public async Task<IActionResult> GetHistoriquePatient(int patientId)
    {
        try
        {
            var role = GetUserRole();
            if (!_parametreService.CanViewParametres(role))
            {
                return Forbid();
            }

            var parametres = await _parametreService.GetHistoriquePatientAsync(patientId);
            return Ok(new { success = true, data = parametres, count = parametres.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur GetHistoriquePatient: {ex.Message}");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Crée ou met à jour les paramètres d'une consultation
    /// Accessible par: infirmier, accueil, admin
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrUpdate([FromBody] CreateParametreRequest request)
    {
        try
        {
            var role = GetUserRole();
            if (!_parametreService.CanModifyParametres(role))
            {
                return Forbid();
            }

            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized(new { success = false, message = "Utilisateur non authentifié" });
            }

            // Validation des données
            var validationErrors = ValidateParametres(request);
            if (validationErrors.Any())
            {
                return BadRequest(new { success = false, message = "Données invalides", errors = validationErrors });
            }

            var parametre = await _parametreService.CreateOrUpdateAsync(request, userId.Value);
            return Ok(new { success = true, data = parametre, message = "Paramètres enregistrés avec succès" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur CreateOrUpdate: {ex.Message}");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Crée les paramètres directement pour un patient (sans consultation existante)
    /// Crée automatiquement une consultation de type "prise_parametres"
    /// Accessible par: infirmier, accueil, admin
    /// </summary>
    [HttpPost("patient")]
    public async Task<IActionResult> CreateByPatient([FromBody] CreateParametreByPatientRequest request)
    {
        try
        {
            var role = GetUserRole();
            if (!_parametreService.CanModifyParametres(role))
            {
                return Forbid();
            }

            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized(new { success = false, message = "Utilisateur non authentifié" });
            }

            // Validation des données
            var validationErrors = ValidateParametresByPatient(request);
            if (validationErrors.Any())
            {
                return BadRequest(new { success = false, message = "Données invalides", errors = validationErrors });
            }

            var parametre = await _parametreService.CreateByPatientAsync(request, userId.Value);
            return Ok(new { success = true, data = parametre, message = "Paramètres enregistrés avec succès" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning($"CreateByPatient validation: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur CreateByPatient: {ex.Message}");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Met à jour les paramètres existants
    /// Accessible par: infirmier, accueil, admin
    /// </summary>
    [HttpPut("{parametreId}")]
    public async Task<IActionResult> Update(int parametreId, [FromBody] UpdateParametreRequest request)
    {
        try
        {
            var role = GetUserRole();
            if (!_parametreService.CanModifyParametres(role))
            {
                return Forbid();
            }

            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized(new { success = false, message = "Utilisateur non authentifié" });
            }

            var parametre = await _parametreService.UpdateAsync(parametreId, request, userId.Value);
            if (parametre == null)
            {
                return NotFound(new { success = false, message = "Paramètres non trouvés" });
            }

            return Ok(new { success = true, data = parametre, message = "Paramètres mis à jour" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur Update: {ex.Message}");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    /// <summary>
    /// Supprime les paramètres (admin uniquement)
    /// </summary>
    [HttpDelete("{parametreId}")]
    public async Task<IActionResult> Delete(int parametreId)
    {
        try
        {
            var role = GetUserRole();
            if (role.ToLower() != "administrateur")
            {
                return Forbid();
            }

            var result = await _parametreService.DeleteAsync(parametreId);
            if (!result)
            {
                return NotFound(new { success = false, message = "Paramètres non trouvés" });
            }

            return Ok(new { success = true, message = "Paramètres supprimés" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur Delete: {ex.Message}");
            return StatusCode(500, new { success = false, message = "Erreur serveur" });
        }
    }

    #region Helpers

    private string GetUserRole()
    {
        // Selon la config JWT/Identity, le rôle peut être exposé soit sous "role"
        // soit sous ClaimTypes.Role (http://schemas.microsoft.com/ws/2008/06/identity/claims/role)
        return User.FindFirst(ClaimTypes.Role)?.Value
               ?? User.FindFirst("role")?.Value
               ?? "";
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst("userId");
        if (claim != null && int.TryParse(claim.Value, out int userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// Valide les paramètres vitaux (valeurs min/max)
    /// </summary>
    private List<string> ValidateParametres(CreateParametreRequest request)
    {
        var errors = new List<string>();

        // Champs obligatoires (workflow infirmier)
        if (!request.Poids.HasValue)
        {
            errors.Add("Le poids est obligatoire");
        }
        if (!request.Temperature.HasValue)
        {
            errors.Add("La température est obligatoire");
        }
        if (!request.TensionSystolique.HasValue)
        {
            errors.Add("La tension systolique est obligatoire");
        }
        if (!request.TensionDiastolique.HasValue)
        {
            errors.Add("La tension diastolique est obligatoire");
        }

        // Validation du poids (0.5 - 500 kg)
        if (request.Poids.HasValue && (request.Poids < 0.5m || request.Poids > 500))
        {
            errors.Add("Le poids doit être entre 0.5 et 500 kg");
        }

        // Validation de la température (30 - 45 °C)
        if (request.Temperature.HasValue && (request.Temperature < 30 || request.Temperature > 45))
        {
            errors.Add("La température doit être entre 30 et 45 °C");
        }

        // Validation de la tension systolique (60 - 250 mmHg)
        if (request.TensionSystolique.HasValue && (request.TensionSystolique < 60 || request.TensionSystolique > 250))
        {
            errors.Add("La tension systolique doit être entre 60 et 250 mmHg");
        }

        // Validation de la tension diastolique (40 - 150 mmHg)
        if (request.TensionDiastolique.HasValue && (request.TensionDiastolique < 40 || request.TensionDiastolique > 150))
        {
            errors.Add("La tension diastolique doit être entre 40 et 150 mmHg");
        }

        // Validation cohérence tension (systolique > diastolique)
        if (request.TensionSystolique.HasValue && request.TensionDiastolique.HasValue 
            && request.TensionSystolique <= request.TensionDiastolique)
        {
            errors.Add("La tension systolique doit être supérieure à la diastolique");
        }

        // Validation de la taille (20 - 300 cm)
        if (request.Taille.HasValue && (request.Taille < 20 || request.Taille > 300))
        {
            errors.Add("La taille doit être entre 20 et 300 cm");
        }

        return errors;
    }

    /// <summary>
    /// Valide les paramètres vitaux pour la création par patient
    /// </summary>
    private List<string> ValidateParametresByPatient(CreateParametreByPatientRequest request)
    {
        var errors = new List<string>();

        // Patient obligatoire
        if (request.IdPatient <= 0)
        {
            errors.Add("L'identifiant du patient est invalide");
        }

        // Champs obligatoires
        if (!request.Poids.HasValue)
        {
            errors.Add("Le poids est obligatoire");
        }
        if (!request.Temperature.HasValue)
        {
            errors.Add("La température est obligatoire");
        }
        if (!request.TensionSystolique.HasValue)
        {
            errors.Add("La tension systolique est obligatoire");
        }
        if (!request.TensionDiastolique.HasValue)
        {
            errors.Add("La tension diastolique est obligatoire");
        }

        // Validation du poids (0.5 - 500 kg)
        if (request.Poids.HasValue && (request.Poids < 0.5m || request.Poids > 500))
        {
            errors.Add("Le poids doit être entre 0.5 et 500 kg");
        }

        // Validation de la température (30 - 45 °C)
        if (request.Temperature.HasValue && (request.Temperature < 30 || request.Temperature > 45))
        {
            errors.Add("La température doit être entre 30 et 45 °C");
        }

        // Validation de la tension systolique (60 - 250 mmHg)
        if (request.TensionSystolique.HasValue && (request.TensionSystolique < 60 || request.TensionSystolique > 250))
        {
            errors.Add("La tension systolique doit être entre 60 et 250 mmHg");
        }

        // Validation de la tension diastolique (40 - 150 mmHg)
        if (request.TensionDiastolique.HasValue && (request.TensionDiastolique < 40 || request.TensionDiastolique > 150))
        {
            errors.Add("La tension diastolique doit être entre 40 et 150 mmHg");
        }

        // Validation cohérence tension (systolique > diastolique)
        if (request.TensionSystolique.HasValue && request.TensionDiastolique.HasValue 
            && request.TensionSystolique <= request.TensionDiastolique)
        {
            errors.Add("La tension systolique doit être supérieure à la diastolique");
        }

        // Validation de la taille (20 - 300 cm)
        if (request.Taille.HasValue && (request.Taille < 20 || request.Taille > 300))
        {
            errors.Add("La taille doit être entre 20 et 300 cm");
        }

        return errors;
    }

    #endregion
}
