namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Service de prescriptions électroniques - Intégration pharmacies externes
/// </summary>
public interface IPrescriptionElectroniqueService
{
    // Création et gestion des ordonnances électroniques
    Task<OrdonnanceElectroniqueDto> CreerOrdonnanceElectroniqueAsync(CreateOrdonnanceElectroniqueRequest request, int medecinId);
    Task<OrdonnanceElectroniqueDto?> GetOrdonnanceAsync(int idOrdonnance);
    Task<OrdonnanceElectroniqueDto?> GetOrdonnanceByCodeAsync(string codeUnique);
    Task<List<OrdonnanceElectroniqueDto>> GetOrdonnancesPatientAsync(int idPatient);
    
    // Transmission aux pharmacies
    Task<TransmissionResult> TransmettreAPharmacieAsync(int idOrdonnance, int idPharmacie);
    Task<List<PharmacieExterneDto>> GetPharmaciesPartenairesAsync(string? ville = null);
    Task<TransmissionResult> AnnulerTransmissionAsync(int idOrdonnance);
    
    // Suivi de dispensation
    Task<bool> MarquerDispenseeAsync(int idOrdonnance, DispensationExterneRequest request);
    Task<StatutDispensationDto> GetStatutDispensationAsync(int idOrdonnance);
    
    // QR Code / Code barre
    Task<byte[]> GenerateQRCodeAsync(int idOrdonnance);
    Task<OrdonnanceElectroniqueDto?> ScanOrdonnanceAsync(string codeScanne);
    
    // Renouvellement
    Task<OrdonnanceElectroniqueDto> RenouvelerOrdonnanceAsync(int idOrdonnance, int medecinId);
    Task<List<OrdonnanceElectroniqueDto>> GetOrdonnancesARenouvelerAsync(int medecinId);
}

// DTOs pour les prescriptions électroniques
public class OrdonnanceElectroniqueDto
{
    public int IdOrdonnance { get; set; }
    public string CodeUnique { get; set; } = string.Empty;
    public string QRCode { get; set; } = string.Empty;
    public int IdPatient { get; set; }
    public string NomPatient { get; set; } = string.Empty;
    public int IdMedecin { get; set; }
    public string NomMedecin { get; set; } = string.Empty;
    public string? NumeroOrdre { get; set; }
    public DateTime DatePrescription { get; set; }
    public DateTime DateExpiration { get; set; }
    public string Statut { get; set; } = "active"; // active, transmise, dispensee, expiree, annulee
    public bool Renouvelable { get; set; }
    public int? NombreRenouvellements { get; set; }
    public int? RenouvellementRestants { get; set; }
    public List<LignePrescriptionDto> Lignes { get; set; } = new();
    public string? Notes { get; set; }
    public TransmissionInfoDto? TransmissionInfo { get; set; }
}

public class LignePrescriptionDto
{
    public int IdLigne { get; set; }
    public int IdMedicament { get; set; }
    public string NomMedicament { get; set; } = string.Empty;
    public string? CodeCIP { get; set; }
    public string Dosage { get; set; } = string.Empty;
    public int Quantite { get; set; }
    public string Posologie { get; set; } = string.Empty;
    public string? DureeTraitement { get; set; }
    public string? Instructions { get; set; }
    public bool Substitutable { get; set; } = true;
    public bool Dispense { get; set; }
    public DateTime? DateDispensation { get; set; }
}

public class CreateOrdonnanceElectroniqueRequest
{
    public int IdPatient { get; set; }
    public int? IdConsultation { get; set; }
    public bool Renouvelable { get; set; }
    public int? NombreRenouvellements { get; set; }
    public int DureeValiditeJours { get; set; } = 90;
    public string? Notes { get; set; }
    public List<CreateLignePrescriptionRequest> Lignes { get; set; } = new();
}

public class CreateLignePrescriptionRequest
{
    public int IdMedicament { get; set; }
    public string Dosage { get; set; } = string.Empty;
    public int Quantite { get; set; }
    public string Posologie { get; set; } = string.Empty;
    public string? DureeTraitement { get; set; }
    public string? Instructions { get; set; }
    public bool Substitutable { get; set; } = true;
}

public class PharmacieExterneDto
{
    public int IdPharmacie { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Adresse { get; set; } = string.Empty;
    public string Ville { get; set; } = string.Empty;
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public bool EstConnectee { get; set; }
    public string? HorairesOuverture { get; set; }
    public double? Distance { get; set; }
}

public class TransmissionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ReferenceTransmission { get; set; }
    public DateTime? DateTransmission { get; set; }
}

public class TransmissionInfoDto
{
    public int IdPharmacie { get; set; }
    public string NomPharmacie { get; set; } = string.Empty;
    public DateTime DateTransmission { get; set; }
    public string Statut { get; set; } = string.Empty;
    public string? ReferenceExterne { get; set; }
}

public class DispensationExterneRequest
{
    public string ReferencePharmacie { get; set; } = string.Empty;
    public DateTime DateDispensation { get; set; }
    public List<LigneDispenseeRequest> LignesDispensees { get; set; } = new();
}

public class LigneDispenseeRequest
{
    public int IdLigne { get; set; }
    public int QuantiteDispensee { get; set; }
    public string? MedicamentSubstitue { get; set; }
}

public class StatutDispensationDto
{
    public int IdOrdonnance { get; set; }
    public string StatutGlobal { get; set; } = string.Empty;
    public int TotalLignes { get; set; }
    public int LignesDispensees { get; set; }
    public DateTime? DerniereDispensation { get; set; }
    public string? PharmacieDispensatrice { get; set; }
}
