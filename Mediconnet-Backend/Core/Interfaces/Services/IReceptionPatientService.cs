using Mediconnet_Backend.DTOs.Accueil;

namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Service pour la gestion des patients créés par l'accueil
/// </summary>
public interface IReceptionPatientService
{
    /// <summary>
    /// Crée un patient complet avec toutes ses informations et génère un mot de passe temporaire
    /// </summary>
    Task<CreatePatientByReceptionResponse> CreatePatientAsync(CreatePatientByReceptionRequest request, int createdByUserId);
    
    /// <summary>
    /// Récupère les informations du patient pour la page de première connexion
    /// </summary>
    Task<FirstLoginPatientInfoResponse> GetFirstLoginInfoAsync(int userId);
    
    /// <summary>
    /// Valide la première connexion (déclaration sur l'honneur + changement mot de passe)
    /// </summary>
    Task<FirstLoginValidationResponse> ValidateFirstLoginAsync(int userId, FirstLoginValidationRequest request);
    
    /// <summary>
    /// Accepte uniquement la déclaration sur l'honneur (sans changement de mot de passe)
    /// </summary>
    Task<AcceptDeclarationResponse> AcceptDeclarationAsync(int userId, AcceptDeclarationRequest request);
    
    /// <summary>
    /// Vérifie si l'utilisateur doit compléter sa première connexion
    /// </summary>
    Task<bool> RequiresFirstLoginValidationAsync(int userId);
}
