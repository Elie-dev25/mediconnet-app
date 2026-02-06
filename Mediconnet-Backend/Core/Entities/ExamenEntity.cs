using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité CategorieExamen - Mappe à la table 'categories_examens'
/// Niveau 1 de la hiérarchie: Catégorie (ex: Biologie Médicale, Imagerie Médicale)
/// </summary>
[Table("categories_examens")]
public class CategorieExamen
{
    [Key]
    [Column("id_categorie")]
    public int IdCategorie { get; set; }

    [Column("nom")]
    public string Nom { get; set; } = "";

    [Column("code")]
    public string Code { get; set; } = "";

    [Column("description")]
    public string? Description { get; set; }

    [Column("icone")]
    public string? Icone { get; set; }

    [Column("ordre_affichage")]
    public int OrdreAffichage { get; set; } = 0;

    [Column("actif")]
    public bool Actif { get; set; } = true;

    // Navigation
    public virtual ICollection<SpecialiteExamen>? Specialites { get; set; }
}

/// <summary>
/// Entité SpecialiteExamen - Mappe à la table 'specialites_examens'
/// Niveau 2 de la hiérarchie: Spécialité (ex: Hématologie, Biochimie, Radiologie)
/// </summary>
[Table("specialites_examens")]
public class SpecialiteExamen
{
    [Key]
    [Column("id_specialite")]
    public int IdSpecialite { get; set; }

    [Column("id_categorie")]
    public int IdCategorie { get; set; }

    [Column("nom")]
    public string Nom { get; set; } = "";

    [Column("code")]
    public string Code { get; set; } = "";

    [Column("description")]
    public string? Description { get; set; }

    [Column("icone")]
    public string? Icone { get; set; }

    [Column("ordre_affichage")]
    public int OrdreAffichage { get; set; } = 0;

    [Column("actif")]
    public bool Actif { get; set; } = true;

    // Navigation
    [ForeignKey("IdCategorie")]
    public virtual CategorieExamen? Categorie { get; set; }

    public virtual ICollection<ExamenCatalogue>? Examens { get; set; }
}

/// <summary>
/// Entité BulletinExamen - Mappe à la table 'bulletin_examen'
/// Représente un examen prescrit lors d'une consultation ou hospitalisation
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

    /// <summary>
    /// Hospitalisation ayant généré cet examen (si applicable)
    /// </summary>
    [Column("id_hospitalisation")]
    public int? IdHospitalisation { get; set; }

    [Column("instructions")]
    public string? Instructions { get; set; }

    [Column("id_exam")]
    public int? IdExamen { get; set; }

    /// <summary>
    /// Examen urgent
    /// </summary>
    [Column("urgence")]
    public bool Urgence { get; set; } = false;

    /// <summary>
    /// Statut de l'examen: prescrit, en_cours, termine, annule
    /// </summary>
    [Column("statut")]
    public string? Statut { get; set; } = "prescrit";

    /// <summary>
    /// Date de réalisation de l'examen
    /// </summary>
    [Column("date_realisation")]
    public DateTime? DateRealisation { get; set; }

    /// <summary>
    /// Résultat textuel de l'examen
    /// </summary>
    [Column("resultat_texte")]
    public string? ResultatTexte { get; set; }

    /// <summary>
    /// Chemin vers le fichier résultat
    /// </summary>
    [Column("resultat_fichier")]
    public string? ResultatFichier { get; set; }

    /// <summary>
    /// Laborantin ayant validé le résultat (colonne legacy: id_biologiste)
    /// </summary>
    [Column("id_biologiste")]
    public int? IdBiologiste { get; set; }

    /// <summary>
    /// Date de saisie du résultat
    /// </summary>
    [Column("date_resultat")]
    public DateTime? DateResultat { get; set; }

    /// <summary>
    /// Commentaire du laboratoire
    /// </summary>
    [Column("commentaire_labo")]
    public string? CommentaireLabo { get; set; }

    // Navigation
    [ForeignKey("IdConsultation")]
    public virtual Consultation? Consultation { get; set; }

    [ForeignKey("IdHospitalisation")]
    public virtual Hospitalisation? Hospitalisation { get; set; }

    [ForeignKey("IdExamen")]
    public virtual ExamenCatalogue? Examen { get; set; }

    [ForeignKey("IdLabo")]
    public virtual Laboratoire? Laboratoire { get; set; }
}

/// <summary>
/// Entité Laboratoire - Mappe à la table 'laboratoire'
/// Représente un laboratoire d'analyses ou centre d'imagerie
/// </summary>
[Table("laboratoire")]
public class Laboratoire
{
    [Key]
    [Column("id_labo")]
    public int IdLabo { get; set; }

    [Column("nom_labo")]
    public string NomLabo { get; set; } = "";

    [Column("contact")]
    public string? Contact { get; set; }

    [Column("adresse")]
    public string? Adresse { get; set; }

    [Column("telephone")]
    public string? Telephone { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [Column("type")]
    public string? Type { get; set; } // "interne" ou "externe"

    [Column("actif")]
    public bool Actif { get; set; } = true;
}

/// <summary>
/// Entité ExamenCatalogue - Mappe à la table 'examens'
/// Niveau 3 de la hiérarchie: Examen (ex: NFS, Glycémie, Radiographie thoracique)
/// </summary>
[Table("examens")]
public class ExamenCatalogue
{
    [Key]
    [Column("id_exam")]
    public int IdExamen { get; set; }

    [Column("id_specialite")]
    public int IdSpecialite { get; set; }

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

    /// <summary>
    /// Indique si l'examen est réalisable dans l'hôpital
    /// </summary>
    [Column("disponible")]
    public bool Disponible { get; set; } = true;

    [Column("actif")]
    public bool Actif { get; set; } = true;

    // Navigation
    [ForeignKey("IdSpecialite")]
    public virtual SpecialiteExamen? Specialite { get; set; }
}
