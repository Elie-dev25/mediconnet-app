using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité représentant l'historique des affectations de service pour les médecins et infirmiers
/// </summary>
[Table("affectation_service")]
public class AffectationService
{
    [Key]
    [Column("id_affectation")]
    public int IdAffectation { get; set; }

    /// <summary>
    /// ID de l'utilisateur (médecin ou infirmier)
    /// </summary>
    [Column("id_user")]
    public int IdUser { get; set; }

    /// <summary>
    /// Type d'utilisateur: "medecin" ou "infirmier"
    /// </summary>
    [Required]
    [Column("type_user")]
    [MaxLength(20)]
    public string TypeUser { get; set; } = string.Empty;

    /// <summary>
    /// ID du service affecté
    /// </summary>
    [Column("id_service")]
    public int IdService { get; set; }

    /// <summary>
    /// Date de début de l'affectation
    /// </summary>
    [Column("date_debut")]
    public DateTime DateDebut { get; set; }

    /// <summary>
    /// Date de fin de l'affectation (null si affectation en cours)
    /// </summary>
    [Column("date_fin")]
    public DateTime? DateFin { get; set; }

    /// <summary>
    /// Motif du changement de service (optionnel)
    /// </summary>
    [Column("motif_changement")]
    [MaxLength(500)]
    public string? MotifChangement { get; set; }

    /// <summary>
    /// ID de l'administrateur ayant effectué le changement
    /// </summary>
    [Column("id_admin_changement")]
    public int? IdAdminChangement { get; set; }

    /// <summary>
    /// Date de création de l'enregistrement
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("IdUser")]
    public virtual Utilisateur? Utilisateur { get; set; }

    [ForeignKey("IdService")]
    public virtual Service? Service { get; set; }

    [ForeignKey("IdAdminChangement")]
    public virtual Utilisateur? AdminChangement { get; set; }
}

/// <summary>
/// Types d'utilisateurs pour les affectations de service
/// </summary>
public static class TypeUserAffectation
{
    public const string Medecin = "medecin";
    public const string Infirmier = "infirmier";
}
