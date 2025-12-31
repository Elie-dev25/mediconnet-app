using Mediconnet_Backend.Controllers.Base;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.DTOs.Admin;
using Microsoft.AspNetCore.Mvc;

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
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IUserManagementService userService,
        IServiceManagementService serviceManager,
        ILogger<AdminController> logger)
    {
        _userService = userService;
        _serviceManager = serviceManager;
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
            _logger.LogError($"Error getting users: {ex.Message}");
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
            _logger.LogError($"Error creating user: {ex.Message}");
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
            _logger.LogError($"Error deleting user: {ex.Message}");
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
            _logger.LogError($"Error getting specialites: {ex.Message}");
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
            _logger.LogError($"Error getting services: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la recuperation des services" });
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
            _logger.LogError($"Error getting service: {ex.Message}");
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
            _logger.LogError($"Error creating service: {ex.Message}");
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
            _logger.LogError($"Error updating service: {ex.Message}");
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
            _logger.LogError($"Error deleting service: {ex.Message}");
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
            _logger.LogError($"Error getting responsables: {ex.Message}");
            return StatusCode(500, new { message = "Erreur lors de la recuperation des responsables" });
        }
    }
}
