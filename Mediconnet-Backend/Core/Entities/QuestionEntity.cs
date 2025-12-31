using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Mediconnet_Backend.Core.Entities;

[Table("question")]
public class Question
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("texte_question")]
    [Required]
    public string TexteQuestion { get; set; } = string.Empty;

    [Column("type_question")]
    [Required]
    [MaxLength(50)]
    public string TypeQuestion { get; set; } = "texte";

    [Column("est_predefinie")]
    public bool EstPredefinie { get; set; }

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("CreatedBy")]
    public virtual Utilisateur? Createur { get; set; }

    public virtual ICollection<ConsultationQuestion>? ConsultationQuestions { get; set; }

    public virtual ICollection<Reponse>? Reponses { get; set; }
}
