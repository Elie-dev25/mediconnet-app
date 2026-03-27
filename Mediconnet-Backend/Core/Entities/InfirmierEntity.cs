using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entite Infirmier - Mappe a la table 'infirmier'
/// </summary>
[Table("infirmier")]
public class Infirmier
{
    [Key]
    [Column("id_user")]
    public int IdUser { get; set; }
    
    [Column("matricule")]
    [MaxLength(50)]
    public string? Matricule { get; set; }
    
    /// <summary>
    /// Statut de l'infirmier: actif, bloque, suspendu
    /// </summary>
    [Column("statut")]
    [MaxLength(20)]
    public string Statut { get; set; } = "actif";
    
    /// <summary>
    /// Service de rattachement de l'infirmier (obligatoire)
    /// </summary>
    [Column("id_service")]
    public int IdService { get; set; }
    
    /// <summary>
    /// Date de nomination comme Major (si applicable, via Service.IdMajor)
    /// </summary>
    [Column("date_nomination_major")]
    public DateTime? DateNominationMajor { get; set; }
    
    /// <summary>
    /// Accréditations/certifications de l'infirmier
    /// </summary>
    [Column("accreditations")]
    [MaxLength(500)]
    public string? Accreditations { get; set; }
    
    /// <summary>
    /// Spécialité de l'infirmier (IDE, IADE, IBODE, etc.)
    /// </summary>
    [Column("id_specialite")]
    public int? IdSpecialite { get; set; }
    
    // Relations
    [ForeignKey("IdUser")]
    public virtual Utilisateur Utilisateur { get; set; } = null!;
    
    /// <summary>
    /// Service de rattachement de l'infirmier
    /// </summary>
    [ForeignKey("IdService")]
    public virtual Service Service { get; set; } = null!;
    
    /// <summary>
    /// Spécialité de l'infirmier
    /// </summary>
    [ForeignKey("IdSpecialite")]
    public virtual SpecialiteInfirmier? Specialite { get; set; }
}
