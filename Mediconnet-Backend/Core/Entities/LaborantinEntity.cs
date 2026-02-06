using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité Laborantin - Mappe à la table 'laborantin'
/// Remplace l'ancienne entité Biologiste
/// </summary>
[Table("laborantin")]
public class Laborantin
{
    [Key]
    [Column("id_user")]
    public int IdUser { get; set; }
    
    /// <summary>Matricule professionnel du laborantin</summary>
    [Column("matricule")]
    [MaxLength(50)]
    public string? Matricule { get; set; }
    
    /// <summary>Spécialisation en laboratoire (microbiologie, biochimie, hématologie, etc.)</summary>
    [Column("specialisation")]
    [MaxLength(100)]
    public string? Specialisation { get; set; }
    
    /// <summary>Laboratoire d'affectation (obligatoire)</summary>
    [Column("id_labo")]
    public int IdLabo { get; set; }
    
    /// <summary>Date d'embauche</summary>
    [Column("date_embauche")]
    public DateTime? DateEmbauche { get; set; }
    
    /// <summary>Statut actif/inactif</summary>
    [Column("actif")]
    public bool Actif { get; set; } = true;
    
    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
    
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    [ForeignKey("IdUser")]
    public virtual Utilisateur Utilisateur { get; set; } = null!;
    
    [ForeignKey("IdLabo")]
    public virtual Laboratoire? Laboratoire { get; set; }
}
