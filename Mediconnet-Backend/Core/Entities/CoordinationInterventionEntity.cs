using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Demande de coordination entre chirurgien et anesthésiste pour une intervention
/// Gère le workflow de validation avant programmation définitive
/// </summary>
[Table("coordination_intervention")]
public class CoordinationIntervention
{
    [Key]
    [Column("id_coordination")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdCoordination { get; set; }

    /// <summary>
    /// Programmation d'intervention associée
    /// </summary>
    [Column("id_programmation")]
    public int IdProgrammation { get; set; }

    /// <summary>
    /// Chirurgien demandeur
    /// </summary>
    [Column("id_chirurgien")]
    public int IdChirurgien { get; set; }

    /// <summary>
    /// Anesthésiste sélectionné
    /// </summary>
    [Column("id_anesthesiste")]
    public int IdAnesthesiste { get; set; }

    /// <summary>
    /// Date proposée par le chirurgien
    /// </summary>
    [Column("date_proposee")]
    public DateTime DateProposee { get; set; }

    /// <summary>
    /// Heure de début proposée (format HH:mm)
    /// </summary>
    [Column("heure_proposee")]
    [MaxLength(5)]
    public string HeureProposee { get; set; } = string.Empty;

    /// <summary>
    /// Durée estimée en minutes
    /// </summary>
    [Column("duree_estimee")]
    public int DureeEstimee { get; set; }

    /// <summary>
    /// Statut de la coordination : 
    /// proposee, validee, modifiee, refusee, annulee
    /// </summary>
    [Column("statut")]
    [MaxLength(20)]
    public string Statut { get; set; } = "proposee";

    /// <summary>
    /// Date/heure contre-proposée par l'anesthésiste (si modifiée)
    /// </summary>
    [Column("date_contre_proposee")]
    public DateTime? DateContreProposee { get; set; }

    /// <summary>
    /// Heure contre-proposée (format HH:mm)
    /// </summary>
    [Column("heure_contre_proposee")]
    [MaxLength(5)]
    public string? HeureContreProposee { get; set; }

    /// <summary>
    /// Commentaire de l'anesthésiste
    /// </summary>
    [Column("commentaire_anesthesiste")]
    public string? CommentaireAnesthesiste { get; set; }

    /// <summary>
    /// Motif de refus si refusée
    /// </summary>
    [Column("motif_refus")]
    public string? MotifRefus { get; set; }

    /// <summary>
    /// Notes du chirurgien pour l'anesthésiste
    /// </summary>
    [Column("notes_chirurgien")]
    public string? NotesChirurgien { get; set; }

    /// <summary>
    /// ID du RDV consultation anesthésiste créé automatiquement
    /// </summary>
    [Column("id_rdv_consultation_anesthesiste")]
    public int? IdRdvConsultationAnesthesiste { get; set; }

    /// <summary>
    /// ID de l'indisponibilité créée pour le chirurgien
    /// </summary>
    [Column("id_indisponibilite_chirurgien")]
    public int? IdIndisponibiliteChirurgien { get; set; }

    /// <summary>
    /// ID de l'indisponibilité créée pour l'anesthésiste
    /// </summary>
    [Column("id_indisponibilite_anesthesiste")]
    public int? IdIndisponibiliteAnesthesiste { get; set; }

    /// <summary>
    /// ID de la réservation de bloc opératoire
    /// </summary>
    [Column("id_reservation_bloc")]
    public int? IdReservationBloc { get; set; }

    /// <summary>
    /// Date de validation par l'anesthésiste
    /// </summary>
    [Column("date_validation")]
    public DateTime? DateValidation { get; set; }

    /// <summary>
    /// Date de réponse (validation, modification ou refus)
    /// </summary>
    [Column("date_reponse")]
    public DateTime? DateReponse { get; set; }

    /// <summary>
    /// Nombre de modifications (pour tracking)
    /// </summary>
    [Column("nb_modifications")]
    public int NbModifications { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(IdProgrammation))]
    public virtual ProgrammationIntervention Programmation { get; set; } = null!;

    [ForeignKey(nameof(IdChirurgien))]
    public virtual Medecin Chirurgien { get; set; } = null!;

    [ForeignKey(nameof(IdAnesthesiste))]
    public virtual Medecin Anesthesiste { get; set; } = null!;

    [ForeignKey(nameof(IdRdvConsultationAnesthesiste))]
    public virtual RendezVous? RdvConsultationAnesthesiste { get; set; }

    [ForeignKey(nameof(IdIndisponibiliteChirurgien))]
    public virtual IndisponibiliteMedecin? IndisponibiliteChirurgien { get; set; }

    [ForeignKey(nameof(IdIndisponibiliteAnesthesiste))]
    public virtual IndisponibiliteMedecin? IndisponibiliteAnesthesiste { get; set; }

    [ForeignKey(nameof(IdReservationBloc))]
    public virtual ReservationBloc? ReservationBloc { get; set; }
}

/// <summary>
/// Historique des échanges de coordination
/// </summary>
[Table("coordination_intervention_historique")]
public class CoordinationInterventionHistorique
{
    [Key]
    [Column("id_historique")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdHistorique { get; set; }

    [Column("id_coordination")]
    public int IdCoordination { get; set; }

    /// <summary>
    /// Type d'action : proposition, validation, modification, refus, annulation
    /// </summary>
    [Column("type_action")]
    [MaxLength(30)]
    public string TypeAction { get; set; } = string.Empty;

    /// <summary>
    /// ID de l'utilisateur ayant effectué l'action
    /// </summary>
    [Column("id_user_action")]
    public int IdUserAction { get; set; }

    /// <summary>
    /// Rôle de l'utilisateur (chirurgien, anesthesiste)
    /// </summary>
    [Column("role_user")]
    [MaxLength(20)]
    public string RoleUser { get; set; } = string.Empty;

    /// <summary>
    /// Détails de l'action (JSON ou texte)
    /// </summary>
    [Column("details")]
    public string? Details { get; set; }

    /// <summary>
    /// Date/heure proposée dans cette action
    /// </summary>
    [Column("date_proposee")]
    public DateTime? DateProposee { get; set; }

    /// <summary>
    /// Heure proposée
    /// </summary>
    [Column("heure_proposee")]
    [MaxLength(5)]
    public string? HeureProposee { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(IdCoordination))]
    public virtual CoordinationIntervention Coordination { get; set; } = null!;

    [ForeignKey(nameof(IdUserAction))]
    public virtual Utilisateur UserAction { get; set; } = null!;
}
