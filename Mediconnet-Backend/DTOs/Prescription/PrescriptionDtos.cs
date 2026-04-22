using System.Text.Json.Serialization;
namespace Mediconnet_Backend.DTOs.Prescription;

// ==================== RequÃªtes de crÃ©ation ====================

/// <summary>
/// RequÃªte de base pour crÃ©er une ordonnance avec mÃ©dicaments
/// </summary>
public class CreateOrdonnanceRequest
{
    /// <summary>
    /// ID du patient (obligatoire)
    /// </summary>
    [JsonRequired]
    public int IdPatient { get; set; }

    /// <summary>
    /// ID de la consultation (optionnel - pour prescription en consultation)
    /// </summary>
    public int? IdConsultation { get; set; }

    /// <summary>
    /// ID de l'hospitalisation (optionnel - pour prescription en hospitalisation)
    /// </summary>
    public int? IdHospitalisation { get; set; }

    /// <summary>
    /// Notes/commentaires de l'ordonnance
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Liste des mÃ©dicaments Ã  prescrire
    /// </summary>
    public List<MedicamentPrescriptionRequest> Medicaments { get; set; } = new();

    // FonctionnalitÃ©s avancÃ©es (optionnelles)
    
    /// <summary>
    /// DurÃ©e de validitÃ© en jours (dÃ©faut: 90 jours)
    /// </summary>
    public int DureeValiditeJours { get; set; } = 90;

    /// <summary>
    /// Indique si l'ordonnance est renouvelable
    /// </summary>
    public bool Renouvelable { get; set; } = false;

    /// <summary>
    /// Nombre de renouvellements autorisÃ©s (si renouvelable)
    /// </summary>
    public int? NombreRenouvellements { get; set; }
}

/// <summary>
/// DÃ©tails d'un mÃ©dicament Ã  prescrire
/// </summary>
public class MedicamentPrescriptionRequest
{
    /// <summary>
    /// ID du mÃ©dicament (si connu via autocomplete)
    /// </summary>
    public int? IdMedicament { get; set; }

    /// <summary>
    /// Nom du mÃ©dicament (utilisÃ© si IdMedicament non fourni)
    /// </summary>
    public string NomMedicament { get; set; } = "";

    /// <summary>
    /// Dosage (ex: "500mg", "1g")
    /// </summary>
    public string? Dosage { get; set; }

    /// <summary>
    /// QuantitÃ© prescrite
    /// </summary>
    public int Quantite { get; set; } = 1;

    /// <summary>
    /// Posologie (ex: "1 comprimÃ© 3 fois par jour")
    /// </summary>
    public string? Posologie { get; set; }

    /// <summary>
    /// FrÃ©quence (ex: "3x/jour", "matin et soir")
    /// </summary>
    public string? Frequence { get; set; }

    /// <summary>
    /// DurÃ©e du traitement (ex: "7 jours", "1 mois")
    /// </summary>
    public string? DureeTraitement { get; set; }

    /// <summary>
    /// Voie d'administration (ex: "orale", "intraveineuse")
    /// </summary>
    public string? VoieAdministration { get; set; }

    /// <summary>
    /// Forme pharmaceutique (ex: "comprimÃ©", "sirop")
    /// </summary>
    public string? FormePharmaceutique { get; set; }

    /// <summary>
    /// Instructions spÃ©ciales
    /// </summary>
    public string? Instructions { get; set; }
}

// ==================== RÃ©ponses ====================

/// <summary>
/// RÃ©sultat de la crÃ©ation d'une ordonnance
/// </summary>
public class OrdonnanceResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int? IdOrdonnance { get; set; }
    public OrdonnanceDto? Ordonnance { get; set; }
    public List<string> Erreurs { get; set; } = new();
    public List<AlertePrescription> Alertes { get; set; } = new();
}

/// <summary>
/// Alerte lors de la prescription (stock, interaction, etc.)
/// </summary>
public class AlertePrescription
{
    public string Type { get; set; } = ""; // stock_faible, rupture, interaction, allergie
    public string Severite { get; set; } = "warning"; // info, warning, error
    public string Message { get; set; } = "";
    public int? IdMedicament { get; set; }
    public string? NomMedicament { get; set; }
}

/// <summary>
/// DTO complet d'une ordonnance
/// </summary>
public class OrdonnanceDto
{
    public int IdOrdonnance { get; set; }
    public DateTime Date { get; set; }
    public int IdPatient { get; set; }
    public string NomPatient { get; set; } = "";
    public int IdMedecin { get; set; }
    public string NomMedecin { get; set; } = "";
    public int? IdConsultation { get; set; }
    public int? IdHospitalisation { get; set; }
    public string TypeContexte { get; set; } = ""; // consultation, hospitalisation, directe
    public string Statut { get; set; } = "active"; // active, dispensee, annulee, expiree
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<LignePrescriptionDto> Lignes { get; set; } = new();
    
    // FonctionnalitÃ©s avancÃ©es (ex-OrdonnanceElectronique)
    public DateTime? DateExpiration { get; set; }
    public bool Renouvelable { get; set; }
    public int? NombreRenouvellements { get; set; }
    public int? RenouvellementRestants { get; set; }
    public int? IdOrdonnanceOriginale { get; set; }
    public bool EstExpire => DateExpiration.HasValue && DateExpiration.Value < DateTime.UtcNow;
}

/// <summary>
/// DTO d'une ligne de prescription (mÃ©dicament prescrit)
/// Supporte les mÃ©dicaments du catalogue ET les mÃ©dicaments en saisie libre (hors catalogue)
/// </summary>
public class LignePrescriptionDto
{
    public int IdPrescriptionMed { get; set; }
    
    /// <summary>
    /// ID du mÃ©dicament dans le catalogue (null si hors catalogue)
    /// </summary>
    public int? IdMedicament { get; set; }
    
    /// <summary>
    /// Nom du mÃ©dicament (catalogue ou saisie libre)
    /// </summary>
    public string NomMedicament { get; set; } = "";
    
    public string? Dosage { get; set; }
    
    /// <summary>
    /// Indique si le mÃ©dicament est hors catalogue (saisie libre par le mÃ©decin)
    /// </summary>
    public bool EstHorsCatalogue { get; set; } = false;
    
    public int Quantite { get; set; }
    public string? Posologie { get; set; }
    public string? Frequence { get; set; }
    public string? DureeTraitement { get; set; }
    public string? VoieAdministration { get; set; }
    public string? FormePharmaceutique { get; set; }
    public string? Instructions { get; set; }
    public int QuantiteDispensee { get; set; }
    public bool EstDispense { get; set; }
}

// ==================== Filtres et recherche ====================

/// <summary>
/// Filtre pour rechercher des ordonnances
/// </summary>
public class FiltreOrdonnanceRequest
{
    public int? IdPatient { get; set; }
    public int? IdMedecin { get; set; }
    public int? IdConsultation { get; set; }
    public int? IdHospitalisation { get; set; }
    public string? Statut { get; set; }
    public string? TypeContexte { get; set; }
    public DateTime? DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// RequÃªte pour annuler une ordonnance
/// </summary>
public class AnnulerOrdonnanceRequest
{
    public string Motif { get; set; } = "";
}

// ==================== Validation ====================

/// <summary>
/// RÃ©sultat de validation d'une prescription
/// </summary>
public class ValidationPrescriptionResult
{
    public bool EstValide { get; set; }
    public List<string> Erreurs { get; set; } = new();
    public List<AlertePrescription> Alertes { get; set; } = new();
}
