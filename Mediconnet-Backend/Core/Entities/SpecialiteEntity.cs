using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Represente une specialite medicale
/// </summary>
[Table("specialites")]
public class Specialite
{
    [Key]
    [Column("id_specialite")]
    public int IdSpecialite { get; set; }

    [Column("nom_specialite")]
    [Required]
    [MaxLength(100)]
    public string NomSpecialite { get; set; } = string.Empty;

    // Navigation
    public virtual ICollection<Medecin> Medecins { get; set; } = new List<Medecin>();
}
