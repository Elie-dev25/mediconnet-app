using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité pour les questions libres posées par le médecin pendant une consultation
/// Ces questions ne font pas partie du questionnaire prédéfini
/// </summary>
[Table("question_libre")]
public class QuestionLibre
{
    [Key]
    [Column("id_question_libre")]
    public int IdQuestionLibre { get; set; }

    /// <summary>ID de la consultation</summary>
    [Column("id_consultation")]
    public int IdConsultation { get; set; }

    /// <summary>Texte de la question posée</summary>
    [Column("texte_question")]
    public string TexteQuestion { get; set; } = string.Empty;

    /// <summary>Réponse du patient</summary>
    [Column("reponse")]
    public string? Reponse { get; set; }

    /// <summary>Ordre d'affichage</summary>
    [Column("ordre")]
    public int Ordre { get; set; } = 0;

    /// <summary>Date de création</summary>
    [Column("date_creation")]
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    /// <summary>ID du médecin ayant posé la question</summary>
    [Column("id_medecin")]
    public int IdMedecin { get; set; }

    // Navigation properties
    [ForeignKey("IdConsultation")]
    public virtual Consultation? Consultation { get; set; }

    [ForeignKey("IdMedecin")]
    public virtual Medecin? Medecin { get; set; }
}
