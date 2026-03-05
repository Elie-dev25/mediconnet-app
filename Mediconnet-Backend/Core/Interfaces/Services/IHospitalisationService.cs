using Mediconnet_Backend.DTOs.Hospitalisation;

namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Interface pour la gestion des hospitalisations
/// </summary>
public interface IHospitalisationService
{
    /// <summary>
    /// Récupère la liste des chambres avec leurs lits
    /// </summary>
    Task<ChambresResponse> GetChambresAsync();

    /// <summary>
    /// Récupère la liste des lits disponibles
    /// </summary>
    Task<LitsDisponiblesResponse> GetLitsDisponiblesAsync();

    /// <summary>
    /// Récupère la liste des hospitalisations avec filtres optionnels
    /// </summary>
    Task<List<HospitalisationDto>> GetHospitalisationsAsync(FiltreHospitalisationRequest? filtre = null);

    /// <summary>
    /// Récupère une hospitalisation par son ID
    /// </summary>
    Task<HospitalisationDto?> GetHospitalisationByIdAsync(int idAdmission);

    /// <summary>
    /// Termine une hospitalisation et libère le lit
    /// </summary>
    Task<HospitalisationResponse> TerminerHospitalisationAsync(TerminerHospitalisationRequest request);

    /// <summary>
    /// Récupère l'historique des hospitalisations d'un patient
    /// </summary>
    Task<List<HospitalisationDto>> GetHospitalisationsPatientAsync(int idPatient);

    /// <summary>
    /// Ordonne une hospitalisation sans attribution de lit (nouveau workflow)
    /// Le médecin prescrit l'hospitalisation, le Major attribuera le lit
    /// </summary>
    Task<HospitalisationResponse> OrdonnerHospitalisationAsync(OrdonnerHospitalisationRequest request, int medecinId);

    /// <summary>
    /// Ordonne une hospitalisation complète avec prescriptions (examens, médicaments, soins)
    /// </summary>
    Task<HospitalisationResponse> OrdonnerHospitalisationCompleteAsync(OrdonnerHospitalisationCompleteRequest request, int medecinId);

    /// <summary>
    /// Attribue un lit à une hospitalisation en attente
    /// </summary>
    Task<HospitalisationResponse> AttribuerLitAsync(AttribuerLitRequest request, int userId, string role);

    /// <summary>
    /// Récupère les hospitalisations en attente de lit, optionnellement filtrées par service
    /// </summary>
    Task<List<HospitalisationDto>> GetHospitalisationsEnAttenteAsync(int? idService = null);
}
