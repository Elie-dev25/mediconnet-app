using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

[Table("consultation_question")]
public class ConsultationQuestion
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("id_consultation")]
    public int ConsultationId { get; set; }

    [Column("id_question")]
    public int QuestionId { get; set; }

    // Navigation vers les réponses
    public virtual ICollection<Reponse>? Reponses { get; set; }

    [ForeignKey("ConsultationId")]
    public virtual Consultation? Consultation { get; set; }

    [ForeignKey("QuestionId")]
    public virtual Question? Question { get; set; }
}
