using Mediconnet_Backend.DTOs.Planning;

namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Interface pour la gestion du planning médecin
/// </summary>
public interface IMedecinPlanningService
{
    // ==================== DASHBOARD ====================
    Task<PlanningDashboardDto> GetDashboardAsync(int medecinId);

    // ==================== CRÉNEAUX HORAIRES ====================
    Task<SemaineTypeDto> GetSemaineTypeAsync(int medecinId);
    Task<SemainePlanningDto> GetSemainePlanningAsync(int medecinId, DateTime dateDebut);
    Task<List<CreneauHoraireDto>> GetCreneauxJourAsync(int medecinId, int jourSemaine);
    Task<(bool Success, string Message, CreneauHoraireDto? Creneau)> CreateCreneauAsync(int medecinId, CreateCreneauRequest request);
    Task<(bool Success, string Message)> UpdateCreneauAsync(int medecinId, int creneauId, CreateCreneauRequest request);
    Task<(bool Success, string Message)> DeleteCreneauAsync(int medecinId, int creneauId);
    Task<(bool Success, string Message)> ToggleCreneauAsync(int medecinId, int creneauId);

    // ==================== INDISPONIBILITÉS ====================
    Task<List<IndisponibiliteDto>> GetIndisponibilitesAsync(int medecinId, DateTime? dateDebut = null, DateTime? dateFin = null);
    Task<(bool Success, string Message, IndisponibiliteDto? Indispo)> CreateIndisponibiliteAsync(int medecinId, CreateIndisponibiliteRequest request);
    Task<(bool Success, string Message)> DeleteIndisponibiliteAsync(int medecinId, int indispoId);

    // ==================== CALENDRIER ====================
    Task<List<JourneeCalendrierDto>> GetCalendrierSemaineAsync(int medecinId, DateTime dateDebut);
    Task<JourneeCalendrierDto> GetCalendrierJourAsync(int medecinId, DateTime date);
}
