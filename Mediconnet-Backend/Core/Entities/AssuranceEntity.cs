using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité représentant une compagnie d'assurance (catalogue)
/// </summary>
public class Assurance
{
    [Key]
    public int IdAssurance { get; set; }

    // ==================== 1. IDENTIFICATION ====================
    
    [Required]
    [MaxLength(150)]
    public string Nom { get; set; } = string.Empty;

    [MaxLength(50)]
    public string TypeAssurance { get; set; } = "privee"; // privee, publique, mutuelle, micro_assurance, programme_public

    [MaxLength(255)]
    public string? SiteWeb { get; set; }

    [MaxLength(30)]
    public string? TelephoneServiceClient { get; set; }

    /// <summary>Email pour l'envoi des factures à l'assurance</summary>
    [MaxLength(255)]
    [Column("email_facturation")]
    public string? EmailFacturation { get; set; }

    // ==================== 2. INFORMATIONS ADMINISTRATIVES ====================
    
    [MaxLength(100)]
    public string? Groupe { get; set; } // Ex: "Groupe AXA", "SUNU Group"

    [MaxLength(100)]
    public string? PaysOrigine { get; set; }

    [MaxLength(50)]
    public string? StatutJuridique { get; set; } // compagnie, mutuelle, organisme_public, cooperative

    [MaxLength(1000)]
    public string? Description { get; set; }

    // ==================== 3. COUVERTURE SANTÉ (Normalisé) ====================
    
    /// <summary>Assurance complémentaire santé</summary>
    public bool IsComplementaire { get; set; } = false;

    /// <summary>FK vers zone de couverture géographique</summary>
    [Column("id_zone_couverture")]
    public int? IdZoneCouverture { get; set; }

    // ==================== 4. VALIDITÉ ET FONCTIONNEMENT ====================
    
    [MaxLength(1000)]
    public string? ConditionsAdhesion { get; set; }

    public bool IsActive { get; set; } = true;

    // ==================== 5. CHAMPS TECHNIQUES ====================
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ==================== 6. CHAMPS LEGACY (à supprimer après migration complète) ====================
    
    [MaxLength(500)]
    [Obsolete("Utiliser TypesCouvertureSante (many-to-many) à la place")]
    public string? TypeCouverture { get; set; }

    [MaxLength(255)]
    [Obsolete("Utiliser CategoriesBeneficiaires (many-to-many) à la place")]
    public string? CategorieBeneficiaires { get; set; }

    [MaxLength(100)]
    [Obsolete("Utiliser IdZoneCouverture (FK) à la place")]
    public string? ZoneCouverture { get; set; }

    [MaxLength(255)]
    [Obsolete("Utiliser ModesPaiement (many-to-many) à la place")]
    public string? ModePaiement { get; set; }

    // ==================== NAVIGATION ====================
    
    /// <summary>Zone de couverture géographique</summary>
    public virtual ZoneCouverture? Zone { get; set; }

    /// <summary>Patients couverts par cette assurance</summary>
    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();

    /// <summary>Couvertures par type de prestation (taux, plafonds, franchises)</summary>
    public virtual ICollection<AssuranceCouverture> Couvertures { get; set; } = new List<AssuranceCouverture>();

    /// <summary>Types de couverture santé offerts (hospitalisation, maternité, etc.)</summary>
    public virtual ICollection<TypeCouvertureSante> TypesCouvertureSante { get; set; } = new List<TypeCouvertureSante>();

    /// <summary>Catégories de bénéficiaires éligibles</summary>
    public virtual ICollection<CategorieBeneficiaire> CategoriesBeneficiaires { get; set; } = new List<CategorieBeneficiaire>();

    /// <summary>Modes de paiement acceptés</summary>
    public virtual ICollection<ModePaiement> ModesPaiement { get; set; } = new List<ModePaiement>();
}
