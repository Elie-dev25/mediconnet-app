using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité StandardChambre - Mappe à la table 'standard_chambre'
/// Définit les standards de chambre avec prix, privilèges et localisation
/// </summary>
[Table("standard_chambre")]
public class StandardChambre
{
    [Key]
    [Column("id_standard")]
    public int IdStandard { get; set; }

    [Column("nom")]
    [Required]
    [MaxLength(100)]
    public string Nom { get; set; } = "";

    [Column("description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    [Column("prix_journalier")]
    [Required]
    public decimal PrixJournalier { get; set; }

    /// <summary>
    /// Privilèges stockés en JSON (ex: ["wifi", "climatisation", "2 repas/jour"])
    /// </summary>
    [Column("privileges")]
    public string? Privileges { get; set; }

    [Column("localisation")]
    [MaxLength(200)]
    public string? Localisation { get; set; }

    [Column("actif")]
    public bool Actif { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual ICollection<Chambre>? Chambres { get; set; }
}

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

    [Column("id_standard")]
    public int? IdStandard { get; set; }

    // Navigation
    [ForeignKey("IdStandard")]
    public virtual StandardChambre? Standard { get; set; }

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
/// Statuts: en_attente_lit, en_cours, termine, annule
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

    /// <summary>
    /// Statut: en_attente_lit (ordonné par médecin, en attente d'attribution lit par Major),
    /// en_cours (lit attribué, patient hospitalisé), termine, annule
    /// </summary>
    [Column("statut")]
    public string? Statut { get; set; }

    [Column("id_patient")]
    public int IdPatient { get; set; }

    /// <summary>
    /// Nullable: null quand en_attente_lit, rempli par le Major lors de l'attribution
    /// </summary>
    [Column("id_lit")]
    public int? IdLit { get; set; }

    [Column("id_medecin")]
    public int? IdMedecin { get; set; }

    /// <summary>
    /// Niveau d'urgence: normale, urgente, critique
    /// </summary>
    [Column("urgence")]
    [MaxLength(20)]
    public string? Urgence { get; set; }

    /// <summary>
    /// Diagnostic principal justifiant l'hospitalisation
    /// </summary>
    [Column("diagnostic_principal")]
    public string? DiagnosticPrincipal { get; set; }

    /// <summary>
    /// ID de la consultation ayant généré cette hospitalisation
    /// </summary>
    [Column("id_consultation")]
    public int? IdConsultation { get; set; }

    /// <summary>
    /// ID du service concerné (pour notifier le Major)
    /// </summary>
    [Column("id_service")]
    public int? IdService { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("IdPatient")]
    public virtual Patient? Patient { get; set; }

    [ForeignKey("IdLit")]
    public virtual Lit? Lit { get; set; }

    [ForeignKey("IdMedecin")]
    public virtual Medecin? Medecin { get; set; }

    [ForeignKey("IdConsultation")]
    public virtual Consultation? Consultation { get; set; }

    [ForeignKey("IdService")]
    public virtual Service? Service { get; set; }

    /// <summary>
    /// Soins prescrits pour cette hospitalisation
    /// </summary>
    public virtual ICollection<SoinHospitalisation> Soins { get; set; } = new List<SoinHospitalisation>();
}

/// <summary>
/// Entité SoinHospitalisation - Mappe à la table 'soin_hospitalisation'
/// Représente un soin prescrit dans le cadre d'une hospitalisation
/// </summary>
[Table("soin_hospitalisation")]
public class SoinHospitalisation
{
    [Key]
    [Column("id_soin")]
    public int IdSoin { get; set; }

    [Column("id_hospitalisation")]
    [Required]
    public int IdHospitalisation { get; set; }

    /// <summary>
    /// Type de soin: soins_infirmiers, surveillance, reeducation, nutrition, autre
    /// </summary>
    [Column("type_soin")]
    [Required]
    [MaxLength(100)]
    public string TypeSoin { get; set; } = string.Empty;

    /// <summary>
    /// Description du soin
    /// </summary>
    [Column("description")]
    [Required]
    [MaxLength(255)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Fréquence: 1x/jour, 2x/jour, 3x/jour, etc. (legacy)
    /// </summary>
    [Column("frequence")]
    [MaxLength(100)]
    public string? Frequence { get; set; }

    /// <summary>
    /// Durée en jours
    /// </summary>
    [Column("duree_jours")]
    public int? DureeJours { get; set; }

    /// <summary>
    /// Nombre de fois par jour (1 à N séances)
    /// </summary>
    [Column("nb_fois_par_jour")]
    public int NbFoisParJour { get; set; } = 1;

    /// <summary>
    /// Horaires personnalisés au format JSON: ["08:00","12:00","18:00"]
    /// Si null, les horaires sont générés automatiquement
    /// </summary>
    [Column("horaires_personnalises")]
    public string? HorairesPersonnalises { get; set; }

    /// <summary>
    /// Moments de la journée: matin,midi,soir,nuit (legacy - remplacé par séances)
    /// </summary>
    [Column("moments")]
    [MaxLength(100)]
    public string? Moments { get; set; }

    /// <summary>
    /// Priorité: basse, normale, haute, urgente
    /// </summary>
    [Column("priorite")]
    [MaxLength(20)]
    public string Priorite { get; set; } = "normale";

    /// <summary>
    /// Instructions spécifiques
    /// </summary>
    [Column("instructions")]
    public string? Instructions { get; set; }

    /// <summary>
    /// Statut: prescrit, en_cours, termine, annule
    /// </summary>
    [Column("statut")]
    [MaxLength(20)]
    public string Statut { get; set; } = "prescrit";

    [Column("date_prescription")]
    public DateTime DatePrescription { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date de début du traitement
    /// </summary>
    [Column("date_debut")]
    public DateTime? DateDebut { get; set; }

    /// <summary>
    /// Date de fin prévue du soin
    /// </summary>
    [Column("date_fin_prevue")]
    public DateTime? DateFinPrevue { get; set; }

    /// <summary>
    /// Médecin ayant prescrit le soin
    /// </summary>
    [Column("id_prescripteur")]
    public int? IdPrescripteur { get; set; }

    /// <summary>
    /// Nombre total d'exécutions prévues (calculé automatiquement)
    /// </summary>
    [Column("nb_executions_prevues")]
    public int NbExecutionsPrevues { get; set; } = 0;

    /// <summary>
    /// Nombre d'exécutions effectuées
    /// </summary>
    [Column("nb_executions_effectuees")]
    public int NbExecutionsEffectuees { get; set; } = 0;

    // Navigation
    [ForeignKey("IdHospitalisation")]
    public virtual Hospitalisation? Hospitalisation { get; set; }

    [ForeignKey("IdPrescripteur")]
    public virtual Medecin? Prescripteur { get; set; }

    /// <summary>
    /// Planification des exécutions de ce soin
    /// </summary>
    public virtual ICollection<ExecutionSoin> Executions { get; set; } = new List<ExecutionSoin>();
}

/// <summary>
/// Entité ExecutionSoin - Mappe à la table 'execution_soin'
/// Représente une exécution planifiée ou effectuée d'un soin prescrit
/// </summary>
[Table("execution_soin")]
public class ExecutionSoin
{
    [Key]
    [Column("id_execution")]
    public int IdExecution { get; set; }

    [Column("id_soin")]
    [Required]
    public int IdSoin { get; set; }

    /// <summary>
    /// Date prévue pour l'exécution
    /// </summary>
    [Column("date_prevue")]
    public DateTime? DatePrevue { get; set; }

    /// <summary>
    /// Moment de la journée: matin, midi, soir, nuit, autre (legacy)
    /// </summary>
    [Column("moment")]
    [MaxLength(20)]
    public string? Moment { get; set; } = "matin";

    /// <summary>
    /// Numéro de séance dans la journée (1, 2, 3...)
    /// </summary>
    [Column("numero_seance")]
    public int NumeroSeance { get; set; } = 1;

    /// <summary>
    /// Heure prévue pour l'exécution
    /// </summary>
    [Column("heure_prevue")]
    public TimeSpan? HeurePrevue { get; set; }

    /// <summary>
    /// Heure réelle de l'exécution
    /// </summary>
    [Column("heure_execution")]
    public TimeSpan? HeureExecution { get; set; }

    /// <summary>
    /// Statut: prevu, fait, manque, reporte, annule
    /// </summary>
    [Column("statut")]
    [MaxLength(20)]
    public string? Statut { get; set; } = "prevu";

    /// <summary>
    /// Date et heure réelle de l'exécution
    /// </summary>
    [Column("date_execution")]
    public DateTime? DateExecution { get; set; }

    /// <summary>
    /// ID de l'infirmier ayant effectué le soin
    /// </summary>
    [Column("id_executant")]
    public int? IdExecutant { get; set; }

    /// <summary>
    /// Observations lors de l'exécution
    /// </summary>
    [Column("observations")]
    public string? Observations { get; set; }

    /// <summary>
    /// Numéro de l'exécution (1ère, 2ème, etc.)
    /// </summary>
    [Column("numero_execution")]
    public int NumeroExecution { get; set; } = 1;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("IdSoin")]
    public virtual SoinHospitalisation? Soin { get; set; }

    [ForeignKey("IdExecutant")]
    public virtual Utilisateur? Executant { get; set; }
}
