using Mediconnet_Backend.DTOs.RendezVous;

namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Interface pour le service de gestion des rendez-vous
/// </summary>
public interface IRendezVousService
{
    // ==================== PATIENT ====================
    
    /// <summary>
    /// Obtenir les statistiques des rendez-vous du patient
    /// </summary>
    Task<RendezVousStatsDto> GetPatientStatsAsync(int patientId);

    /// <summary>
    /// Obtenir les rendez-vous à venir du patient
    /// </summary>
    Task<List<RendezVousListDto>> GetPatientUpcomingAsync(int patientId);

    /// <summary>
    /// Obtenir l'historique des rendez-vous du patient
    /// </summary>
    Task<List<RendezVousListDto>> GetPatientHistoryAsync(int patientId, int limite = 20);

    /// <summary>
    /// Obtenir le détail d'un rendez-vous
    /// </summary>
    Task<RendezVousDto?> GetRendezVousAsync(int rdvId, int patientId);

    /// <summary>
    /// Créer un nouveau rendez-vous
    /// </summary>
    Task<(bool Success, string Message, RendezVousDto? RendezVous)> CreateRendezVousAsync(
        CreateRendezVousRequest request, int patientId);

    /// <summary>
    /// Modifier un rendez-vous existant
    /// </summary>
    Task<(bool Success, string Message)> UpdateRendezVousAsync(
        int rdvId, UpdateRendezVousRequest request, int patientId);

    /// <summary>
    /// Annuler un rendez-vous
    /// </summary>
    Task<(bool Success, string Message)> AnnulerRendezVousAsync(
        AnnulerRendezVousRequest request, int patientId);

    // ==================== CRÉNEAUX ====================

    /// <summary>
    /// Obtenir les médecins disponibles
    /// </summary>
    Task<List<MedecinDisponibleDto>> GetMedecinsDisponiblesAsync(int? serviceId = null);

    /// <summary>
    /// Obtenir les créneaux disponibles pour un médecin
    /// </summary>
    Task<CreneauxDisponiblesResponse> GetCreneauxDisponiblesAsync(
        int medecinId, DateTime dateDebut, DateTime dateFin);

    // ==================== MÉDECIN ====================

    /// <summary>
    /// Obtenir les rendez-vous du médecin
    /// </summary>
    Task<List<RendezVousDto>> GetMedecinRendezVousAsync(int medecinId, DateTime? dateDebut = null, DateTime? dateFin = null, string? statut = null);

    /// <summary>
    /// Obtenir les RDV du jour pour le médecin
    /// </summary>
    Task<List<RendezVousDto>> GetMedecinRdvJourAsync(int medecinId, DateTime date);

    /// <summary>
    /// Mettre à jour le statut d'un RDV (médecin)
    /// </summary>
    Task<(bool Success, string Message)> UpdateStatutRdvAsync(int medecinId, int rdvId, string nouveauStatut);

    /// <summary>
    /// Obtenir les RDV en attente de validation pour le médecin
    /// </summary>
    Task<List<RendezVousDto>> GetRdvEnAttenteAsync(int medecinId);

    /// <summary>
    /// Valider un RDV (le confirmer) avec gestion des conflits
    /// </summary>
    Task<ActionRdvResponse> ValiderRdvAsync(int medecinId, int rdvId);

    /// <summary>
    /// Annuler un RDV par le médecin avec notification
    /// </summary>
    Task<ActionRdvResponse> AnnulerRdvMedecinAsync(int medecinId, AnnulerRdvMedecinRequest request);

    /// <summary>
    /// Suggérer un nouveau créneau pour un RDV
    /// </summary>
    Task<ActionRdvResponse> SuggererCreneauAsync(int medecinId, SuggererCreneauRequest request);

    /// <summary>
    /// Accepter une proposition de créneau (côté patient)
    /// </summary>
    Task<ActionRdvResponse> AccepterPropositionAsync(int patientId, int rdvId);

    /// <summary>
    /// Refuser une proposition de créneau (côté patient)
    /// </summary>
    Task<ActionRdvResponse> RefuserPropositionAsync(int patientId, RefuserPropositionRequest request);

    /// <summary>
    /// Récupérer les RDV avec proposition de créneau pour un patient
    /// </summary>
    Task<List<RendezVousDto>> GetPropositionsPatientAsync(int patientId);
}
