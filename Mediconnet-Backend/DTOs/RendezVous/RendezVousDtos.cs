using System.ComponentModel.DataAnnotations;

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
}

/// <summary>
/// DTO pour la liste des rendez-vous (vue simplifiée)
/// </summary>
public class RendezVousListDto
{
    public int IdRendezVous { get; set; }
    public DateTime DateHeure { get; set; }
    public int Duree { get; set; }
    public string Statut { get; set; } = string.Empty;
    public string TypeRdv { get; set; } = string.Empty;
    public string? Motif { get; set; }
    public string MedecinNom { get; set; } = string.Empty;
    public string? ServiceNom { get; set; }
}

/// <summary>
/// DTO pour créer un rendez-vous
/// </summary>
public class CreateRendezVousRequest
{
    [Required]
    public int IdMedecin { get; set; }

    public int? IdService { get; set; }

    [Required]
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
    public int IdRendezVous { get; set; }

    [Required]
    [MaxLength(500)]
    public string Motif { get; set; } = string.Empty;
}

/// <summary>
/// Statut possible d'un créneau horaire
/// </summary>
public enum SlotStatus
{
    /// <summary>Créneau disponible pour réservation</summary>
    Disponible,
    /// <summary>Créneau déjà réservé</summary>
    Occupe,
    /// <summary>Créneau indisponible (congés, pause, etc.)</summary>
    Indisponible,
    /// <summary>Créneau temporairement verrouillé par un autre utilisateur</summary>
    Verrouille,
    /// <summary>Créneau dans le passé</summary>
    Passe
}

/// <summary>
/// DTO pour les créneaux disponibles avec statut détaillé
/// </summary>
public class CreneauDisponibleDto
{
    public DateTime DateHeure { get; set; }
    public int Duree { get; set; }
    public bool Disponible { get; set; }
    
    /// <summary>Statut détaillé du créneau</summary>
    public string Statut { get; set; } = "disponible";
    
    /// <summary>Raison si indisponible</summary>
    public string? Raison { get; set; }
    
    /// <summary>ID du rendez-vous si occupé</summary>
    public int? IdRendezVous { get; set; }
}

/// <summary>
/// Réponse avec créneaux et statut de disponibilité médecin
/// </summary>
public class CreneauxDisponiblesResponse
{
    public bool MedecinDisponible { get; set; }
    public string? MessageIndisponibilite { get; set; }
    public List<CreneauDisponibleDto> Creneaux { get; set; } = new();
    
    /// <summary>Nombre de créneaux disponibles</summary>
    public int TotalDisponibles => Creneaux.Count(c => c.Disponible);
    
    /// <summary>Nombre de créneaux occupés</summary>
    public int TotalOccupes => Creneaux.Count(c => c.Statut == "occupe");
    
    /// <summary>Horodatage de la réponse</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO pour réserver un créneau (avec token de verrouillage)
/// </summary>
public class ReserverCreneauRequest
{
    public int IdMedecin { get; set; }
    public DateTime DateHeure { get; set; }
    public int Duree { get; set; } = 30;
}

/// <summary>
/// Réponse de réservation de créneau
/// </summary>
public class ReserverCreneauResponse
{
    public bool Success { get; set; }
    public string? LockToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour rechercher des créneaux
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
/// DTO pour les médecins disponibles
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
/// Requête de mise à jour du statut d'un rendez-vous
/// </summary>
public class UpdateStatutRequest
{
    [Required]
    [MaxLength(50)]
    public string Statut { get; set; } = "";
}

/// <summary>
/// Requête pour valider un RDV (médecin)
/// </summary>
public class ValiderRdvRequest
{
    [Required]
    public int IdRendezVous { get; set; }
}

/// <summary>
/// Requête pour annuler un RDV par le médecin
/// </summary>
public class AnnulerRdvMedecinRequest
{
    [Required]
    public int IdRendezVous { get; set; }

    [Required]
    [MaxLength(500)]
    public string Motif { get; set; } = string.Empty;
}

/// <summary>
/// Requête pour suggérer un nouveau créneau
/// </summary>
public class SuggererCreneauRequest
{
    [Required]
    public int IdRendezVous { get; set; }

    [Required]
    public DateTime NouveauCreneau { get; set; }

    [MaxLength(500)]
    public string? Message { get; set; }
}

/// <summary>
/// Réponse de validation/action sur RDV
/// </summary>
public class ActionRdvResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public RendezVousDto? RendezVous { get; set; }
    public bool ConflitDetecte { get; set; }
}

/// <summary>
/// Requête patient pour accepter une proposition de créneau
/// </summary>
public class AccepterPropositionRequest
{
    [Required]
    public int IdRendezVous { get; set; }
}

/// <summary>
/// Requête patient pour refuser une proposition de créneau
/// </summary>
public class RefuserPropositionRequest
{
    [Required]
    public int IdRendezVous { get; set; }

    [MaxLength(500)]
    public string? Motif { get; set; }
}
