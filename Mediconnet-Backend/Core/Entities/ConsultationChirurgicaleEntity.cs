using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité représentant l'examen chirurgical d'une consultation spécialisée (extension 1-1)
/// </summary>
[Table("consultation_chirurgicale")]
public class ConsultationChirurgicale
{
    [Key]
    [Column("id_consultation")]
    public int IdConsultation { get; set; }

    /// <summary>Zone anatomique examinée (ex: abdomen, membre inférieur droit, etc.)</summary>
    [Column("zone_examinee")]
    public string? ZoneExaminee { get; set; }

    /// <summary>Inspection locale de la zone (aspect cutané, tuméfaction, déformation, etc.)</summary>
    [Column("inspection_locale")]
    public string? InspectionLocale { get; set; }

    /// <summary>Palpation locale (douleur, masse, défense, etc.)</summary>
    [Column("palpation_locale")]
    public string? PalpationLocale { get; set; }

    /// <summary>Signes inflammatoires locaux (rougeur, chaleur, œdème, etc.)</summary>
    [Column("signes_inflammatoires")]
    public string? SignesInflammatoires { get; set; }

    /// <summary>État des cicatrices existantes (si applicable)</summary>
    [Column("cicatrices_existantes")]
    public string? CicatricesExistantes { get; set; }

    /// <summary>Mobilité et fonction de la zone (amplitude, force, etc.)</summary>
    [Column("mobilite_fonction")]
    public string? MobiliteFonction { get; set; }

    /// <summary>Conclusion de l'examen chirurgical</summary>
    [Column("conclusion_chirurgicale")]
    public string? ConclusionChirurgicale { get; set; }

    /// <summary>Décision : surveillance, traitement_medical, indication_operatoire</summary>
    [Column("decision")]
    public string? Decision { get; set; }

    /// <summary>Notes complémentaires</summary>
    [Column("notes_complementaires")]
    public string? NotesComplementaires { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(IdConsultation))]
    public virtual Consultation Consultation { get; set; } = null!;
}
