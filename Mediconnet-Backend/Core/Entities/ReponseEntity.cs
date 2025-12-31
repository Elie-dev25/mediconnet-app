using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

[Table("reponse")]
public class Reponse
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("consultation_id")]
    public int ConsultationId { get; set; }

    [Column("question_id")]
    public int QuestionId { get; set; }

    [Column("valeur_reponse")]
    public string? ValeurReponse { get; set; }

    [Column("rempli_par")]
    [Required]
    [MaxLength(20)]
    public string RempliPar { get; set; } = "patient";

    [Column("date_saisie")]
    public DateTime DateSaisie { get; set; } = DateTime.UtcNow;

    [ForeignKey("ConsultationId")]
    public virtual Consultation? Consultation { get; set; }

    [ForeignKey("QuestionId")]
    public virtual Question? Question { get; set; }
}
