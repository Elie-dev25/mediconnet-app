using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité Paramètre - Stocke les paramètres vitaux d'une consultation
/// Enregistrés par l'infirmier(e) ou l'accueil avant la consultation médicale
/// </summary>
[Table("parametre")]
public class Parametre
{
    [Key]
    [Column("id_parametre")]
    public int IdParametre { get; set; }

    /// <summary>Référence vers la consultation associée (relation 1-1)</summary>
    [Column("id_consultation")]
    public int IdConsultation { get; set; }

    /// <summary>Poids du patient en kg</summary>
    [Column("poids")]
    public decimal? Poids { get; set; }

    /// <summary>Température du patient en °C</summary>
    [Column("temperature")]
    public decimal? Temperature { get; set; }

    /// <summary>Tension artérielle systolique (haute) en mmHg</summary>
    [Column("tension_systolique")]
    public int? TensionSystolique { get; set; }

    /// <summary>Tension artérielle diastolique (basse) en mmHg</summary>
    [Column("tension_diastolique")]
    public int? TensionDiastolique { get; set; }

    /// <summary>Taille du patient en cm</summary>
    [Column("taille")]
    public decimal? Taille { get; set; }

    /// <summary>Date et heure d'enregistrement des paramètres</summary>
    [Column("date_enregistrement")]
    public DateTime DateEnregistrement { get; set; } = DateTime.UtcNow;

    /// <summary>ID de l'utilisateur ayant enregistré les paramètres (infirmier/accueil)</summary>
    [Column("enregistre_par")]
    public int? EnregistrePar { get; set; }

    // Navigation properties
    [ForeignKey("IdConsultation")]
    public virtual Consultation? Consultation { get; set; }

    [ForeignKey("EnregistrePar")]
    public virtual Utilisateur? UtilisateurEnregistrant { get; set; }

    // Propriétés calculées
    [NotMapped]
    public string? TensionFormatee => 
        TensionSystolique.HasValue && TensionDiastolique.HasValue 
            ? $"{TensionSystolique}/{TensionDiastolique}" 
            : null;

    [NotMapped]
    public decimal? IMC => 
        Poids.HasValue && Taille.HasValue && Taille.Value > 0 
            ? Math.Round(Poids.Value / ((Taille.Value / 100) * (Taille.Value / 100)), 2) 
            : null;
}
