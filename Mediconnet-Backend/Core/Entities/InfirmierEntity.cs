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
    
    // Relations
    [ForeignKey("IdUser")]
    public virtual Utilisateur Utilisateur { get; set; } = null!;
}
