using Mediconnet_Backend.Controllers.Base;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Admin;
using Mediconnet_Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// Controleur legacy pour la retrocompatibilite des anciennes routes
/// Les nouvelles implementations sont dans UsersController, ServicesController, SpecialitesController
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AdminController : BaseAdminController
{
    private readonly IUserManagementService _userService;
    private readonly IServiceManagementService _serviceManager;
    private readonly IUserDetailsService _userDetailsService;
    private readonly IInfirmierManagementService _infirmierService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IUserManagementService userService,
        IServiceManagementService serviceManager,
        IUserDetailsService userDetailsService,
        IInfirmierManagementService infirmierService,
        ApplicationDbContext context,
        ILogger<AdminController> logger)
    {
        _userService = userService;
        _serviceManager = serviceManager;
        _userDetailsService = userDetailsService;
        _infirmierService = infirmierService;
        _context = context;
        _logger = logger;
    }

    // ==================== USERS (Legacy routes) ====================

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        try
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return StatusCode(500, new { message = "Erreur lors de la recuperation des utilisateurs" });
        }
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            var (success, message, userId) = await _userService.CreateUserAsync(request);
            if (!success) return BadRequest(new { message });
            return Ok(new { message, userId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { message = "Erreur lors de la creation de l'utilisateur" });
        }
    }

    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        try
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            var (success, message) = await _userService.DeleteUserAsync(userId, GetCurrentUserId());
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
            return StatusCode(500, new { message = "Erreur lors de la suppression de l'utilisateur" });
        }
    }

    // ==================== SPECIALITES (Legacy route) ====================

    [HttpGet("specialites")]
    public async Task<IActionResult> GetSpecialites()
    {
        try
        {
            var specialites = await _userService.GetSpecialitesAsync();
            return Ok(specialites);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting specialites");
            return StatusCode(500, new { message = "Erreur lors de la recuperation des specialites" });
        }
    }

    // ==================== SERVICES (Legacy routes) ====================

    [HttpGet("services")]
    public async Task<IActionResult> GetServices()
    {
        try
        {
            var services = await _serviceManager.GetAllServicesAsync();
            return Ok(services);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting services");
            return StatusCode(500, new { message = "Erreur lors de la recuperation des services" });
        }
    }

    // ==================== LABORATOIRES ====================

    [HttpGet("laboratoires")]
    public async Task<IActionResult> GetLaboratoires()
    {
        try
        {
            var laboratoires = await _context.Laboratoires
                .Where(l => l.Actif == true && l.Type == "interne")
                .Select(l => new {
                    idLabo = l.IdLabo,
                    nomLabo = l.NomLabo,
                    contact = l.Contact,
                    telephone = l.Telephone
                })
                .OrderBy(l => l.nomLabo)
                .ToListAsync();
            return Ok(laboratoires);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting laboratoires");
            return StatusCode(500, new { message = "Erreur lors de la recuperation des laboratoires" });
        }
    }

    [HttpGet("services/{id}")]
    public async Task<IActionResult> GetService(int id)
    {
        try
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            var service = await _serviceManager.GetServiceByIdAsync(id);
            if (service == null) return NotFound(new { message = "Service non trouve" });
            return Ok(service);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service");
            return StatusCode(500, new { message = "Erreur lors de la recuperation du service" });
        }
    }

    [HttpPost("services")]
    public async Task<IActionResult> CreateService([FromBody] CreateServiceRequest request)
    {
        try
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            var (success, message, serviceId) = await _serviceManager.CreateServiceAsync(request);
            if (!success) return BadRequest(new { message });
            return Ok(new { message, serviceId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating service");
            return StatusCode(500, new { message = "Erreur lors de la creation du service" });
        }
    }

    [HttpPut("services/{id}")]
    public async Task<IActionResult> UpdateService(int id, [FromBody] UpdateServiceRequest request)
    {
        try
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            var (success, message) = await _serviceManager.UpdateServiceAsync(id, request);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating service");
            return StatusCode(500, new { message = "Erreur lors de la modification du service" });
        }
    }

    [HttpDelete("services/{id}")]
    public async Task<IActionResult> DeleteService(int id)
    {
        try
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            var (success, message) = await _serviceManager.DeleteServiceAsync(id);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting service");
            return StatusCode(500, new { message = "Erreur lors de la suppression du service" });
        }
    }

    [HttpGet("responsables")]
    public async Task<IActionResult> GetResponsables()
    {
        try
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            var responsables = await _serviceManager.GetResponsablesAsync();
            return Ok(responsables);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting responsables");
            return StatusCode(500, new { message = "Erreur lors de la recuperation des responsables" });
        }
    }

    // ==================== USER DETAILS (Fiche utilisateur) ====================

    /// <summary>
    /// Récupère les détails complets d'un utilisateur (pour la fiche latérale)
    /// </summary>
    [HttpGet("users/{userId}/details")]
    public async Task<IActionResult> GetUserDetails(int userId)
    {
        try
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            var details = await _userDetailsService.GetUserDetailsAsync(userId);
            if (details == null)
                return NotFound(new { success = false, message = "Utilisateur non trouvé" });

            return Ok(new { success = true, data = details });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user details");
            return StatusCode(500, new { success = false, message = "Erreur lors de la récupération des détails" });
        }
    }

    // ==================== INFIRMIER MANAGEMENT ====================

    /// <summary>
    /// Met à jour le statut d'un infirmier (actif, bloque, suspendu)
    /// </summary>
    [HttpPut("infirmiers/{userId}/statut")]
    public async Task<IActionResult> UpdateInfirmierStatut(int userId, [FromBody] UpdateInfirmierStatutRequest request)
    {
        try
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            var (success, message) = await _infirmierService.UpdateStatutAsync(userId, request.Statut);
            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating infirmier statut");
            return StatusCode(500, new { success = false, message = "Erreur lors de la mise à jour du statut" });
        }
    }

    /// <summary>
    /// Nomme un infirmier comme Major d'un service
    /// </summary>
    [HttpPost("infirmiers/{userId}/nommer-major")]
    public async Task<IActionResult> NommerInfirmierMajor(int userId, [FromBody] NommerInfirmierMajorRequest request)
    {
        try
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            var (success, message) = await _infirmierService.NommerMajorAsync(userId, request.IdService);
            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error nominating infirmier major");
            return StatusCode(500, new { success = false, message = "Erreur lors de la nomination" });
        }
    }

    /// <summary>
    /// Révoque la nomination Major d'un infirmier
    /// </summary>
    [HttpPost("infirmiers/{userId}/revoquer-major")]
    public async Task<IActionResult> RevoquerInfirmierMajor(int userId, [FromBody] RevoquerMajorRequest? request)
    {
        try
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            var (success, message) = await _infirmierService.RevoquerMajorAsync(userId, request?.Motif);
            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking infirmier major");
            return StatusCode(500, new { success = false, message = "Erreur lors de la révocation" });
        }
    }

    /// <summary>
    /// Met à jour les accréditations d'un infirmier
    /// </summary>
    [HttpPut("infirmiers/{userId}/accreditations")]
    public async Task<IActionResult> UpdateInfirmierAccreditations(int userId, [FromBody] UpdateAccreditationsRequest request)
    {
        try
        {
            var accessCheck = CheckAdminAccess();
            if (accessCheck != null) return accessCheck;

            var (success, message) = await _infirmierService.UpdateAccreditationsAsync(userId, request.Accreditations);
            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating infirmier accreditations");
            return StatusCode(500, new { success = false, message = "Erreur lors de la mise à jour des accréditations" });
        }
    }
}
