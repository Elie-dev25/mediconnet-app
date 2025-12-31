using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité Chambre - Mappe à la table 'chambre'
/// </summary>
[Table("chambre")]
public class Chambre
{
    [Key]
    [Column("id_chambre")]
    public int IdChambre { get; set; }

    [Column("numero")]
    public string? Numero { get; set; }

    [Column("capacite")]
    public int? Capacite { get; set; }

    [Column("etat")]
    public string? Etat { get; set; }

    [Column("statut")]
    public string? Statut { get; set; }

    // Navigation
    public virtual ICollection<Lit>? Lits { get; set; }
}

/// <summary>
/// Entité Lit - Mappe à la table 'lit'
/// </summary>
[Table("lit")]
public class Lit
{
    [Key]
    [Column("id_lit")]
    public int IdLit { get; set; }

    [Column("numero")]
    public string? Numero { get; set; }

    [Column("statut")]
    public string? Statut { get; set; }

    [Column("id_chambre")]
    public int IdChambre { get; set; }

    // Navigation
    [ForeignKey("IdChambre")]
    public virtual Chambre? Chambre { get; set; }

    public virtual ICollection<Hospitalisation>? Hospitalisations { get; set; }
}

/// <summary>
/// Entité Hospitalisation - Mappe à la table 'hospitalisation'
/// </summary>
[Table("hospitalisation")]
public class Hospitalisation
{
    [Key]
    [Column("id_admission")]
    public int IdAdmission { get; set; }

    [Column("date_entree")]
    [Required]
    public DateTime DateEntree { get; set; }

    [Column("date_sortie")]
    public DateTime? DateSortie { get; set; }

    [Column("motif")]
    public string? Motif { get; set; }

    [Column("statut")]
    public string? Statut { get; set; }

    [Column("id_patient")]
    public int IdPatient { get; set; }

    [Column("id_lit")]
    public int IdLit { get; set; }

    // Navigation
    [ForeignKey("IdPatient")]
    public virtual Patient? Patient { get; set; }

    [ForeignKey("IdLit")]
    public virtual Lit? Lit { get; set; }
}
