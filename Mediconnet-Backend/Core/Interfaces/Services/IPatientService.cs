using Mediconnet_Backend.DTOs.Patient;

namespace Mediconnet_Backend.Core.Interfaces.Services
{
    /// <summary>
    /// Interface pour les services liés aux patients
    /// </summary>
    public interface IPatientService
    {
        /// <summary>
        /// Récupère le profil complet d'un patient
        /// </summary>
        Task<PatientProfileDto?> GetProfileAsync(int userId);

        /// <summary>
        /// Vérifie le statut de complétion du profil
        /// </summary>
        Task<ProfileStatusDto> GetProfileStatusAsync(int userId);

        /// <summary>
        /// Met à jour le profil d'un patient
        /// </summary>
        Task<bool> UpdateProfileAsync(int userId, UpdatePatientProfileRequest request);

        /// <summary>
        /// Récupère les N patients les plus récemment enregistrés
        /// </summary>
        Task<RecentPatientsResponse> GetRecentPatientsAsync(int count = 6);

        /// <summary>
        /// Recherche des patients par numéro de dossier, nom ou email
        /// </summary>
        Task<PatientSearchResponse> SearchPatientsAsync(PatientSearchRequest request);
    }
}
