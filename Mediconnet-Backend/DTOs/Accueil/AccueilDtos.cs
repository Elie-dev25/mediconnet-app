using System.ComponentModel.DataAnnotations;

namespace Mediconnet_Backend.DTOs.Accueil;

/// <summary>
/// DTO pour le profil de l'agent d'accueil
/// </summary>
public class AccueilProfileDto
{
    public int IdUser { get; set; }
    public string Nom { get; set; } = "";
    public string Prenom { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Telephone { get; set; }
    public string? Poste { get; set; }
    public DateTime? DateEmbauche { get; set; }
    public DateTime? CreatedAt { get; set; }
}

/// <summary>
/// DTO pour le dashboard de l'accueil
/// </summary>
public class AccueilDashboardDto
{
    public int PatientsEnregistresAujourdHui { get; set; }
    public int PatientsEnAttente { get; set; }
    public int RdvPrevusAujourdHui { get; set; }
    public int RdvEnCours { get; set; }
}

/// <summary>
/// DTO pour la liste des patients en file d'attente
/// </summary>
public class PatientFileAttenteDto
{
    public int IdPatient { get; set; }
    public string NumeroDossier { get; set; } = "";
    public string Nom { get; set; } = "";
    public string Prenom { get; set; } = "";
    public string? Telephone { get; set; }
    public DateTime HeureArrivee { get; set; }
    public string? Motif { get; set; }
    public string Statut { get; set; } = "en_attente";
    public int? IdMedecin { get; set; }
    public string? NomMedecin { get; set; }
}

/// <summary>
/// Requête d'enregistrement rapide d'un patient à l'arrivée
/// </summary>
public class EnregistrerArriveePatientRequest
{
    [Required(ErrorMessage = "Le nom est requis")]
    [StringLength(100, ErrorMessage = "Le nom ne peut dépasser 100 caractères")]
    public string Nom { get; set; } = "";

    [Required(ErrorMessage = "Le prénom est requis")]
    [StringLength(100, ErrorMessage = "Le prénom ne peut dépasser 100 caractères")]
    public string Prenom { get; set; } = "";

    [Phone(ErrorMessage = "Numéro de téléphone invalide")]
    public string? Telephone { get; set; }

    [EmailAddress(ErrorMessage = "Email invalide")]
    public string? Email { get; set; }

    public DateTime? DateNaissance { get; set; }

    public string? Sexe { get; set; }

    public string? Motif { get; set; }

    /// <summary>
    /// ID du médecin si RDV prévu
    /// </summary>
    public int? IdMedecinCible { get; set; }

    /// <summary>
    /// ID du RDV existant si le patient vient pour un RDV
    /// </summary>
    public int? IdRendezVous { get; set; }
}

/// <summary>
/// Réponse après enregistrement d'une arrivée
/// </summary>
public class EnregistrerArriveeResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int? IdPatient { get; set; }
    public string? NumeroDossier { get; set; }
    public bool NouveauPatient { get; set; }
}

/// <summary>
/// Requête de recherche de patient
/// </summary>
public class RecherchePatientRequest
{
    public string? Terme { get; set; }
    public string? NumeroDossier { get; set; }
    public string? Telephone { get; set; }
}

/// <summary>
/// DTO pour les RDV du jour visibles par l'accueil
/// </summary>
public class RdvAccueilDto
{
    public int IdRendezVous { get; set; }
    public DateTime DateHeure { get; set; }
    public string PatientNom { get; set; } = "";
    public string PatientPrenom { get; set; } = "";
    public string? PatientTelephone { get; set; }
    public string MedecinNom { get; set; } = "";
    public string MedecinPrenom { get; set; } = "";
    public string? Specialite { get; set; }
    public string Statut { get; set; } = "";
    public string? Motif { get; set; }
    public bool PatientArrive { get; set; }
}

/// <summary>
/// Requête pour marquer l'arrivée d'un patient pour un RDV
/// </summary>
public class MarquerArriveeRdvRequest
{
    [Required]
    public int IdRendezVous { get; set; }
}
