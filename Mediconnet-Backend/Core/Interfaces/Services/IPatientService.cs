using Mediconnet_Backend.DTOs.Patient;

namespace Mediconnet_Backend.Core.Interfaces.Services
{
    /// <summary>
    /// Interface pour les services liés aux patients
    /// </summary>
    public interface IPatientService
    {
        /// <summary>
        /// Récupère les N patients les plus récemment enregistrés
        /// </summary>
        /// <param name="count">Nombre de patients à récupérer (par défaut 6)</param>
        /// <returns>Liste des patients récents</returns>
        Task<RecentPatientsResponse> GetRecentPatientsAsync(int count = 6);

        /// <summary>
        /// Recherche des patients par numéro de dossier, nom ou email
        /// </summary>
        /// <param name="request">Critères de recherche</param>
        /// <returns>Liste des patients correspondants</returns>
        Task<PatientSearchResponse> SearchPatientsAsync(PatientSearchRequest request);
    }
}
