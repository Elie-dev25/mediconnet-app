using Mediconnet_Backend.DTOs.Medecin;

namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Interface pour le service médecin
/// Gère la logique métier liée aux médecins
/// </summary>
public interface IMedecinService
{
    /// <summary>
    /// Obtenir le profil complet d'un médecin
    /// </summary>
    Task<MedecinProfileDto?> GetProfileAsync(int userId);

    /// <summary>
    /// Obtenir les statistiques du dashboard médecin
    /// </summary>
    Task<MedecinDashboardDto> GetDashboardAsync(int userId);

    /// <summary>
    /// Mettre à jour le profil du médecin
    /// </summary>
    Task<bool> UpdateProfileAsync(int userId, UpdateMedecinProfileRequest request);

    /// <summary>
    /// Obtenir l'agenda du médecin
    /// </summary>
    Task<MedecinAgendaDto> GetAgendaAsync(int userId, DateTime dateDebut, DateTime dateFin);
}
