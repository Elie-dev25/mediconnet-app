using Mediconnet_Backend.DTOs.Auth;

namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Service d'enregistrement des patients en deux étapes
/// </summary>
public interface IPatientRegistrationService
{
    /// <summary>
    /// Étape 1: Créer un compte patient avec les informations essentielles
    /// </summary>
    Task<PatientRegistrationStep1Response?> RegisterPatientStep1Async(
        PatientRegistrationStep1Request request);

    /// <summary>
    /// Étape 2: Compléter le profil patient avec toutes les informations
    /// </summary>
    Task<PatientProfileCompletionResponse?> CompletePatientProfileAsync(
        int userId,
        PatientProfileCompletionRequest request);

    /// <summary>
    /// Récupérer le statut du profil d'un patient
    /// </summary>
    Task<PatientProfileStatusResponse?> GetPatientProfileStatusAsync(int userId);

    /// <summary>
    /// Récupérer le profil complet d'un patient
    /// </summary>
    Task<PatientProfileCompletionRequest?> GetPatientProfileAsync(int userId);

    /// <summary>
    /// Mettre à jour partiellement le profil patient
    /// </summary>
    Task<bool> UpdatePatientProfileAsync(
        int userId,
        PatientProfileCompletionRequest request);
}
