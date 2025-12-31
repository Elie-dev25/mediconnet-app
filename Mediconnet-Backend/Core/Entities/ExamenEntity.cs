using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité BulletinExamen - Mappe à la table 'bulletin_examen'
/// Représente un examen prescrit lors d'une consultation
/// </summary>
[Table("bulletin_examen")]
public class BulletinExamen
{
    [Key]
    [Column("id_bull_exam")]
    public int IdBulletinExamen { get; set; }

    [Column("date_demande")]
    public DateTime DateDemande { get; set; } = DateTime.UtcNow;

    [Column("id_labo")]
    public int? IdLabo { get; set; }

    [Column("id_consultation")]
    public int? IdConsultation { get; set; }

    [Column("instructions")]
    public string? Instructions { get; set; }

    [Column("id_exam")]
    public int? IdExamen { get; set; }

    // Navigation
    [ForeignKey("IdConsultation")]
    public virtual Consultation? Consultation { get; set; }

    [ForeignKey("IdExamen")]
    public virtual ExamenCatalogue? Examen { get; set; }
}

/// <summary>
/// Entité ExamenCatalogue - Mappe à la table 'examens'
/// Catalogue des examens disponibles
/// </summary>
[Table("examens")]
public class ExamenCatalogue
{
    [Key]
    [Column("id_exam")]
    public int IdExamen { get; set; }

    [Column("nom_exam")]
    public string NomExamen { get; set; } = "";

    [Column("description")]
    public string? Description { get; set; }

    [Column("prix_unitaire")]
    public decimal PrixUnitaire { get; set; }

    [Column("duree_estimee_minutes")]
    public int? DureeEstimeeMinutes { get; set; }

    [Column("preparation_requise")]
    public string? PreparationRequise { get; set; }

    [Column("type_examen")]
    public string? TypeExamen { get; set; }

    [Column("categorie")]
    public string? Categorie { get; set; }

    [Column("actif")]
    public bool Actif { get; set; } = true;
}
