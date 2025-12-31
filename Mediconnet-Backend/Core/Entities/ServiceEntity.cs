using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Represente un service hospitalier
/// </summary>
[Table("service")]
public class Service
{
    [Key]
    [Column("id_service")]
    public int IdService { get; set; }

    [Column("nom_service")]
    [Required]
    [MaxLength(150)]
    public string NomService { get; set; } = string.Empty;

    [Column("responsable_service")]
    public int? ResponsableService { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    // Navigation
    [ForeignKey("ResponsableService")]
    public virtual Utilisateur? Responsable { get; set; }
    
    public virtual ICollection<Medecin> Medecins { get; set; } = new List<Medecin>();
}
