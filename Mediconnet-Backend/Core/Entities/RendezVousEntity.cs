using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité représentant un rendez-vous médical
/// </summary>
public class RendezVous
{
    [Key]
    public int IdRendezVous { get; set; }

    [Required]
    public int IdPatient { get; set; }

    [Required]
    public int IdMedecin { get; set; }

    public int? IdService { get; set; }

    [Required]
    public DateTime DateHeure { get; set; }

    /// <summary>
    /// Durée en minutes
    /// </summary>
    public int Duree { get; set; } = 30;

    [Required]
    [MaxLength(30)]
    public string Statut { get; set; } = "planifie"; // planifie, confirme, en_cours, termine, annule, absent

    [MaxLength(100)]
    public string? Motif { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(500)]
    public string? MotifAnnulation { get; set; }

    public DateTime? DateAnnulation { get; set; }

    public int? AnnulePar { get; set; }

    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public DateTime? DateModification { get; set; }

    /// <summary>
    /// Type de rendez-vous
    /// </summary>
    [MaxLength(50)]
    public string TypeRdv { get; set; } = "consultation"; // consultation, suivi, urgence, examen, vaccination

    /// <summary>
    /// Indique si le patient a été notifié par SMS/Email
    /// </summary>
    public bool Notifie { get; set; } = false;

    /// <summary>
    /// Rappel envoyé (24h avant)
    /// </summary>
    public bool RappelEnvoye { get; set; } = false;

    /// <summary>
    /// Version pour le verrouillage optimiste (gestion de concurrence)
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // Navigation
    public virtual Patient? Patient { get; set; }
    public virtual Medecin? Medecin { get; set; }
    public virtual Service? Service { get; set; }
}

/// <summary>
/// Verrou temporaire sur un créneau horaire pour éviter les doubles réservations
/// Expire automatiquement après un délai (ex: 5 minutes)
/// </summary>
public class SlotLock
{
    [Key]
    public int IdLock { get; set; }

    [Required]
    public int IdMedecin { get; set; }

    [Required]
    public DateTime DateHeure { get; set; }

    public int Duree { get; set; } = 30;

    /// <summary>
    /// Utilisateur qui détient le verrou
    /// </summary>
    [Required]
    public int IdUser { get; set; }

    /// <summary>
    /// Token unique pour identifier le verrou
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string LockToken { get; set; } = string.Empty;

    /// <summary>
    /// Date/heure d'expiration du verrou
    /// </summary>
    [Required]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Date de création
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual Medecin? Medecin { get; set; }
    public virtual Utilisateur? User { get; set; }
}

/// <summary>
/// Indisponibilités d'un médecin (congés, absences, formations)
/// </summary>
public class IndisponibiliteMedecin
{
    [Key]
    public int IdIndisponibilite { get; set; }

    [Required]
    public int IdMedecin { get; set; }

    [Required]
    public DateTime DateDebut { get; set; }

    [Required]
    public DateTime DateFin { get; set; }

    [MaxLength(50)]
    public string Type { get; set; } = "conge"; // conge, maladie, formation, autre

    [MaxLength(200)]
    public string? Motif { get; set; }

    public bool JourneeComplete { get; set; } = true;

    // Navigation
    public virtual Medecin? Medecin { get; set; }
}

/// <summary>
/// Créneaux horaires disponibles pour un médecin
/// </summary>
public class CreneauDisponible
{
    [Key]
    public int IdCreneau { get; set; }

    [Required]
    public int IdMedecin { get; set; }

    /// <summary>
    /// Jour de la semaine (1=Lundi, 2=Mardi, ..., 7=Dimanche)
    /// </summary>
    [Required]
    public int JourSemaine { get; set; }

    [Required]
    public TimeSpan HeureDebut { get; set; }

    [Required]
    public TimeSpan HeureFin { get; set; }

    /// <summary>
    /// Durée par défaut des créneaux en minutes
    /// </summary>
    public int DureeParDefaut { get; set; } = 30;

    public bool Actif { get; set; } = true;

    /// <summary>
    /// Date de début de validité (null = valide depuis toujours / semaine type)
    /// </summary>
    public DateTime? DateDebutValidite { get; set; }

    /// <summary>
    /// Date de fin de validité (null = valide indéfiniment / semaine type)
    /// </summary>
    public DateTime? DateFinValidite { get; set; }

    /// <summary>
    /// Indique si c'est un créneau de la semaine type (récurrent) ou spécifique à une période
    /// </summary>
    public bool EstSemaineType { get; set; } = true;

    // Navigation
    public virtual Medecin? Medecin { get; set; }
}
