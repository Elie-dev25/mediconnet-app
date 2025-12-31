using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Represente un caissier
/// </summary>
[Table("caissier")]
public class Caissier
{
    [Key]
    [Column("id_user")]
    public int IdUser { get; set; }

    // Navigation
    [ForeignKey("IdUser")]
    public virtual Utilisateur Utilisateur { get; set; } = null!;
}
