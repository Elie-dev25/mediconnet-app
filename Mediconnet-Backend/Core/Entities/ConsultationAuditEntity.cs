using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité d'audit pour tracer toutes les modifications apportées aux consultations
/// </summary>
[Table("consultation_audit")]
public class ConsultationAudit
{
    [Key]
    [Column("id_audit")]
    public int IdAudit { get; set; }

    /// <summary>ID de la consultation concernée</summary>
    [Column("id_consultation")]
    public int IdConsultation { get; set; }

    /// <summary>ID de l'utilisateur ayant effectué la modification</summary>
    [Column("id_utilisateur")]
    public int IdUtilisateur { get; set; }

    /// <summary>Type d'action: creation, modification, statut_change, annulation, validation</summary>
    [Column("type_action")]
    public string TypeAction { get; set; } = string.Empty;

    /// <summary>Champ modifié (null pour les actions globales)</summary>
    [Column("champ_modifie")]
    public string? ChampModifie { get; set; }

    /// <summary>Ancienne valeur (sérialisée en JSON si complexe)</summary>
    [Column("ancienne_valeur")]
    public string? AncienneValeur { get; set; }

    /// <summary>Nouvelle valeur (sérialisée en JSON si complexe)</summary>
    [Column("nouvelle_valeur")]
    public string? NouvelleValeur { get; set; }

    /// <summary>Description lisible de la modification</summary>
    [Column("description")]
    public string? Description { get; set; }

    /// <summary>Adresse IP de l'utilisateur</summary>
    [Column("adresse_ip")]
    public string? AdresseIp { get; set; }

    /// <summary>User-Agent du navigateur</summary>
    [Column("user_agent")]
    public string? UserAgent { get; set; }

    /// <summary>Date et heure de la modification</summary>
    [Column("date_modification")]
    public DateTime DateModification { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("IdConsultation")]
    public virtual Consultation? Consultation { get; set; }

    [ForeignKey("IdUtilisateur")]
    public virtual Utilisateur? Utilisateur { get; set; }
}

/// <summary>
/// Types d'actions d'audit pour les consultations
/// </summary>
public static class ConsultationAuditActions
{
    public const string Creation = "creation";
    public const string Modification = "modification";
    public const string StatutChange = "statut_change";
    public const string Annulation = "annulation";
    public const string Validation = "validation";
    public const string Pause = "pause";
    public const string Reprise = "reprise";
    public const string AjoutOrdonnance = "ajout_ordonnance";
    public const string AjoutExamen = "ajout_examen";
    public const string AjoutOrientation = "ajout_orientation";
}
