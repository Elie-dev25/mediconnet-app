using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Mediconnet_Backend.Core.Entities;

[Table("question")]
public class Question
{
    [Key]
    [Column("id_question")]
    public int Id { get; set; }

    [Column("texte")]
    [Required]
    public string TexteQuestion { get; set; } = string.Empty;

    [Column("type")]
    [MaxLength(50)]
    public string? TypeQuestion { get; set; } = "text";

    [Column("categorie")]
    [MaxLength(100)]
    public string? Categorie { get; set; }

    [Column("ordre")]
    public int Ordre { get; set; } = 0;

    [Column("obligatoire")]
    public bool Obligatoire { get; set; } = false;

    [Column("actif")]
    public bool Actif { get; set; } = true;

    public virtual ICollection<ConsultationQuestion>? ConsultationQuestions { get; set; }
}
