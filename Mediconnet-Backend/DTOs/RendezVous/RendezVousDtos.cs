using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mediconnet_Backend.DTOs.RendezVous;

/// <summary>
/// DTO pour afficher un rendez-vous
/// </summary>
public class RendezVousDto
{
    public int IdRendezVous { get; set; }
    public int IdPatient { get; set; }
    public string PatientNom { get; set; } = string.Empty;
    public string PatientPrenom { get; set; } = string.Empty;
    public string? NumeroDossier { get; set; }
    public int IdMedecin { get; set; }
    public string MedecinNom { get; set; } = string.Empty;
    public string MedecinPrenom { get; set; } = string.Empty;
    public string? MedecinSpecialite { get; set; }
    public int? IdService { get; set; }
    public string? ServiceNom { get; set; }
    public DateTime DateHeure { get; set; }
    public int Duree { get; set; }
    public string Statut { get; set; } = string.Empty;
    public string? Motif { get; set; }
    public string? Notes { get; set; }
    public string TypeRdv { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; }
    
    // Informations d'orientation (si RDV crÃ©Ã© depuis une orientation)
    public int? IdOrientation { get; set; }
    public string? MotifOrientation { get; set; }
    public string? MedecinOrienteur { get; set; }
    public string? TypeOrientation { get; set; }
}

/// <summary>
/// DTO pour la liste des rendez-vous (vue simplifiÃ©e)
/// </summary>
public class RendezVousListDto
{
    public int IdRendezVous { get; set; }
    public int? IdConsultation { get; set; }
    public DateTime DateHeure { get; set; }
    public int Duree { get; set; }
    public string Statut { get; set; } = string.Empty;
    public string TypeRdv { get; set; } = string.Empty;
    public string? Motif { get; set; }
    public string MedecinNom { get; set; } = string.Empty;
    public string? ServiceNom { get; set; }
    public bool AnamneseRemplie { get; set; }
}

/// <summary>
/// DTO pour crÃ©er un rendez-vous
/// </summary>
public class CreateRendezVousRequest
{
    [Required]
    [JsonRequired]
    public int IdMedecin { get; set; }

    public int? IdService { get; set; }

    [Required]
    [JsonRequired]
    public DateTime DateHeure { get; set; }

    public int Duree { get; set; } = 30;

    [MaxLength(100)]
    public string? Motif { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(50)]
    public string TypeRdv { get; set; } = "consultation";
}

/// <summary>
/// DTO pour modifier un rendez-vous
/// </summary>
public class UpdateRendezVousRequest
{
    public DateTime? DateHeure { get; set; }
    public int? Duree { get; set; }

    [MaxLength(100)]
    public string? Motif { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(50)]
    public string? TypeRdv { get; set; }
}

/// <summary>
/// DTO pour annuler un rendez-vous
/// </summary>
public class AnnulerRendezVousRequest
{
    [Required]
    [JsonRequired]
    public int IdRendezVous { get; set; }

    [Required]
    [MaxLength(500)]
    public string Motif { get; set; } = string.Empty;
}

/// <summary>
/// Statut possible d'un crÃ©neau horaire
/// </summary>
public enum SlotStatus
{
    /// <summary>CrÃ©neau disponible pour rÃ©servation</summary>
    Disponible,
    /// <summary>CrÃ©neau dÃ©jÃ  rÃ©servÃ©</summary>
    Occupe,
    /// <summary>CrÃ©neau indisponible (congÃ©s, pause, etc.)</summary>
    Indisponible,
    /// <summary>CrÃ©neau temporairement verrouillÃ© par un autre utilisateur</summary>
    Verrouille,
    /// <summary>CrÃ©neau dans le passÃ©</summary>
    Passe
}

/// <summary>
/// DTO pour les crÃ©neaux disponibles avec statut dÃ©taillÃ©
/// </summary>
public class CreneauDisponibleDto
{
    public DateTime DateHeure { get; set; }
    public int Duree { get; set; }
    public bool Disponible { get; set; }
    
    /// <summary>Statut dÃ©taillÃ© du crÃ©neau</summary>
    public string Statut { get; set; } = "disponible";
    
    /// <summary>Raison si indisponible</summary>
    public string? Raison { get; set; }
    
    /// <summary>ID du rendez-vous si occupÃ©</summary>
    public int? IdRendezVous { get; set; }
}

/// <summary>
/// RÃ©ponse avec crÃ©neaux et statut de disponibilitÃ© mÃ©decin
/// </summary>
public class CreneauxDisponiblesResponse
{
    public bool MedecinDisponible { get; set; }
    public string? MessageIndisponibilite { get; set; }
    public List<CreneauDisponibleDto> Creneaux { get; set; } = new();
    
    /// <summary>Nombre de crÃ©neaux disponibles</summary>
    public int TotalDisponibles => Creneaux.Count(c => c.Disponible);
    
    /// <summary>Nombre de crÃ©neaux occupÃ©s</summary>
    public int TotalOccupes => Creneaux.Count(c => c.Statut == "occupe");
    
    /// <summary>Horodatage de la rÃ©ponse</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO pour rÃ©server un crÃ©neau (avec token de verrouillage)
/// </summary>
public class ReserverCreneauRequest
{
    public int IdMedecin { get; set; }
    public DateTime DateHeure { get; set; }
    public int Duree { get; set; } = 30;
}

/// <summary>
/// RÃ©ponse de rÃ©servation de crÃ©neau
/// </summary>
public class ReserverCreneauResponse
{
    public bool Success { get; set; }
    public string? LockToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour rechercher des crÃ©neaux
/// </summary>
public class RechercheCreneauxRequest
{
    [Required]
    public int IdMedecin { get; set; }

    [Required]
    public DateTime DateDebut { get; set; }

    [Required]
    public DateTime DateFin { get; set; }
}

/// <summary>
/// DTO pour les mÃ©decins disponibles
/// </summary>
public class MedecinDisponibleDto
{
    public int IdMedecin { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string? Specialite { get; set; }
    public string? ServiceNom { get; set; }
    public int? IdService { get; set; }
    public int ProchainCreneauDansJours { get; set; }
}

/// <summary>
/// Statistiques rendez-vous patient
/// </summary>
public class RendezVousStatsDto
{
    public int TotalRendezVous { get; set; }
    public int RendezVousAVenir { get; set; }
    public int RendezVousPasses { get; set; }
    public int RendezVousAnnules { get; set; }
    public RendezVousDto? ProchainRendezVous { get; set; }
}

/// <summary>
/// RequÃªte de mise Ã  jour du statut d'un rendez-vous
/// </summary>
public class UpdateStatutRequest
{
    [Required]
    [MaxLength(50)]
    public string Statut { get; set; } = "";
}

/// <summary>
/// RequÃªte pour valider un RDV (mÃ©decin)
/// </summary>
public class ValiderRdvRequest
{
    [Required]
    [JsonRequired]
    public int IdRendezVous { get; set; }
}

/// <summary>
/// RequÃªte pour annuler un RDV par le mÃ©decin
/// </summary>
public class AnnulerRdvMedecinRequest
{
    [Required]
    [JsonRequired]
    public int IdRendezVous { get; set; }

    [Required]
    [MaxLength(500)]
    public string Motif { get; set; } = string.Empty;
}

/// <summary>
/// RequÃªte pour suggÃ©rer un nouveau crÃ©neau
/// </summary>
public class SuggererCreneauRequest
{
    [Required]
    [JsonRequired]
    public int IdRendezVous { get; set; }

    [Required]
    [JsonRequired]
    public DateTime NouveauCreneau { get; set; }

    [MaxLength(500)]
    public string? Message { get; set; }
}

/// <summary>
/// RÃ©ponse de validation/action sur RDV
/// </summary>
public class ActionRdvResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public RendezVousDto? RendezVous { get; set; }
    public bool ConflitDetecte { get; set; }
}

/// <summary>
/// RequÃªte patient pour accepter une proposition de crÃ©neau
/// </summary>
public class AccepterPropositionRequest
{
    [Required]
    [JsonRequired]
    public int IdRendezVous { get; set; }
}

/// <summary>
/// RequÃªte patient pour refuser une proposition de crÃ©neau
/// </summary>
public class RefuserPropositionRequest
{
    [Required]
    [JsonRequired]
    public int IdRendezVous { get; set; }

    [MaxLength(500)]
    public string? Motif { get; set; }
}

/// <summary>
/// DTO pour les factures patient en attente
/// </summary>
public class FacturePatientDto
{
    public int IdFacture { get; set; }
    public string? NumeroFacture { get; set; }
    public int? IdRendezVous { get; set; }
    public DateTime? DateRendezVous { get; set; }
    public string? MedecinNom { get; set; }
    public string? ServiceNom { get; set; }
    public decimal MontantTotal { get; set; }
    public decimal MontantRestant { get; set; }
    public string Statut { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; }
    public DateTime? DateEcheance { get; set; }
    public bool CouvertureAssurance { get; set; }
    public decimal? TauxCouverture { get; set; }
    public decimal? MontantAssurance { get; set; }
}

/// <summary>
/// RequÃªte pour payer une facture en ligne
/// </summary>
public class PayerFactureEnLigneRequest
{
    [Required]
    [JsonRequired]
    public int IdFacture { get; set; }

    [Required]
    [MaxLength(50)]
    public string ModePaiement { get; set; } = "mobile_money"; // mobile_money, carte_bancaire

    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// RÃ©ponse de paiement en ligne
/// </summary>
public class PayerFactureEnLigneResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? NumeroTransaction { get; set; }
    public int? IdRendezVous { get; set; }
    public string? StatutRdv { get; set; }
}
