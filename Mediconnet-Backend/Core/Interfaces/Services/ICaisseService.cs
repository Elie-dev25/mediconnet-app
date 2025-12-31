using Mediconnet_Backend.DTOs.Caisse;

namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Interface pour le service de gestion de la caisse
/// </summary>
public interface ICaisseService
{
    // ==================== KPIs ====================
    Task<CaisseKpiDto> GetKpisAsync(int caissierUserId);

    // ==================== FACTURES ====================
    Task<List<FactureListItemDto>> GetFacturesEnAttenteAsync();
    Task<List<FactureListItemDto>> GetFacturesPatientAsync(int idPatient);
    Task<FactureDto?> GetFactureAsync(int idFacture);

    // ==================== TRANSACTIONS ====================
    Task<List<TransactionDto>> GetTransactionsJourAsync(int? caissierUserId = null);
    Task<List<TransactionDto>> GetTransactionsAsync(DateTime? dateDebut, DateTime? dateFin, string? modePaiement);
    Task<(bool Success, string Message, TransactionDto? Transaction)> CreerTransactionAsync(
        CreateTransactionRequest request, int caissierUserId);
    Task<(bool Success, string Message)> AnnulerTransactionAsync(
        AnnulerTransactionRequest request, int caissierUserId);

    // ==================== SESSION CAISSE ====================
    Task<SessionCaisseDto?> GetSessionActiveAsync(int caissierUserId);
    Task<List<SessionCaisseDto>> GetHistoriqueSessionsAsync(int caissierUserId, int limite = 10);
    Task<(bool Success, string Message, SessionCaisseDto? Session)> OuvrirCaisseAsync(
        OuvrirCaisseRequest request, int caissierUserId);
    Task<(bool Success, string Message, SessionCaisseDto? Session)> FermerCaisseAsync(
        FermerCaisseRequest request, int caissierUserId);

    // ==================== RECHERCHE PATIENT ====================
    Task<List<PatientSearchResultDto>> RechercherPatientsAsync(string query);

    // ==================== REÇU ====================
    Task<RecuTransactionDto?> GetRecuTransactionAsync(int idTransaction);

    // ==================== STATISTIQUES ====================
    Task<List<RepartitionPaiementDto>> GetRepartitionPaiementsAsync(DateTime? dateDebut, DateTime? dateFin);
    Task<List<RevenuParServiceDto>> GetRevenusParServiceAsync(DateTime date);
    Task<List<FactureRetardDto>> GetFacturesEnRetardAsync(int limite = 5);
}
