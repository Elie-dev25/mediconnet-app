using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Programmation d'une intervention chirurgicale
/// Créée lorsque le chirurgien décide d'une indication opératoire
/// </summary>
[Table("programmation_intervention")]
public class ProgrammationIntervention
{
    [Key]
    [Column("id_programmation")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdProgrammation { get; set; }

    /// <summary>
    /// Consultation chirurgicale à l'origine de l'indication opératoire
    /// </summary>
    [Column("id_consultation")]
    public int IdConsultation { get; set; }

    [Column("id_patient")]
    public int IdPatient { get; set; }

    [Column("id_medecin")]
    public int IdChirurgien { get; set; }

    /// <summary>
    /// Type d'intervention : programmee, urgence, ambulatoire
    /// </summary>
    [Column("type_intervention")]
    [MaxLength(50)]
    public string TypeIntervention { get; set; } = "programmee";

    /// <summary>
    /// Classification ASA (1 à 5)
    /// </summary>
    [Column("classification_asa")]
    [MaxLength(10)]
    public string? ClassificationAsa { get; set; }

    /// <summary>
    /// Niveau de risque opératoire : faible, modere, eleve
    /// </summary>
    [Column("risque_operatoire")]
    [MaxLength(20)]
    public string? RisqueOperatoire { get; set; }

    /// <summary>
    /// Consentement éclairé obtenu
    /// </summary>
    [Column("consentement_eclaire")]
    public bool ConsentementEclaire { get; set; } = false;

    /// <summary>
    /// Date de signature du consentement
    /// </summary>
    [Column("date_consentement")]
    public DateTime? DateConsentement { get; set; }

    /// <summary>
    /// Indication opératoire (diagnostic justifiant l'intervention)
    /// </summary>
    [Column("indication_operatoire")]
    public string? IndicationOperatoire { get; set; }

    /// <summary>
    /// Technique chirurgicale prévue
    /// </summary>
    [Column("technique_prevue")]
    public string? TechniquePrevue { get; set; }

    /// <summary>
    /// Date prévue de l'intervention
    /// </summary>
    [Column("date_prevue")]
    public DateTime? DatePrevue { get; set; }

    /// <summary>
    /// Heure de début prévue (format HH:mm)
    /// </summary>
    [Column("heure_debut")]
    [MaxLength(5)]
    public string? HeureDebut { get; set; }

    /// <summary>
    /// Durée estimée de l'intervention en minutes
    /// </summary>
    [Column("duree_estimee")]
    public int? DureeEstimee { get; set; }

    /// <summary>
    /// ID de l'indisponibilité créée pour bloquer le créneau
    /// </summary>
    [Column("id_indisponibilite")]
    public int? IdIndisponibilite { get; set; }

    /// <summary>
    /// Navigation vers l'indisponibilité
    /// </summary>
    [ForeignKey(nameof(IdIndisponibilite))]
    public virtual IndisponibiliteMedecin? Indisponibilite { get; set; }

    /// <summary>
    /// Notes pour l'anesthésiste
    /// </summary>
    [Column("notes_anesthesie")]
    public string? NotesAnesthesie { get; set; }

    /// <summary>
    /// Bilan pré-opératoire requis
    /// </summary>
    [Column("bilan_preoperatoire")]
    public string? BilanPreoperatoire { get; set; }

    /// <summary>
    /// Instructions pré-opératoires pour le patient
    /// </summary>
    [Column("instructions_patient")]
    public string? InstructionsPatient { get; set; }

    /// <summary>
    /// Statut : en_attente_coordination, coordination_proposee, coordination_validee, planifiee, realisee, annulee
    /// </summary>
    [Column("statut")]
    [MaxLength(30)]
    public string Statut { get; set; } = "en_attente_coordination";

    /// <summary>
    /// ID de l'anesthésiste assigné
    /// </summary>
    [Column("id_anesthesiste")]
    public int? IdAnesthesiste { get; set; }

    /// <summary>
    /// Motif d'annulation si annulée
    /// </summary>
    [Column("motif_annulation")]
    public string? MotifAnnulation { get; set; }

    /// <summary>
    /// Notes complémentaires
    /// </summary>
    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(IdConsultation))]
    public virtual Consultation Consultation { get; set; } = null!;

    [ForeignKey(nameof(IdPatient))]
    public virtual Patient Patient { get; set; } = null!;

    [ForeignKey(nameof(IdChirurgien))]
    public virtual Medecin Chirurgien { get; set; } = null!;

    [ForeignKey(nameof(IdAnesthesiste))]
    public virtual Medecin? Anesthesiste { get; set; }

    /// <summary>
    /// Réservation de bloc opératoire associée
    /// </summary>
    public virtual ReservationBloc? ReservationBloc { get; set; }

    /// <summary>
    /// Coordination avec l'anesthésiste
    /// </summary>
    public virtual CoordinationIntervention? Coordination { get; set; }
}
