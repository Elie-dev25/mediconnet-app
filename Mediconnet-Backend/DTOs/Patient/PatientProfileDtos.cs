namespace Mediconnet_Backend.DTOs.Patient;

/// <summary>
/// DTO pour le profil complet du patient
/// </summary>
public class PatientProfileDto
{
    public int IdUser { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? Naissance { get; set; }
    public string? Sexe { get; set; }
    public string? Telephone { get; set; }
    public string? SituationMatrimoniale { get; set; }
    public string? Adresse { get; set; }
    public string? Photo { get; set; }
    public string? NumeroDossier { get; set; }
    public string? Ethnie { get; set; }
    public string? GroupeSanguin { get; set; }
    public int? NbEnfants { get; set; }
    public string? PersonneContact { get; set; }
    public string? NumeroContact { get; set; }
    public string? Profession { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool IsProfileComplete { get; set; }
}

/// <summary>
/// DTO pour la mise a jour du profil patient
/// </summary>
public class UpdatePatientProfileRequest
{
    public DateTime? Naissance { get; set; }
    public string? Sexe { get; set; }
    public string? Telephone { get; set; }
    public string? SituationMatrimoniale { get; set; }
    public string? Adresse { get; set; }
    public string? Ethnie { get; set; }
    public string? GroupeSanguin { get; set; }
    public int? NbEnfants { get; set; }
    public string? PersonneContact { get; set; }
    public string? NumeroContact { get; set; }
    public string? Profession { get; set; }
}

/// <summary>
/// DTO pour verifier si le profil est complet
/// </summary>
public class ProfileStatusDto
{
    public bool IsComplete { get; set; }
    public List<string> MissingFields { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour le dashboard patient
/// </summary>
public class PatientDashboardDto
{
    public List<VisiteDto> VisitesAVenir { get; set; } = new();
    public List<VisiteDto> VisitesPassees { get; set; } = new();
    public List<TraitementDto> TraitementsPrevus { get; set; } = new();
    public PatientStatsDto Stats { get; set; } = new();
}

/// <summary>
/// DTO pour une visite/rendez-vous
/// </summary>
public class VisiteDto
{
    public int IdRendezVous { get; set; }
    public DateTime DateHeure { get; set; }
    public int Duree { get; set; }
    public string Statut { get; set; } = "";
    public string TypeRdv { get; set; } = "";
    public string? Motif { get; set; }
    public string NomMedecin { get; set; } = "";
    public string? Service { get; set; }
}

/// <summary>
/// DTO pour un traitement/prescription
/// </summary>
public class TraitementDto
{
    public int IdPrescription { get; set; }
    public string Medicament { get; set; } = "";
    public string Posologie { get; set; } = "";
    public DateTime DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public string NomMedecin { get; set; } = "";
}

/// <summary>
/// DTO pour les statistiques patient
/// </summary>
public class PatientStatsDto
{
    public int TotalRendezVous { get; set; }
    public int RendezVousAVenir { get; set; }
    public int RendezVousPasses { get; set; }
    public int Ordonnances { get; set; }
    public int Examens { get; set; }
    public int Factures { get; set; }
}

#region Dossier Medical DTOs

/// <summary>
/// DTO principal pour le dossier medical complet
/// </summary>
public class DossierMedicalDto
{
    public PatientProfileDto Patient { get; set; } = new();
    public List<AntecedentDto> Antecedents { get; set; } = new();
    public List<AllergieDto> Allergies { get; set; } = new();
    public List<ConsultationHistoryDto> Consultations { get; set; } = new();
    public List<OrdonnanceDto> Ordonnances { get; set; } = new();
    public List<ExamenDto> Examens { get; set; } = new();
    public DossierStatsDto Stats { get; set; } = new();
}

/// <summary>
/// DTO pour les statistiques du dossier medical
/// </summary>
public class DossierStatsDto
{
    public int TotalConsultations { get; set; }
    public int TotalOrdonnances { get; set; }
    public int TotalExamens { get; set; }
    public DateTime? DerniereVisite { get; set; }
}

/// <summary>
/// DTO pour l'historique des consultations
/// </summary>
public class ConsultationHistoryDto
{
    public int IdConsultation { get; set; }
    public DateTime DateConsultation { get; set; }
    public string Motif { get; set; } = "";
    public string? DiagnosticPrincipal { get; set; }
    public string NomMedecin { get; set; } = "";
    public string? Specialite { get; set; }
    public string Statut { get; set; } = "";
}

/// <summary>
/// DTO pour une ordonnance
/// </summary>
public class OrdonnanceDto
{
    public int IdOrdonnance { get; set; }
    public DateTime DateOrdonnance { get; set; }
    public string NomMedecin { get; set; } = "";
    public List<MedicamentPrescritDto> Medicaments { get; set; } = new();
    public string Statut { get; set; } = "";
}

/// <summary>
/// DTO pour un medicament prescrit
/// </summary>
public class MedicamentPrescritDto
{
    public string Nom { get; set; } = "";
    public string Dosage { get; set; } = "";
    public string Frequence { get; set; } = "";
    public string Duree { get; set; } = "";
    public string? Instructions { get; set; }
}

/// <summary>
/// DTO pour un examen medical
/// </summary>
public class ExamenDto
{
    public int IdExamen { get; set; }
    public DateTime DateExamen { get; set; }
    public string TypeExamen { get; set; } = "";
    public string NomExamen { get; set; } = "";
    public string? Resultat { get; set; }
    public string NomMedecin { get; set; } = "";
    public string Statut { get; set; } = "";
    public bool Urgent { get; set; }
}

/// <summary>
/// DTO pour un antecedent medical
/// </summary>
public class AntecedentDto
{
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime? DateDebut { get; set; }
    public bool Actif { get; set; }
}

/// <summary>
/// DTO pour une allergie
/// </summary>
public class AllergieDto
{
    public string Type { get; set; } = "";
    public string Allergene { get; set; } = "";
    public string Severite { get; set; } = "";
    public string? Reaction { get; set; }
}

#endregion
