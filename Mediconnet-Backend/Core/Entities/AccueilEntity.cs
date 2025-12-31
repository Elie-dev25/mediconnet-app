using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Represente un agent d'accueil
/// Herite de Utilisateur via relation 1-1
/// </summary>
[Table("accueil")]
public class Accueil
{
    [Key]
    [Column("id_user")]
    public int IdUser { get; set; }

    /// <summary>
    /// Poste ou bureau de l'agent d'accueil
    /// </summary>
    [Column("poste")]
    [MaxLength(100)]
    public string? Poste { get; set; }

    /// <summary>
    /// Date d'embauche de l'agent
    /// </summary>
    [Column("date_embauche")]
    public DateTime? DateEmbauche { get; set; }

    // Navigation
    [ForeignKey("IdUser")]
    public virtual Utilisateur Utilisateur { get; set; } = null!;
}
