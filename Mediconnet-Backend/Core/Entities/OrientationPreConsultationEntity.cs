using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité OrientationPreConsultation - Gestion unifiée des orientations patient
/// Remplace les anciennes tables orientation_specialiste et recommandation
/// Types: medecin_interne, medecin_externe, hopital, service_interne, laboratoire
/// </summary>
[Table("orientation_pre_consultation")]
public class OrientationPreConsultation
{
    [Key]
    [Column("id_orientation")]
    public int IdOrientation { get; set; }

    // ==================== RÉFÉRENCES OBLIGATOIRES ====================

    [Column("id_consultation")]
    [Required]
    public int IdConsultation { get; set; }

    [Column("id_patient")]
    [Required]
    public int IdPatient { get; set; }

    [Column("id_medecin_prescripteur")]
    [Required]
    public int IdMedecinPrescripteur { get; set; }

    // ==================== TYPE D'ORIENTATION ====================

    /// <summary>
    /// Type d'orientation: medecin_interne, medecin_externe, hopital, service_interne, laboratoire
    /// </summary>
    [Column("type_orientation")]
    [Required]
    [MaxLength(30)]
    public string TypeOrientation { get; set; } = "medecin_interne";

    // ==================== DESTINATION (selon le type) ====================

    /// <summary>FK vers spécialité (pour médecin interne)</summary>
    [Column("id_specialite")]
    public int? IdSpecialite { get; set; }

    /// <summary>FK vers médecin interne orienté</summary>
    [Column("id_medecin_oriente")]
    public int? IdMedecinOriente { get; set; }

    /// <summary>Nom du destinataire (médecin externe ou hôpital)</summary>
    [Column("nom_destinataire")]
    [MaxLength(255)]
    public string? NomDestinataire { get; set; }

    /// <summary>Spécialité en texte libre</summary>
    [Column("specialite_texte")]
    [MaxLength(100)]
    public string? SpecialiteTexte { get; set; }

    /// <summary>Adresse du destinataire externe</summary>
    [Column("adresse_destinataire")]
    public string? AdresseDestinataire { get; set; }

    /// <summary>Téléphone du destinataire externe</summary>
    [Column("telephone_destinataire")]
    [MaxLength(20)]
    public string? TelephoneDestinataire { get; set; }

    // ==================== DÉTAILS DE L'ORIENTATION ====================

    /// <summary>Motif de l'orientation (obligatoire)</summary>
    [Column("motif")]
    [Required]
    public string Motif { get; set; } = "";

    /// <summary>Notes complémentaires</summary>
    [Column("notes")]
    public string? Notes { get; set; }

    // ==================== PRIORITÉ ET URGENCE ====================

    /// <summary>Orientation urgente</summary>
    [Column("urgence")]
    public bool Urgence { get; set; } = false;

    /// <summary>Orientation prioritaire</summary>
    [Column("prioritaire")]
    public bool Prioritaire { get; set; } = false;

    // ==================== SUIVI DE L'ORIENTATION ====================

    /// <summary>Statut: en_attente, acceptee, refusee, rdv_pris, terminee, annulee</summary>
    [Column("statut")]
    [MaxLength(30)]
    public string Statut { get; set; } = "en_attente";

    [Column("date_orientation")]
    public DateTime DateOrientation { get; set; } = DateTime.UtcNow;

    [Column("date_rdv_propose")]
    public DateTime? DateRdvPropose { get; set; }

    /// <summary>FK vers le RDV créé suite à l'orientation</summary>
    [Column("id_rdv_cree")]
    public int? IdRdvCree { get; set; }

    // ==================== MÉTADONNÉES ====================

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    // ==================== NAVIGATION PROPERTIES ====================

    [ForeignKey("IdConsultation")]
    public virtual Consultation? Consultation { get; set; }

    [ForeignKey("IdPatient")]
    public virtual Patient? Patient { get; set; }

    [ForeignKey("IdMedecinPrescripteur")]
    public virtual Medecin? MedecinPrescripteur { get; set; }

    [ForeignKey("IdSpecialite")]
    public virtual Specialite? Specialite { get; set; }

    [ForeignKey("IdMedecinOriente")]
    public virtual Medecin? MedecinOriente { get; set; }

    [ForeignKey("IdRdvCree")]
    public virtual RendezVous? RendezVousCree { get; set; }
}

/// <summary>
/// Types d'orientation disponibles
/// </summary>
public static class TypesOrientation
{
    public const string MedecinInterne = "medecin_interne";
    public const string MedecinExterne = "medecin_externe";
    public const string Hopital = "hopital";
    public const string ServiceInterne = "service_interne";
    public const string Laboratoire = "laboratoire";
}

/// <summary>
/// Statuts d'orientation
/// </summary>
public static class StatutsOrientation
{
    public const string EnAttente = "en_attente";
    public const string Acceptee = "acceptee";
    public const string Refusee = "refusee";
    public const string RdvPris = "rdv_pris";
    public const string Terminee = "terminee";
    public const string Annulee = "annulee";
}
