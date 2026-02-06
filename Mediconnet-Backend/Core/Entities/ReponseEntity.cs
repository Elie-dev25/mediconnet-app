using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

[Table("reponse")]
public class Reponse
{
    [Key]
    [Column("id_reponse")]
    public int Id { get; set; }

    [Column("id_consultation_question")]
    public int ConsultationQuestionId { get; set; }

    [Column("valeur")]
    public string? ValeurReponse { get; set; }

    [Column("date_reponse")]
    public DateTime DateReponse { get; set; } = DateTime.UtcNow;

    [ForeignKey("ConsultationQuestionId")]
    public virtual ConsultationQuestion? ConsultationQuestion { get; set; }
}
