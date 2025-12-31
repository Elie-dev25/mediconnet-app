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

    public virtual ICollection<Reponse>? Reponses { get; set; }

    public virtual Ordonnance? Ordonnance { get; set; }

    public virtual ICollection<BulletinExamen>? BulletinsExamen { get; set; }
}
