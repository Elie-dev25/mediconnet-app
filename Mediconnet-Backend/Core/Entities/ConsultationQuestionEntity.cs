using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

[Table("consultation_question")]
public class ConsultationQuestion
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("consultation_id")]
    public int ConsultationId { get; set; }

    [Column("question_id")]
    public int QuestionId { get; set; }

    [Column("ordre_affichage")]
    public int OrdreAffichage { get; set; }

    [ForeignKey("ConsultationId")]
    public virtual Consultation? Consultation { get; set; }

    [ForeignKey("QuestionId")]
    public virtual Question? Question { get; set; }
}
