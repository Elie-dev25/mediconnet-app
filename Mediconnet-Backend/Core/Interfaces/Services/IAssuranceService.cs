using Mediconnet_Backend.DTOs.Assurance;

namespace Mediconnet_Backend.Core.Interfaces.Services;

public interface IAssuranceService
{
    // ==================== ASSURANCES ====================
    
    /// <summary>
    /// Récupérer toutes les assurances avec filtres optionnels
    /// </summary>
    Task<AssuranceListResponse> GetAssurancesAsync(AssuranceFilterDto? filter = null);

    /// <summary>
    /// Récupérer les assurances actives (pour les listes déroulantes)
    /// </summary>
    Task<List<AssuranceListDto>> GetAssurancesActivesAsync();

    /// <summary>
    /// Récupérer une assurance par son ID
    /// </summary>
    Task<AssuranceDetailDto?> GetAssuranceByIdAsync(int idAssurance);

    /// <summary>
    /// Créer une nouvelle assurance
    /// </summary>
    Task<AssuranceResponse> CreateAssuranceAsync(CreateAssuranceDto dto);

    /// <summary>
    /// Mettre à jour une assurance
    /// </summary>
    Task<AssuranceResponse> UpdateAssuranceAsync(int idAssurance, UpdateAssuranceDto dto);

    /// <summary>
    /// Activer/Désactiver une assurance
    /// </summary>
    Task<AssuranceResponse> ToggleAssuranceStatusAsync(int idAssurance);

    /// <summary>
    /// Supprimer une assurance
    /// </summary>
    Task<AssuranceResponse> DeleteAssuranceAsync(int idAssurance);

    // ==================== PATIENT ASSURANCE ====================

    /// <summary>
    /// Récupérer l'assurance d'un patient
    /// </summary>
    Task<PatientAssuranceInfoDto?> GetPatientAssuranceAsync(int idPatient);

    /// <summary>
    /// Mettre à jour l'assurance d'un patient
    /// </summary>
    Task<PatientAssuranceResponse> UpdatePatientAssuranceAsync(int idPatient, UpdatePatientAssuranceDto dto);

    /// <summary>
    /// Retirer l'assurance d'un patient
    /// </summary>
    Task<PatientAssuranceResponse> RemovePatientAssuranceAsync(int idPatient);
}
