using Mediconnet_Backend.DTOs.Consultation;

namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Interface pour le service de gestion des paramètres vitaux
/// </summary>
public interface IParametreService
{
    /// <summary>Récupère les paramètres d'une consultation</summary>
    Task<ParametreDto?> GetByConsultationIdAsync(int consultationId);
    
    /// <summary>Récupère l'historique des paramètres d'un patient</summary>
    Task<List<ParametreDto>> GetHistoriquePatientAsync(int patientId);
    
    /// <summary>Crée ou met à jour les paramètres d'une consultation</summary>
    Task<ParametreDto> CreateOrUpdateAsync(CreateParametreRequest request, int userId);
    
    /// <summary>Crée les paramètres pour un patient (crée automatiquement une consultation de type prise_parametres)</summary>
    Task<ParametreDto> CreateByPatientAsync(CreateParametreByPatientRequest request, int userId);
    
    /// <summary>Met à jour les paramètres existants</summary>
    Task<ParametreDto?> UpdateAsync(int parametreId, UpdateParametreRequest request, int userId);
    
    /// <summary>Supprime les paramètres (admin uniquement)</summary>
    Task<bool> DeleteAsync(int parametreId);
    
    /// <summary>Vérifie si l'utilisateur peut modifier les paramètres</summary>
    bool CanModifyParametres(string role);
    
    /// <summary>Vérifie si l'utilisateur peut voir les paramètres</summary>
    bool CanViewParametres(string role);
}
