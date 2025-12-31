using Mediconnet_Backend.DTOs.Admin;

namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Interface pour la gestion des services hospitaliers
/// </summary>
public interface IServiceManagementService
{
    /// <summary>
    /// Obtenir tous les services
    /// </summary>
    Task<List<ServiceDto>> GetAllServicesAsync();

    /// <summary>
    /// Obtenir un service par son ID
    /// </summary>
    Task<ServiceDto?> GetServiceByIdAsync(int id);

    /// <summary>
    /// Creer un nouveau service
    /// </summary>
    Task<(bool Success, string Message, int? ServiceId)> CreateServiceAsync(CreateServiceRequest request);

    /// <summary>
    /// Modifier un service existant
    /// </summary>
    Task<(bool Success, string Message)> UpdateServiceAsync(int id, UpdateServiceRequest request);

    /// <summary>
    /// Supprimer un service
    /// </summary>
    Task<(bool Success, string Message)> DeleteServiceAsync(int id);

    /// <summary>
    /// Obtenir la liste des responsables potentiels (medecins)
    /// </summary>
    Task<List<ResponsableDto>> GetResponsablesAsync();
}
