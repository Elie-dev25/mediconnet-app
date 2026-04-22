using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mediconnet_Backend.DTOs.Planning;

// ==================== CRÃ‰NEAUX DISPONIBLES ====================

/// <summary>
/// DTO pour afficher un crÃ©neau horaire configurÃ©
/// </summary>
public class CreneauHoraireDto
{
    public int IdCreneau { get; set; }
    public int JourSemaine { get; set; } // 1=Lundi, 2=Mardi, ..., 7=Dimanche
    public string JourNom { get; set; } = "";
    public string HeureDebut { get; set; } = "";
    public string HeureFin { get; set; } = "";
    public int DureeParDefaut { get; set; }
    public bool Actif { get; set; }
    public DateTime? DateDebutValidite { get; set; }
    public DateTime? DateFinValidite { get; set; }
    public bool EstSemaineType { get; set; } = true;
}

/// <summary>
/// DTO pour crÃ©er/modifier un crÃ©neau horaire
/// </summary>
public class CreateCreneauRequest
{
    [Required]
    [Range(1, 7, ErrorMessage = "Le jour doit Ãªtre entre 1 (Lundi) et 7 (Dimanche)")]
    public int JourSemaine { get; set; }

    [Required]
    [RegularExpression(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Format heure invalide (HH:mm)")]
    public string HeureDebut { get; set; } = "";

    [Required]
    [RegularExpression(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Format heure invalide (HH:mm)")]
    public string HeureFin { get; set; } = "";

    [Range(10, 120, ErrorMessage = "La durÃ©e doit Ãªtre entre 10 et 120 minutes")]
    public int DureeParDefaut { get; set; } = 30;

    /// <summary>
    /// Date de dÃ©but de validitÃ© (null = semaine type rÃ©currente)
    /// </summary>
    public DateTime? DateDebutValidite { get; set; }

    /// <summary>
    /// Date de fin de validitÃ© (null = valide indÃ©finiment)
    /// </summary>
    public DateTime? DateFinValidite { get; set; }

    /// <summary>
    /// Si true, crÃ©e un crÃ©neau de semaine type (rÃ©current). Si false, crÃ©neau spÃ©cifique Ã  la pÃ©riode.
    /// </summary>
    public bool EstSemaineType { get; set; } = true;
}

/// <summary>
/// DTO pour la semaine type du mÃ©decin
/// </summary>
public class SemaineTypeDto
{
    public List<JourSemaineDto> Jours { get; set; } = new();
    public int TotalHeures { get; set; }
    public int TotalCreneaux { get; set; }
}

/// <summary>
/// DTO pour une semaine spÃ©cifique avec ses crÃ©neaux
/// </summary>
public class SemainePlanningDto
{
    public DateTime DateDebut { get; set; }
    public DateTime DateFin { get; set; }
    public string Label { get; set; } = "";
    public List<JourSemaineDto> Jours { get; set; } = new();
    public int TotalHeures { get; set; }
    public int TotalCreneaux { get; set; }
    public bool EstSemaineCourante { get; set; }
}

public class JourSemaineDto
{
    public int Numero { get; set; }
    public string Nom { get; set; } = "";
    public DateTime? Date { get; set; }
    public List<CreneauHoraireDto> Creneaux { get; set; } = new();
    public bool Travaille { get; set; }
    public string HeuresTotal { get; set; } = "0h00";
    public bool EstIndisponible { get; set; }
}

// ==================== INDISPONIBILITÃ‰S ====================

/// <summary>
/// DTO pour afficher une indisponibilitÃ©
/// </summary>
public class IndisponibiliteDto
{
    public int IdIndisponibilite { get; set; }
    public DateTime DateDebut { get; set; }
    public DateTime DateFin { get; set; }
    public string Type { get; set; } = "";
    public string TypeLibelle { get; set; } = "";
    public string? Motif { get; set; }
    public bool JourneeComplete { get; set; }
    public int NombreJours { get; set; }
}

/// <summary>
/// DTO pour crÃ©er une indisponibilitÃ©
/// </summary>
public class CreateIndisponibiliteRequest
{
    [Required]
    [JsonRequired]
    public DateTime DateDebut { get; set; }

    [Required]
    [JsonRequired]
    public DateTime DateFin { get; set; }

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = "conge"; // conge, maladie, formation, autre

    [MaxLength(200)]
    public string? Motif { get; set; }

    public bool JourneeComplete { get; set; } = true;
}

// ==================== PLANNING GLOBAL ====================

/// <summary>
/// DTO pour le tableau de bord planning du mÃ©decin
/// </summary>
public class PlanningDashboardDto
{
    public int RdvAujourdHui { get; set; }
    public int RdvCetteSemaine { get; set; }
    public int RdvCeMois { get; set; }
    public int JoursCongeRestants { get; set; }
    public List<RdvPlanningDto> ProchainsRdv { get; set; } = new();
    public IndisponibiliteDto? ProchaineIndisponibilite { get; set; }
}

/// <summary>
/// DTO simplifiÃ© pour afficher un RDV dans le planning
/// </summary>
public class RdvPlanningDto
{
    public int IdRendezVous { get; set; }
    public DateTime DateHeure { get; set; }
    public int Duree { get; set; }
    public int PatientId { get; set; }
    public string PatientNom { get; set; } = "";
    public string PatientPrenom { get; set; } = "";
    public string? NumeroDossier { get; set; }
    public string? Motif { get; set; }
    public string TypeRdv { get; set; } = "";
    public string Statut { get; set; } = "";
}

/// <summary>
/// DTO pour le calendrier journalier
/// </summary>
public class JourneeCalendrierDto
{
    public DateTime Date { get; set; }
    public string JourNom { get; set; } = "";
    public bool EstIndisponible { get; set; }
    public string? MotifIndisponibilite { get; set; }
    public List<RdvPlanningDto> RendezVous { get; set; } = new();
    public List<CreneauLibreDto> CreneauxLibres { get; set; } = new();
}

public class CreneauLibreDto
{
    public DateTime DateHeure { get; set; }
    public int Duree { get; set; }
}
