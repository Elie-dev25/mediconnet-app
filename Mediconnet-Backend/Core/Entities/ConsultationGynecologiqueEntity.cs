using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité représentant l'examen gynécologique d'une consultation spécialisée (extension 1-1)
/// </summary>
[Table("consultation_gyneco")]
public class ConsultationGynecologique
{
    [Key]
    [Column("id_consultation")]
    public int IdConsultation { get; set; }

    [Column("inspection_externe")]
    public string? InspectionExterne { get; set; }

    [Column("examen_speculum")]
    public string? ExamenSpeculum { get; set; }

    [Column("toucher_vaginal")]
    public string? ToucherVaginal { get; set; }

    [Column("autres_observations")]
    public string? AutresObservations { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(IdConsultation))]
    public virtual Consultation Consultation { get; set; } = null!;
}
