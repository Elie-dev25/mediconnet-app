using Mediconnet_Backend.DTOs.Accueil;

namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Interface pour la gestion des consultations
/// </summary>
public interface IConsultationService
{
    /// <summary>
    /// Enregistre une nouvelle consultation et crée la facture associée
    /// </summary>
    Task<EnregistrerConsultationResponse> EnregistrerConsultationAsync(EnregistrerConsultationRequest request, int createdByUserId);
    
    /// <summary>
    /// Récupère la liste des médecins disponibles
    /// </summary>
    Task<List<MedecinDisponibleDto>> GetMedecinsDisponiblesAsync();

    /// <summary>
    /// Récupère la liste des médecins filtrés par service et/ou spécialité
    /// </summary>
    Task<List<MedecinDisponibleDto>> GetMedecinsFiltresAsync(int? idService, int? idSpecialite);

    /// <summary>
    /// Récupère la liste des services hospitaliers
    /// </summary>
    Task<List<ServiceDto>> GetServicesAsync();

    /// <summary>
    /// Récupère la liste des spécialités médicales
    /// </summary>
    Task<List<SpecialiteDto>> GetSpecialitesAsync();

    /// <summary>
    /// Récupère la liste des médecins avec leur statut de disponibilité en temps réel
    /// </summary>
    Task<MedecinsDisponibiliteResponse> GetMedecinsAvecDisponibiliteAsync(int? idService, int? idSpecialite);

    /// <summary>
    /// Vérifie si un patient a un paiement de consultation encore valide (règle des 14 jours)
    /// </summary>
    Task<VerifierPaiementResponse> VerifierPaiementValideAsync(int idPatient, int idMedecin);
}
