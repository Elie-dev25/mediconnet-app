using Mediconnet_Backend.DTOs.Prescription;

namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Interface centralisée pour la gestion des prescriptions médicamenteuses
/// Unifie la logique de prescription depuis tous les contextes :
/// - Consultation classique
/// - Hospitalisation
/// - Prescription directe (hors consultation)
/// </summary>
public interface IPrescriptionService
{
    // ==================== Création d'ordonnances ====================

    /// <summary>
    /// Crée une ordonnance générique (méthode de base)
    /// </summary>
    /// <param name="request">Données de l'ordonnance</param>
    /// <param name="medecinId">ID du médecin prescripteur</param>
    /// <returns>Résultat avec l'ordonnance créée ou erreurs</returns>
    Task<OrdonnanceResult> CreerOrdonnanceAsync(CreateOrdonnanceRequest request, int medecinId);

    /// <summary>
    /// Crée une ordonnance dans le contexte d'une consultation
    /// </summary>
    /// <param name="idConsultation">ID de la consultation</param>
    /// <param name="medicaments">Liste des médicaments à prescrire</param>
    /// <param name="notes">Notes/commentaires</param>
    /// <param name="medecinId">ID du médecin prescripteur</param>
    /// <returns>Résultat avec l'ordonnance créée ou erreurs</returns>
    Task<OrdonnanceResult> CreerOrdonnanceConsultationAsync(
        int idConsultation, 
        List<MedicamentPrescriptionRequest> medicaments, 
        string? notes, 
        int medecinId);

    /// <summary>
    /// Crée une ordonnance dans le contexte d'une hospitalisation
    /// </summary>
    /// <param name="idHospitalisation">ID de l'hospitalisation</param>
    /// <param name="medicaments">Liste des médicaments à prescrire</param>
    /// <param name="notes">Notes/commentaires</param>
    /// <param name="medecinId">ID du médecin prescripteur</param>
    /// <returns>Résultat avec l'ordonnance créée ou erreurs</returns>
    Task<OrdonnanceResult> CreerOrdonnanceHospitalisationAsync(
        int idHospitalisation, 
        List<MedicamentPrescriptionRequest> medicaments, 
        string? notes, 
        int medecinId);

    /// <summary>
    /// Crée une ordonnance directe (hors consultation/hospitalisation)
    /// Utile pour les renouvellements ou prescriptions depuis la fiche patient
    /// </summary>
    /// <param name="idPatient">ID du patient</param>
    /// <param name="medicaments">Liste des médicaments à prescrire</param>
    /// <param name="notes">Notes/commentaires</param>
    /// <param name="medecinId">ID du médecin prescripteur</param>
    /// <returns>Résultat avec l'ordonnance créée ou erreurs</returns>
    Task<OrdonnanceResult> CreerOrdonnanceDirecteAsync(
        int idPatient, 
        List<MedicamentPrescriptionRequest> medicaments, 
        string? notes, 
        int medecinId);

    // ==================== Lecture ====================

    /// <summary>
    /// Récupère une ordonnance par son ID
    /// </summary>
    Task<OrdonnanceDto?> GetOrdonnanceAsync(int idOrdonnance);

    /// <summary>
    /// Récupère l'ordonnance liée à une consultation
    /// </summary>
    Task<OrdonnanceDto?> GetOrdonnanceByConsultationAsync(int idConsultation);

    /// <summary>
    /// Récupère les ordonnances d'un patient
    /// </summary>
    Task<List<OrdonnanceDto>> GetOrdonnancesPatientAsync(int idPatient);

    /// <summary>
    /// Récupère les ordonnances d'une hospitalisation
    /// </summary>
    Task<List<OrdonnanceDto>> GetOrdonnancesHospitalisationAsync(int idHospitalisation);

    /// <summary>
    /// Recherche des ordonnances avec filtres
    /// </summary>
    Task<(List<OrdonnanceDto> Items, int Total)> RechercherOrdonnancesAsync(FiltreOrdonnanceRequest filtre);

    // ==================== Modification ====================

    /// <summary>
    /// Met à jour une ordonnance existante (remplace les médicaments)
    /// </summary>
    /// <param name="idOrdonnance">ID de l'ordonnance</param>
    /// <param name="medicaments">Nouvelle liste de médicaments</param>
    /// <param name="notes">Notes mises à jour</param>
    /// <param name="medecinId">ID du médecin effectuant la modification</param>
    /// <returns>Résultat avec l'ordonnance mise à jour ou erreurs</returns>
    Task<OrdonnanceResult> MettreAJourOrdonnanceAsync(
        int idOrdonnance, 
        List<MedicamentPrescriptionRequest> medicaments, 
        string? notes, 
        int medecinId);

    /// <summary>
    /// Annule une ordonnance
    /// </summary>
    /// <param name="idOrdonnance">ID de l'ordonnance</param>
    /// <param name="motif">Motif d'annulation</param>
    /// <param name="medecinId">ID du médecin effectuant l'annulation</param>
    /// <returns>True si annulation réussie</returns>
    Task<bool> AnnulerOrdonnanceAsync(int idOrdonnance, string motif, int medecinId);

    // ==================== Validation ====================

    /// <summary>
    /// Valide une prescription avant création (vérifie stock, interactions, etc.)
    /// </summary>
    /// <param name="idPatient">ID du patient</param>
    /// <param name="medicaments">Liste des médicaments à valider</param>
    /// <returns>Résultat de validation avec alertes éventuelles</returns>
    Task<ValidationPrescriptionResult> ValiderPrescriptionAsync(
        int idPatient, 
        List<MedicamentPrescriptionRequest> medicaments);

    // ==================== Utilitaires ====================

    /// <summary>
    /// Recherche un médicament dans le catalogue par ID ou nom exact.
    /// Ne crée PAS de médicament - retourne null si non trouvé.
    /// </summary>
    /// <param name="idMedicament">ID du médicament (optionnel)</param>
    /// <param name="nomMedicament">Nom du médicament</param>
    /// <returns>ID du médicament si trouvé, null sinon</returns>
    Task<int?> RechercherMedicamentCatalogueAsync(int? idMedicament, string nomMedicament);
}
