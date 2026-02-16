using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité Recommandation - Recommandation médicale liée à une consultation
/// Permet de recommander un hôpital ou un médecin (interne ou externe)
/// </summary>
[Table("recommandation")]
public class Recommandation
{
    [Key]
    [Column("id_recommandation")]
    public int IdRecommandation { get; set; }

    [Column("id_consultation")]
    [Required]
    public int IdConsultation { get; set; }

    [Column("id_patient")]
    [Required]
    public int IdPatient { get; set; }

    /// <summary>Médecin prescripteur de la recommandation</summary>
    [Column("id_medecin")]
    [Required]
    public int IdMedecin { get; set; }

    /// <summary>Type de recommandation: hopital, medecin</summary>
    [Column("type")]
    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = "medecin";

    /// <summary>Nom de l'hôpital recommandé (saisie libre)</summary>
    [Column("nom_hopital")]
    [MaxLength(255)]
    public string? NomHopital { get; set; }

    /// <summary>Nom du médecin recommandé (saisie libre pour médecin externe)</summary>
    [Column("nom_medecin_recommande")]
    [MaxLength(255)]
    public string? NomMedecinRecommande { get; set; }

    /// <summary>FK vers médecin interne recommandé (nullable)</summary>
    [Column("id_medecin_recommande")]
    public int? IdMedecinRecommande { get; set; }

    /// <summary>Spécialité du médecin recommandé</summary>
    [Column("specialite")]
    [MaxLength(100)]
    public string? Specialite { get; set; }

    /// <summary>Motif / commentaire (obligatoire)</summary>
    [Column("motif")]
    [Required]
    public string Motif { get; set; } = "";

    /// <summary>Recommandation prioritaire</summary>
    [Column("prioritaire")]
    public bool Prioritaire { get; set; } = false;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    // Navigation
    [ForeignKey("IdConsultation")]
    public virtual Consultation? Consultation { get; set; }

    [ForeignKey("IdPatient")]
    public virtual Patient? Patient { get; set; }

    [ForeignKey("IdMedecin")]
    public virtual Medecin? Medecin { get; set; }

    [ForeignKey("IdMedecinRecommande")]
    public virtual Medecin? MedecinRecommande { get; set; }
}
