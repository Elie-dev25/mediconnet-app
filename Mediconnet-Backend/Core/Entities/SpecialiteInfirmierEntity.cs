using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Représente une spécialité infirmier (distincte des spécialités médecins)
/// </summary>
[Table("specialite_infirmier")]
public class SpecialiteInfirmier
{
    [Key]
    [Column("id_specialite")]
    public int IdSpecialite { get; set; }

    /// <summary>
    /// Code de la spécialité (ex: IDE, IADE, IBODE)
    /// </summary>
    [Column("code")]
    [MaxLength(20)]
    public string? Code { get; set; }

    /// <summary>
    /// Nom complet de la spécialité
    /// </summary>
    [Column("nom")]
    [Required]
    [MaxLength(100)]
    public string Nom { get; set; } = string.Empty;

    /// <summary>
    /// Description de la spécialité
    /// </summary>
    [Column("description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    [Column("actif")]
    public bool Actif { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual ICollection<Infirmier> Infirmiers { get; set; } = new List<Infirmier>();
}
