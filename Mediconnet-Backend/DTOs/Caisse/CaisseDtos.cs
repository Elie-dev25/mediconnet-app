using System.ComponentModel.DataAnnotations;

namespace Mediconnet_Backend.DTOs.Caisse;

// ==================== KPI DTOs ====================

/// <summary>
/// KPIs du dashboard caissier
/// </summary>
public class CaisseKpiDto
{
    public decimal RevenuJour { get; set; }
    public int NombreTransactionsJour { get; set; }
    public int FacturesEnAttente { get; set; }
    public decimal SoldeCaisse { get; set; }
    public decimal EcartCaisse { get; set; }
    public decimal RemboursementsJour { get; set; }
    public int AnnulationsJour { get; set; }
    public bool CaisseOuverte { get; set; }
    public int? IdSessionActive { get; set; }
}

// ==================== FACTURE DTOs ====================

public class FactureDto
{
    public int IdFacture { get; set; }
    public string NumeroFacture { get; set; } = string.Empty;
    public int IdPatient { get; set; }
    public string PatientNom { get; set; } = string.Empty;
    public string PatientPrenom { get; set; } = string.Empty;
    public string? NumeroDossier { get; set; }
    public decimal MontantTotal { get; set; }
    public decimal MontantPaye { get; set; }
    public decimal MontantRestant { get; set; }
    public string Statut { get; set; } = string.Empty;
    public string? TypeFacture { get; set; }
    public DateTime DateCreation { get; set; }
    public DateTime? DateEcheance { get; set; }
    public string? ServiceNom { get; set; }
    
    // Informations assurance
    public bool CouvertureAssurance { get; set; }
    public decimal? TauxCouverture { get; set; }
    public decimal? MontantAssurance { get; set; }
    public decimal MontantPatient => MontantTotal - (MontantAssurance ?? 0);
    public string? NomAssurance { get; set; }
    
    public List<LigneFactureDto> Lignes { get; set; } = new();
}

public class LigneFactureDto
{
    public int IdLigne { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int Quantite { get; set; }
    public decimal PrixUnitaire { get; set; }
    public decimal Montant { get; set; }
    public string? Categorie { get; set; }
}

public class FactureListItemDto
{
    public int IdFacture { get; set; }
    public string NumeroFacture { get; set; } = string.Empty;
    public string PatientNom { get; set; } = string.Empty;
    public string? NumeroDossier { get; set; }
    public decimal MontantTotal { get; set; }
    public decimal MontantRestant { get; set; }
    public string Statut { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; }
    public DateTime? DateEcheance { get; set; }
    
    // Informations assurance
    public bool CouvertureAssurance { get; set; }
    public decimal? TauxCouverture { get; set; }
    public decimal? MontantAssurance { get; set; }
    public string? NomAssurance { get; set; }
}

// ==================== TRANSACTION DTOs ====================

public class TransactionDto
{
    public int IdTransaction { get; set; }
    public string NumeroTransaction { get; set; } = string.Empty;
    public int IdFacture { get; set; }
    public string NumeroFacture { get; set; } = string.Empty;
    public string PatientNom { get; set; } = string.Empty;
    public string? NumeroDossier { get; set; }
    public decimal Montant { get; set; }
    public string ModePaiement { get; set; } = string.Empty;
    public string Statut { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public DateTime DateTransaction { get; set; }
    public string CaissierNom { get; set; } = string.Empty;
    public decimal? MontantRecu { get; set; }
    public decimal? RenduMonnaie { get; set; }
}

public class CreateTransactionRequest
{
    [Required]
    public int IdFacture { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Le montant doit être positif")]
    public decimal Montant { get; set; }

    [Required]
    [MaxLength(30)]
    public string ModePaiement { get; set; } = "especes";

    public decimal? MontantRecu { get; set; }

    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Token unique pour éviter les doublons (idempotence)
    /// </summary>
    public string? IdempotencyToken { get; set; }
}

public class AnnulerTransactionRequest
{
    [Required]
    public int IdTransaction { get; set; }

    [Required]
    [MaxLength(500)]
    public string Motif { get; set; } = string.Empty;
}

// ==================== SESSION CAISSE DTOs ====================

public class SessionCaisseDto
{
    public int IdSession { get; set; }
    public string CaissierNom { get; set; } = string.Empty;
    public decimal MontantOuverture { get; set; }
    public decimal? MontantFermeture { get; set; }
    public decimal? MontantSysteme { get; set; }
    public decimal? Ecart { get; set; }
    public DateTime DateOuverture { get; set; }
    public DateTime? DateFermeture { get; set; }
    public string Statut { get; set; } = string.Empty;
    public int NombreTransactions { get; set; }
    public decimal TotalEncaisse { get; set; }
}

public class OuvrirCaisseRequest
{
    [Required]
    [Range(0, double.MaxValue)]
    public decimal MontantOuverture { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class FermerCaisseRequest
{
    [Required]
    [Range(0, double.MaxValue)]
    public decimal MontantFermeture { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

// ==================== PATIENT SEARCH DTOs ====================

public class PatientSearchResultDto
{
    public int IdPatient { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string? NumeroDossier { get; set; }
    public string? Telephone { get; set; }
    public int FacturesEnAttente { get; set; }
}

// ==================== STATISTIQUES DTOs ====================

public class RepartitionPaiementDto
{
    public string ModePaiement { get; set; } = string.Empty;
    public decimal Montant { get; set; }
    public int Nombre { get; set; }
    public decimal Pourcentage { get; set; }
}

public class RevenuParServiceDto
{
    public string ServiceNom { get; set; } = string.Empty;
    public decimal Montant { get; set; }
}

public class FactureRetardDto
{
    public int IdFacture { get; set; }
    public string NumeroFacture { get; set; } = string.Empty;
    public string PatientNom { get; set; } = string.Empty;
    public decimal MontantRestant { get; set; }
    public int JoursRetard { get; set; }
}

// ==================== REÇU DTOs ====================

public class RecuTransactionDto
{
    public string NumeroRecu { get; set; } = string.Empty;
    public string NumeroTransaction { get; set; } = string.Empty;
    public string NumeroFacture { get; set; } = string.Empty;
    public DateTime DateTransaction { get; set; }
    
    // Informations patient
    public string PatientNom { get; set; } = string.Empty;
    public string PatientPrenom { get; set; } = string.Empty;
    public string? NumeroDossier { get; set; }
    public string? Telephone { get; set; }
    
    // Informations paiement
    public decimal MontantTotal { get; set; }
    public decimal MontantPaye { get; set; }
    public decimal? MontantRecu { get; set; }
    public decimal? RenduMonnaie { get; set; }
    public string ModePaiement { get; set; } = string.Empty;
    public string? Reference { get; set; }
    
    // Informations assurance
    public bool CouvertureAssurance { get; set; }
    public string? NomAssurance { get; set; }
    public decimal? TauxCouverture { get; set; }
    public decimal? MontantAssurance { get; set; }
    public decimal MontantPatient { get; set; }
    
    // Détails facture
    public string? TypeFacture { get; set; }
    public string? ServiceNom { get; set; }
    public string? MedecinNom { get; set; }
    public List<LigneFactureDto> Lignes { get; set; } = new();
    
    // Informations caissier
    public string CaissierNom { get; set; } = string.Empty;
    
    // Établissement
    public string NomEtablissement { get; set; } = "Centre Médical Mediconnet";
    public string AdresseEtablissement { get; set; } = "Adresse de l'établissement";
    public string TelephoneEtablissement { get; set; } = "+XXX XX XX XX XX";
}
