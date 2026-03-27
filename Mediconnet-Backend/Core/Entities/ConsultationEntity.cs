using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité Consultation - Représente une consultation médicale
/// Les paramètres vitaux sont stockés dans l'entité Parametre associée
/// </summary>
[Table("consultation")]
public class Consultation
{
    [Key]
    [Column("id_consultation")]
    public int IdConsultation { get; set; }

    /// <summary>Date et heure de la consultation</summary>
    [Column("date_heure")]
    public DateTime DateHeure { get; set; }

    /// <summary>Date de dernière modification</summary>
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Date et heure de début effectif de la consultation</summary>
    [Column("date_debut_effective")]
    public DateTime? DateDebutEffective { get; set; }

    /// <summary>Date et heure de fin de la consultation</summary>
    [Column("date_fin")]
    public DateTime? DateFin { get; set; }

    /// <summary>Durée réelle de la consultation en minutes</summary>
    [Column("duree_minutes")]
    public int? DureeMinutes { get; set; }

    /// <summary>Motif d'annulation si la consultation est annulée</summary>
    [Column("motif_annulation")]
    public string? MotifAnnulation { get; set; }

    /// <summary>Date d'annulation</summary>
    [Column("date_annulation")]
    public DateTime? DateAnnulation { get; set; }

    /// <summary>Motif de la consultation</summary>
    [Column("motif")]
    public string? Motif { get; set; }

    /// <summary>Diagnostic établi par le médecin</summary>
    [Column("diagnostic")]
    public string? Diagnostic { get; set; }

    /// <summary>Statut de la consultation (planifie, en_cours, termine, annule)</summary>
    [Column("statut")]
    public string? Statut { get; set; }

    /// <summary>ID du médecin effectuant la consultation</summary>
    [Column("id_medecin")]
    public int IdMedecin { get; set; }

    /// <summary>ID du patient consulté</summary>
    [Column("id_patient")]
    public int IdPatient { get; set; }

    [Column("id_rdv")]
    public int? IdRendezVous { get; set; }

    /// <summary>Type de consultation (normale, urgence, suivi, etc.)</summary>
    [Column("type_consultation")]
    public string? TypeConsultation { get; set; }

    /// <summary>Antécédents médicaux relevés</summary>
    [Column("antecedents")]
    public string? Antecedents { get; set; }

    /// <summary>Chemin du questionnaire rempli</summary>
    [Column("chemin_questionnaire")]
    public string? CheminQuestionnaire { get; set; }

    /// <summary>Anamnèse - historique et symptômes</summary>
    [Column("anamnese")]
    public string? Anamnese { get; set; }

    /// <summary>Notes cliniques</summary>
    [Column("notes_cliniques")]
    public string? NotesCliniques { get; set; }

    /// <summary>Conclusion de la consultation</summary>
    [Column("conclusion")]
    public string? Conclusion { get; set; }

    /// <summary>Recommandations du médecin</summary>
    [Column("recommandations")]
    public string? Recommandations { get; set; }

    // ==================== EXAMEN CLINIQUE (Étape 2) ====================
    
    /// <summary>Observations visuelles: aspect général, peau, muqueuses</summary>
    [Column("examen_inspection")]
    public string? ExamenInspection { get; set; }
    
    /// <summary>Résultats de la palpation: abdomen, ganglions, etc.</summary>
    [Column("examen_palpation")]
    public string? ExamenPalpation { get; set; }
    
    /// <summary>Auscultation: coeur, poumons, abdomen</summary>
    [Column("examen_auscultation")]
    public string? ExamenAuscultation { get; set; }
    
    /// <summary>Percussion: thorax, abdomen</summary>
    [Column("examen_percussion")]
    public string? ExamenPercussion { get; set; }
    
    /// <summary>Autres observations cliniques</summary>
    [Column("examen_autres")]
    public string? ExamenAutres { get; set; }

    // ==================== DIAGNOSTIC ET ORIENTATION (Étape 3) ====================
    
    /// <summary>Diagnostics différentiels ou associés</summary>
    [Column("diagnostics_secondaires")]
    public string? DiagnosticsSecondaires { get; set; }
    
    /// <summary>Hypothèses à confirmer par examens</summary>
    [Column("hypotheses_diagnostiques")]
    public string? HypothesesDiagnostiques { get; set; }

    // ==================== PLAN DE TRAITEMENT (Étape 4) ====================
    
    /// <summary>Explication du diagnostic au patient</summary>
    [Column("explication_diagnostic")]
    public string? ExplicationDiagnostic { get; set; }
    
    /// <summary>Options de traitement proposées</summary>
    [Column("options_traitement")]
    public string? OptionsTraitement { get; set; }
    
    /// <summary>Spécialiste vers lequel orienter le patient</summary>
    [Column("orientation_specialiste")]
    public string? OrientationSpecialiste { get; set; }
    
    /// <summary>Motif de l'orientation vers un spécialiste</summary>
    [Column("motif_orientation")]
    public string? MotifOrientation { get; set; }

    // ==================== CONCLUSION (Étape 5) ====================
    
    /// <summary>Résumé des points importants</summary>
    [Column("resume_consultation")]
    public string? ResumeConsultation { get; set; }
    
    /// <summary>Questions du patient et réponses</summary>
    [Column("questions_patient")]
    public string? QuestionsPatient { get; set; }
    
    /// <summary>Consignes données au patient</summary>
    [Column("consignes_patient")]
    public string? ConsignesPatient { get; set; }

    // ==================== PROGRESSION CONSULTATION ====================
    
    /// <summary>Étape actuelle de la consultation (pour reprise après pause)</summary>
    [Column("etape_actuelle")]
    public string? EtapeActuelle { get; set; }

    // Navigation properties
    [ForeignKey("IdMedecin")]
    public virtual Medecin? Medecin { get; set; }

    [ForeignKey("IdPatient")]
    public virtual Patient? Patient { get; set; }

    [ForeignKey("IdRendezVous")]
    public virtual RendezVous? RendezVous { get; set; }

    /// <summary>Paramètres vitaux associés (relation 1-1)</summary>
    public virtual Parametre? Parametre { get; set; }

    public virtual ICollection<ConsultationQuestion>? ConsultationQuestions { get; set; }

    public virtual ICollection<QuestionLibre>? QuestionsLibres { get; set; }

    public virtual Ordonnance? Ordonnance { get; set; }

    public virtual ConsultationGynecologique? ConsultationGynecologique { get; set; }

    public virtual ConsultationChirurgicale? ConsultationChirurgicale { get; set; }

    public virtual ICollection<BulletinExamen>? BulletinsExamen { get; set; }

    /// <summary>Orientations pré-consultation (unifiées)</summary>
    public virtual ICollection<OrientationPreConsultation>? OrientationsPreConsultation { get; set; }
}
