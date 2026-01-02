namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Service de gestion des lits - Suivi temps réel et affectation automatique
/// </summary>
public interface ILitManagementService
{
    // Dashboard temps réel
    Task<OccupationDashboardDto> GetOccupationDashboardAsync();
    Task<List<ChambreOccupationDto>> GetOccupationParChambreAsync();
    
    // Affectation automatique
    Task<AffectationResult> AffecterLitAutomatiqueAsync(AffectationRequest request);
    Task<List<LitSuggestionDto>> GetLitsSuggeresAsync(int idPatient, string? criteres = null);
    
    // Gestion des réservations
    Task<ReservationLitDto> ReserverLitAsync(ReservationLitRequest request);
    Task<bool> AnnulerReservationAsync(int idReservation);
    Task<List<ReservationLitDto>> GetReservationsEnCoursAsync();
    
    // Transferts
    Task<TransfertResult> TransfererPatientAsync(TransfertRequest request);
    Task<List<TransfertHistoriqueDto>> GetHistoriqueTransfertsAsync(int? idPatient = null);
    
    // Maintenance
    Task<bool> MarquerLitEnMaintenanceAsync(int idLit, string motif);
    Task<bool> LibererLitMaintenanceAsync(int idLit);
    Task<List<LitMaintenanceDto>> GetLitsEnMaintenanceAsync();
    
    // Statistiques
    Task<OccupationStatsDto> GetStatistiquesOccupationAsync(DateTime dateDebut, DateTime dateFin);
}

// DTOs pour la gestion des lits
public class OccupationDashboardDto
{
    public int TotalLits { get; set; }
    public int LitsOccupes { get; set; }
    public int LitsLibres { get; set; }
    public int LitsReserves { get; set; }
    public int LitsEnMaintenance { get; set; }
    public decimal TauxOccupation { get; set; }
    public int SortiesPrevuesAujourdhui { get; set; }
    public int AdmissionsPrevuesAujourdhui { get; set; }
    public List<OccupationParServiceDto> OccupationParService { get; set; } = new();
}

public class OccupationParServiceDto
{
    public string NomService { get; set; } = string.Empty;
    public int TotalLits { get; set; }
    public int LitsOccupes { get; set; }
    public decimal TauxOccupation { get; set; }
}

public class ChambreOccupationDto
{
    public int IdChambre { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string? Service { get; set; }
    public int Capacite { get; set; }
    public int Occupes { get; set; }
    public int Libres { get; set; }
    public int Reserves { get; set; }
    public int EnMaintenance { get; set; }
    public List<LitOccupationDto> Lits { get; set; } = new();
}

public class LitOccupationDto
{
    public int IdLit { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string Statut { get; set; } = string.Empty;
    public int? IdPatient { get; set; }
    public string? NomPatient { get; set; }
    public DateTime? DateEntree { get; set; }
    public DateTime? DateSortiePrevue { get; set; }
    public string? MotifHospitalisation { get; set; }
}

public class AffectationRequest
{
    public int IdPatient { get; set; }
    public string? TypeChambre { get; set; } // standard, isolement, soins_intensifs
    public string? ServicePrefere { get; set; }
    public bool Urgence { get; set; }
    public DateTime? DateEntreePrevue { get; set; }
}

public class AffectationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? IdLit { get; set; }
    public string? NumeroLit { get; set; }
    public string? NumeroChambre { get; set; }
    public string? Raison { get; set; }
}

public class LitSuggestionDto
{
    public int IdLit { get; set; }
    public string NumeroLit { get; set; } = string.Empty;
    public string NumeroChambre { get; set; } = string.Empty;
    public string? Service { get; set; }
    public int Score { get; set; } // Score de pertinence
    public string Raison { get; set; } = string.Empty;
}

public class ReservationLitRequest
{
    public int IdLit { get; set; }
    public int IdPatient { get; set; }
    public DateTime DateReservation { get; set; }
    public DateTime? DateExpiration { get; set; }
    public string? Notes { get; set; }
}

public class ReservationLitDto
{
    public int IdReservation { get; set; }
    public int IdLit { get; set; }
    public string NumeroLit { get; set; } = string.Empty;
    public string NumeroChambre { get; set; } = string.Empty;
    public int IdPatient { get; set; }
    public string NomPatient { get; set; } = string.Empty;
    public DateTime DateReservation { get; set; }
    public DateTime? DateExpiration { get; set; }
    public string Statut { get; set; } = string.Empty;
}

public class TransfertRequest
{
    public int IdAdmission { get; set; }
    public int IdNouveauLit { get; set; }
    public string Motif { get; set; } = string.Empty;
}

public class TransfertResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? IdTransfert { get; set; }
}

public class TransfertHistoriqueDto
{
    public int IdTransfert { get; set; }
    public int IdPatient { get; set; }
    public string NomPatient { get; set; } = string.Empty;
    public int IdLitOrigine { get; set; }
    public string NumeroLitOrigine { get; set; } = string.Empty;
    public int IdLitDestination { get; set; }
    public string NumeroLitDestination { get; set; } = string.Empty;
    public string Motif { get; set; } = string.Empty;
    public DateTime DateTransfert { get; set; }
    public string? EffectuePar { get; set; }
}

public class LitMaintenanceDto
{
    public int IdLit { get; set; }
    public string NumeroLit { get; set; } = string.Empty;
    public string NumeroChambre { get; set; } = string.Empty;
    public string Motif { get; set; } = string.Empty;
    public DateTime DateDebut { get; set; }
    public DateTime? DateFinPrevue { get; set; }
}

public class OccupationStatsDto
{
    public decimal TauxOccupationMoyen { get; set; }
    public decimal DureeMoyenneSejour { get; set; }
    public int NombreAdmissions { get; set; }
    public int NombreSorties { get; set; }
    public int NombreTransferts { get; set; }
    public List<OccupationJournaliereDto> OccupationJournaliere { get; set; } = new();
}

public class OccupationJournaliereDto
{
    public DateTime Date { get; set; }
    public int LitsOccupes { get; set; }
    public int TotalLits { get; set; }
    public decimal TauxOccupation { get; set; }
}
