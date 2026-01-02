namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Service de facturation avancée
/// </summary>
public interface IFactureService
{
    // Génération PDF
    Task<byte[]> GenerateFacturePdfAsync(int idFacture);
    Task<byte[]> GenerateRecuPdfAsync(int idTransaction);
    
    // Paiements échelonnés
    Task<EcheancierDto> CreerEcheancierAsync(CreateEcheancierRequest request);
    Task<EcheancierDto?> GetEcheancierAsync(int idFacture);
    Task<List<EcheanceDto>> GetEcheancesEnRetardAsync();
    Task<bool> MarquerEcheancePayeeAsync(int idEcheance, int transactionId);
    
    // Intégration assurances
    Task<DemandeRemboursementDto> CreerDemandeRemboursementAsync(CreateDemandeRemboursementRequest request);
    Task<List<DemandeRemboursementDto>> GetDemandesRemboursementAsync(int? idAssurance = null, string? statut = null);
    Task<DemandeRemboursementDto> TraiterDemandeRemboursementAsync(int idDemande, TraiterDemandeRequest request);
    Task<decimal> CalculerCouvertureAssuranceAsync(int idPatient, decimal montantTotal, string typeActe);
}

// DTOs pour la facturation avancée
public class EcheancierDto
{
    public int IdEcheancier { get; set; }
    public int IdFacture { get; set; }
    public string NumeroFacture { get; set; } = string.Empty;
    public decimal MontantTotal { get; set; }
    public int NombreEcheances { get; set; }
    public decimal MontantParEcheance { get; set; }
    public DateTime DateDebut { get; set; }
    public string Frequence { get; set; } = "mensuel"; // mensuel, bimensuel, hebdomadaire
    public string Statut { get; set; } = "actif";
    public List<EcheanceDto> Echeances { get; set; } = new();
}

public class EcheanceDto
{
    public int IdEcheance { get; set; }
    public int NumeroEcheance { get; set; }
    public decimal Montant { get; set; }
    public DateTime DateEcheance { get; set; }
    public DateTime? DatePaiement { get; set; }
    public string Statut { get; set; } = "en_attente"; // en_attente, payee, en_retard
    public int? IdTransaction { get; set; }
    public int JoursRetard { get; set; }
}

public class CreateEcheancierRequest
{
    public int IdFacture { get; set; }
    public int NombreEcheances { get; set; }
    public DateTime DatePremierPaiement { get; set; }
    public string Frequence { get; set; } = "mensuel";
}

public class DemandeRemboursementDto
{
    public int IdDemande { get; set; }
    public string NumeroDemande { get; set; } = string.Empty;
    public int IdFacture { get; set; }
    public string NumeroFacture { get; set; } = string.Empty;
    public int IdAssurance { get; set; }
    public string NomAssurance { get; set; } = string.Empty;
    public int IdPatient { get; set; }
    public string NomPatient { get; set; } = string.Empty;
    public decimal MontantDemande { get; set; }
    public decimal? MontantApprouve { get; set; }
    public string Statut { get; set; } = "en_attente"; // en_attente, approuvee, rejetee, payee
    public DateTime DateDemande { get; set; }
    public DateTime? DateTraitement { get; set; }
    public string? MotifRejet { get; set; }
    public string? ReferenceAssurance { get; set; }
}

public class CreateDemandeRemboursementRequest
{
    public int IdFacture { get; set; }
    public decimal MontantDemande { get; set; }
    public string? Justificatif { get; set; }
}

public class TraiterDemandeRequest
{
    public string Decision { get; set; } = string.Empty; // approuvee, rejetee
    public decimal? MontantApprouve { get; set; }
    public string? MotifRejet { get; set; }
    public string? ReferenceAssurance { get; set; }
}
