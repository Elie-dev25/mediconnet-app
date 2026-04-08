using System;
using System.ComponentModel.DataAnnotations;

namespace Mediconnet_Backend.DTOs.Chirurgie;

// ==================== DTOs de lecture ====================

/// <summary>
/// DTO pour afficher une coordination d'intervention
/// </summary>
public class CoordinationInterventionDto
{
    public int IdCoordination { get; set; }
    public int IdProgrammation { get; set; }
    public int IdChirurgien { get; set; }
    public string NomChirurgien { get; set; } = string.Empty;
    public string SpecialiteChirurgien { get; set; } = string.Empty;
    public int IdAnesthesiste { get; set; }
    public string NomAnesthesiste { get; set; } = string.Empty;
    public int IdPatient { get; set; }
    public string NomPatient { get; set; } = string.Empty;
    public string IndicationOperatoire { get; set; } = string.Empty;
    public string TypeIntervention { get; set; } = string.Empty;
    public DateTime DateProposee { get; set; }
    public string HeureProposee { get; set; } = string.Empty;
    public int DureeEstimee { get; set; }
    public string Statut { get; set; } = string.Empty;
    public DateTime? DateContreProposee { get; set; }
    public string? HeureContreProposee { get; set; }
    public string? CommentaireAnesthesiste { get; set; }
    public string? MotifRefus { get; set; }
    public string? NotesChirurgien { get; set; }
    public string? NotesAnesthesie { get; set; }
    public string? ClassificationAsa { get; set; }
    public string? RisqueOperatoire { get; set; }
    public int? IdRdvConsultationAnesthesiste { get; set; }
    public DateTime? DateRdvConsultation { get; set; }
    public DateTime? DateValidation { get; set; }
    public DateTime? DateReponse { get; set; }
    public int NbModifications { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO pour l'agenda d'un anesthésiste (créneaux disponibles)
/// </summary>
public class CreneauDisponibleDto
{
    public DateTime Date { get; set; }
    public string HeureDebut { get; set; } = string.Empty;
    public string HeureFin { get; set; } = string.Empty;
    public int DureeMinutes { get; set; }
    public bool EstDisponible { get; set; }
    public string? MotifIndisponibilite { get; set; }
}

/// <summary>
/// DTO pour afficher un anesthésiste avec ses disponibilités
/// </summary>
public class AnesthesisteDisponibiliteDto
{
    public int IdMedecin { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string NomComplet => $"Dr. {Prenom} {Nom}";
    public string? Photo { get; set; }
    public int NbInterventionsSemaine { get; set; }
    public List<CreneauDisponibleDto> CreneauxDisponibles { get; set; } = new();
}

/// <summary>
/// DTO pour l'historique d'une coordination
/// </summary>
public class CoordinationHistoriqueDto
{
    public int IdHistorique { get; set; }
    public string TypeAction { get; set; } = string.Empty;
    public string NomUser { get; set; } = string.Empty;
    public string RoleUser { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime? DateProposee { get; set; }
    public string? HeureProposee { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ==================== DTOs de création/modification ====================

/// <summary>
/// Requête pour proposer une coordination (par le chirurgien)
/// </summary>
public class ProposerCoordinationRequest
{
    [Required]
    public int IdProgrammation { get; set; }

    [Required]
    public int IdAnesthesiste { get; set; }

    [Required]
    public DateTime DateProposee { get; set; }

    [Required]
    [RegularExpression(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Format heure invalide (HH:mm)")]
    public string HeureProposee { get; set; } = string.Empty;

    [Required]
    [Range(15, 720, ErrorMessage = "Durée entre 15 min et 12h")]
    public int DureeEstimee { get; set; }

    public string? NotesChirurgien { get; set; }
}

/// <summary>
/// Requête pour valider une coordination (par l'anesthésiste)
/// </summary>
public class ValiderCoordinationRequest
{
    [Required]
    public int IdCoordination { get; set; }

    public string? CommentaireAnesthesiste { get; set; }

    /// <summary>
    /// Date du RDV de consultation pré-opératoire (optionnel, l'anesthésiste peut le planifier plus tard)
    /// </summary>
    public DateTime? DateRdvConsultation { get; set; }

    /// <summary>
    /// Heure du RDV de consultation pré-opératoire (format HH:mm)
    /// </summary>
    [RegularExpression(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Format heure invalide (HH:mm)")]
    public string? HeureRdvConsultation { get; set; }
}

/// <summary>
/// Requête pour modifier/contre-proposer une coordination (par l'anesthésiste)
/// </summary>
public class ModifierCoordinationRequest
{
    [Required]
    public int IdCoordination { get; set; }

    [Required]
    public DateTime DateContreProposee { get; set; }

    [Required]
    [RegularExpression(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Format heure invalide (HH:mm)")]
    public string HeureContreProposee { get; set; } = string.Empty;

    [Required]
    public string CommentaireAnesthesiste { get; set; } = string.Empty;
}

/// <summary>
/// Requête pour refuser une coordination (par l'anesthésiste)
/// </summary>
public class RefuserCoordinationRequest
{
    [Required]
    public int IdCoordination { get; set; }

    [Required]
    [MinLength(10, ErrorMessage = "Le motif de refus doit contenir au moins 10 caractères")]
    public string MotifRefus { get; set; } = string.Empty;
}

/// <summary>
/// Requête pour accepter une contre-proposition (par le chirurgien)
/// </summary>
public class AccepterContrePropositionRequest
{
    [Required]
    public int IdCoordination { get; set; }

    public string? NotesChirurgien { get; set; }
}

/// <summary>
/// Requête pour refuser une contre-proposition (par le chirurgien)
/// </summary>
public class RefuserContrePropositionRequest
{
    [Required]
    public int IdCoordination { get; set; }

    [Required]
    [MinLength(10, ErrorMessage = "Le motif de refus doit contenir au moins 10 caractères")]
    public string MotifRefus { get; set; } = string.Empty;

    /// <summary>
    /// Si true, permet de relancer avec un autre anesthésiste
    /// </summary>
    public bool RelancerAvecAutre { get; set; } = false;
}

/// <summary>
/// Requête pour annuler une coordination
/// </summary>
public class AnnulerCoordinationRequest
{
    [Required]
    public int IdCoordination { get; set; }

    [Required]
    [MinLength(10, ErrorMessage = "Le motif d'annulation doit contenir au moins 10 caractères")]
    public string MotifAnnulation { get; set; } = string.Empty;
}

// ==================== DTOs de réponse ====================

/// <summary>
/// Réponse après action sur une coordination
/// </summary>
public class CoordinationActionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? IdCoordination { get; set; }
    public string? NouveauStatut { get; set; }
    public int? IdRdvConsultationAnesthesiste { get; set; }
    public DateTime? DateRdvConsultation { get; set; }
}

/// <summary>
/// DTO pour les statistiques de coordination d'un médecin
/// </summary>
public class CoordinationStatsDto
{
    public int EnAttente { get; set; }
    public int Validees { get; set; }
    public int Modifiees { get; set; }
    public int Refusees { get; set; }
    public int Total { get; set; }
}

/// <summary>
/// Filtre pour rechercher des coordinations
/// </summary>
public class CoordinationFilterDto
{
    public string? Statut { get; set; }
    public DateTime? DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public int? IdChirurgien { get; set; }
    public int? IdAnesthesiste { get; set; }
    public int? IdPatient { get; set; }
}
