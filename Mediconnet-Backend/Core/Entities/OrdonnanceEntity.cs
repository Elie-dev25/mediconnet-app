using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité Ordonnance - Mappe à la table 'ordonnance'
/// Supporte tous les contextes de prescription :
/// - Consultation classique
/// - Hospitalisation
/// - Prescription directe (hors consultation)
/// </summary>
[Table("ordonnance")]
public class Ordonnance
{
    [Key]
    [Column("id_ordonnance")]
    public int IdOrdonnance { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ID du patient (lien direct pour faciliter les requêtes)
    /// </summary>
    [Column("id_patient")]
    public int? IdPatient { get; set; }

    /// <summary>
    /// ID du médecin prescripteur (lien direct)
    /// </summary>
    [Column("id_medecin")]
    public int? IdMedecin { get; set; }

    /// <summary>
    /// ID de la consultation de rattachement (optionnel - peut être null si hospitalisation ou prescription directe)
    /// </summary>
    [Column("id_consultation")]
    public int? IdConsultation { get; set; }

    /// <summary>
    /// ID de l'hospitalisation (si prescription en contexte hospitalier)
    /// </summary>
    [Column("id_hospitalisation")]
    public int? IdHospitalisation { get; set; }

    /// <summary>
    /// Type de contexte : consultation, hospitalisation, directe
    /// </summary>
    [Column("type_contexte")]
    [StringLength(50)]
    public string? TypeContexte { get; set; }

    /// <summary>
    /// Statut de l'ordonnance : active, dispensee, partielle, annulee, expiree
    /// </summary>
    [Column("statut")]
    [StringLength(50)]
    public string Statut { get; set; } = "active";

    [Column("commentaire")]
    public string? Commentaire { get; set; }

    /// <summary>
    /// Date de création
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date de dernière mise à jour
    /// </summary>
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // ==================== Fonctionnalités avancées ====================

    /// <summary>
    /// Date d'expiration de l'ordonnance (par défaut 3 mois après création)
    /// </summary>
    [Column("date_expiration")]
    public DateTime? DateExpiration { get; set; }

    /// <summary>
    /// Indique si l'ordonnance est renouvelable
    /// </summary>
    [Column("renouvelable")]
    public bool Renouvelable { get; set; } = false;

    /// <summary>
    /// Nombre de renouvellements autorisés
    /// </summary>
    [Column("nombre_renouvellements")]
    public int? NombreRenouvellements { get; set; }

    /// <summary>
    /// Nombre de renouvellements restants
    /// </summary>
    [Column("renouvellements_restants")]
    public int? RenouvellementRestants { get; set; }

    /// <summary>
    /// ID de l'ordonnance originale (si c'est un renouvellement)
    /// </summary>
    [Column("id_ordonnance_originale")]
    public int? IdOrdonnanceOriginale { get; set; }

    // Navigation
    [ForeignKey("IdConsultation")]
    public virtual Consultation? Consultation { get; set; }

    [ForeignKey("IdPatient")]
    public virtual Patient? Patient { get; set; }

    [ForeignKey("IdMedecin")]
    public virtual Medecin? Medecin { get; set; }

    [ForeignKey("IdHospitalisation")]
    public virtual Hospitalisation? Hospitalisation { get; set; }

    /// <summary>
    /// Navigation vers l'ordonnance originale (pour les renouvellements)
    /// </summary>
    [ForeignKey("IdOrdonnanceOriginale")]
    public virtual Ordonnance? OrdonnanceOriginale { get; set; }

    /// <summary>
    /// Collection des renouvellements de cette ordonnance
    /// </summary>
    public virtual ICollection<Ordonnance>? Renouvellements { get; set; }

    public virtual ICollection<PrescriptionMedicament>? Medicaments { get; set; }
}

/// <summary>
/// Entité PrescriptionMedicament - Mappe à la table 'ordonnance_medicament'
/// Supporte les médicaments du catalogue ET les médicaments en saisie libre (hors catalogue)
/// </summary>
[Table("ordonnance_medicament")]
public class PrescriptionMedicament
{
    [Key]
    [Column("id_prescription_med")]
    public int IdPrescriptionMed { get; set; }

    [Column("id_ordonnance")]
    public int IdOrdonnance { get; set; }

    /// <summary>
    /// Référence au catalogue médicament (NULL si saisie libre)
    /// </summary>
    [Column("id_medicament")]
    public int? IdMedicament { get; set; }

    /// <summary>
    /// Nom du médicament en saisie libre (utilisé si IdMedicament est NULL)
    /// </summary>
    [Column("nom_medicament_libre")]
    public string? NomMedicamentLibre { get; set; }

    /// <summary>
    /// Dosage en saisie libre (utilisé si IdMedicament est NULL)
    /// </summary>
    [Column("dosage_libre")]
    public string? DosageLibre { get; set; }

    /// <summary>
    /// Indique si le médicament est hors catalogue (saisie libre)
    /// </summary>
    [Column("est_hors_catalogue")]
    public bool EstHorsCatalogue { get; set; } = false;

    [Column("quantite")]
    public int Quantite { get; set; } = 1;

    [Column("duree_traitement")]
    public string? DureeTraitement { get; set; }

    [Column("posologie")]
    public string? Posologie { get; set; }

    [Column("frequence")]
    public string? Frequence { get; set; }

    [Column("voie_administration")]
    public string? VoieAdministration { get; set; }

    [Column("forme_pharmaceutique")]
    public string? FormePharmaceutique { get; set; }

    [Column("instructions")]
    public string? Instructions { get; set; }

    // Navigation
    [ForeignKey("IdOrdonnance")]
    public virtual Ordonnance? Ordonnance { get; set; }

    [ForeignKey("IdMedicament")]
    public virtual Medicament? Medicament { get; set; }

    /// <summary>
    /// Retourne le nom du médicament (catalogue ou saisie libre)
    /// </summary>
    [NotMapped]
    public string NomMedicamentEffectif => EstHorsCatalogue 
        ? NomMedicamentLibre ?? "Médicament non spécifié"
        : Medicament?.Nom ?? NomMedicamentLibre ?? "Médicament inconnu";

    /// <summary>
    /// Retourne le dosage du médicament (catalogue ou saisie libre)
    /// </summary>
    [NotMapped]
    public string? DosageEffectif => EstHorsCatalogue 
        ? DosageLibre 
        : Medicament?.Dosage ?? DosageLibre;
}
