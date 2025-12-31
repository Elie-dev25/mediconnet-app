namespace Mediconnet_Backend.DTOs.Accueil;

/// <summary>
/// DTO pour l'enregistrement d'une nouvelle consultation
/// </summary>
public class EnregistrerConsultationRequest
{
    public int IdPatient { get; set; }
    public string Motif { get; set; } = string.Empty;
    public int IdMedecin { get; set; }
    public decimal PrixConsultation { get; set; }
    /// <summary>
    /// Heure du créneau sélectionné par l'agent d'accueil
    /// </summary>
    public DateTime? DateHeureCreneau { get; set; }
}

/// <summary>
/// DTO pour la réponse après enregistrement de consultation
/// </summary>
public class EnregistrerConsultationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int IdConsultation { get; set; }
    public int IdPaiement { get; set; }
    public string NumeroPaiement { get; set; } = string.Empty;
    public PatientConsultationDto? Patient { get; set; }
}

/// <summary>
/// DTO pour les informations du patient dans la réponse
/// </summary>
public class PatientConsultationDto
{
    public int IdUser { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string NumeroDossier { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour un médecin disponible
/// </summary>
public class MedecinDisponibleDto
{
    public int IdMedecin { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Specialite { get; set; } = string.Empty;
    public string? Service { get; set; }
    public int? IdService { get; set; }
    public int? IdSpecialite { get; set; }
}

/// <summary>
/// DTO pour un service hospitalier
/// </summary>
public class ServiceDto
{
    public int IdService { get; set; }
    public string NomService { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// DTO pour une spécialité médicale
/// </summary>
public class SpecialiteDto
{
    public int IdSpecialite { get; set; }
    public string NomSpecialite { get; set; } = string.Empty;
}

/// <summary>
/// Requête pour filtrer les médecins
/// </summary>
public class FiltrerMedecinsRequest
{
    public int? IdService { get; set; }
    public int? IdSpecialite { get; set; }
}

/// <summary>
/// DTO pour un médecin avec son statut de disponibilité
/// </summary>
public class MedecinAvecDisponibiliteDto
{
    public int IdMedecin { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Specialite { get; set; } = string.Empty;
    public string? Service { get; set; }
    public int? IdService { get; set; }
    public int? IdSpecialite { get; set; }
    
    // Statut de disponibilité
    public string Statut { get; set; } = "disponible"; // disponible, occupe, absent
    public bool EstDisponible { get; set; } = true;
    
    // Détails de charge
    public int PatientsEnAttente { get; set; } = 0;
    public int PatientsEnConsultation { get; set; } = 0;
    public int RendezVousAujourdhui { get; set; } = 0;
    
    // Prochaine disponibilité
    public string? RaisonIndisponibilite { get; set; }
    public DateTime? ProchaineDisponibilite { get; set; }
    
    // Temps d'attente estimé (en minutes)
    public int? TempsAttenteEstime { get; set; }
}

/// <summary>
/// Réponse pour la liste des médecins avec disponibilité
/// </summary>
public class MedecinsDisponibiliteResponse
{
    public bool Success { get; set; } = true;
    public List<MedecinAvecDisponibiliteDto> Medecins { get; set; } = new();
    public int TotalDisponibles { get; set; }
    public int TotalOccupes { get; set; }
    public int TotalAbsents { get; set; }
}

/// <summary>
/// Réponse pour la vérification de paiement valide (règle des 14 jours)
/// </summary>
public class VerifierPaiementResponse
{
    public bool PaiementValide { get; set; }
    public string? NumeroFacture { get; set; }
    public DateTime? DatePaiement { get; set; }
    public DateTime? DateExpiration { get; set; }
    public string? Message { get; set; }
}
