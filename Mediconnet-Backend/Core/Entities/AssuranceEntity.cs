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

    // ==================== 2. INFORMATIONS ADMINISTRATIVES ====================
    
    [MaxLength(100)]
    public string? Groupe { get; set; } // Ex: "Groupe AXA", "SUNU Group"

    [MaxLength(100)]
    public string? PaysOrigine { get; set; }

    [MaxLength(50)]
    public string? StatutJuridique { get; set; } // compagnie, mutuelle, organisme_public, cooperative

    [MaxLength(1000)]
    public string? Description { get; set; }

    // ==================== 3. COUVERTURE SANTÉ ====================
    
    [MaxLength(500)]
    public string? TypeCouverture { get; set; } // accidents, maladies, hospitalisation, maternite, forfait_soins_base

    public bool IsComplementaire { get; set; } = false;

    [MaxLength(255)]
    public string? CategorieBeneficiaires { get; set; } // salaries, familles, diaspora, artisans, femmes_enceintes

    // ==================== 4. VALIDITÉ ET FONCTIONNEMENT ====================
    
    [MaxLength(1000)]
    public string? ConditionsAdhesion { get; set; }

    [MaxLength(100)]
    public string? ZoneCouverture { get; set; } // national, rural, diaspora, international

    [MaxLength(255)]
    public string? ModePaiement { get; set; } // mobile_money, entreprise, cotisations, prelevement

    public bool IsActive { get; set; } = true;

    // ==================== 5. CHAMPS TECHNIQUES ====================
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation - Une assurance peut couvrir plusieurs patients
    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
}
