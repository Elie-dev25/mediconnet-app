using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entite Medecin - Mappe a la table 'medecin'
/// </summary>
[Table("medecin")]
public class Medecin
{
    [Key]
    [Column("id_user")]
    public int IdUser { get; set; }
    
    [Column("numero_ordre")]
    [MaxLength(50)]
    public string? NumeroOrdre { get; set; }
    
    [Column("id_service")]
    public int IdService { get; set; }
    
    [Column("id_specialite")]
    public int? IdSpecialite { get; set; }
    
    // Relations
    [ForeignKey("IdUser")]
    public virtual Utilisateur Utilisateur { get; set; } = null!;
    
    [ForeignKey("IdService")]
    public virtual Service Service { get; set; } = null!;
    
    [ForeignKey("IdSpecialite")]
    public virtual Specialite? Specialite { get; set; }
}
