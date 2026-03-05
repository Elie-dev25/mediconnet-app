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
    
    // Informations utilisateur étendues
    public string? Nationalite { get; set; }
    public string? RegionOrigine { get; set; }
    
    // Informations médicales
    public string? MaladiesChroniques { get; set; }
    public bool? AllergiesConnues { get; set; }
    public string? AllergiesDetails { get; set; }
    public bool? AntecedentsFamiliaux { get; set; }
    public string? AntecedentsFamiliauxDetails { get; set; }
    public bool? OperationsChirurgicales { get; set; }
    public string? OperationsDetails { get; set; }
    
    // Habitudes de vie
    public bool? ConsommationAlcool { get; set; }
    public string? FrequenceAlcool { get; set; }
    public bool? Tabagisme { get; set; }
    public bool? ActivitePhysique { get; set; }
    
    // Assurance
    public int? AssuranceId { get; set; }
    public string? NomAssurance { get; set; }
    public string? NumeroCarteAssurance { get; set; }
    public decimal? TauxCouvertureOverride { get; set; }
    public DateTime? DateDebutValidite { get; set; }
    public DateTime? DateFinValidite { get; set; }
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
    public string? Nationalite { get; set; }
    public string? RegionOrigine { get; set; }
    public string? Ethnie { get; set; }
    public string? GroupeSanguin { get; set; }
    public int? NbEnfants { get; set; }
    public string? PersonneContact { get; set; }
    public string? NumeroContact { get; set; }
    public string? Profession { get; set; }
    public string? FrequenceAlcool { get; set; }
    public string? AllergiesDetails { get; set; }
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
    public List<HospitalisationHistoryDto> Hospitalisations { get; set; } = new();
    public List<OrientationHistoryDto> Orientations { get; set; } = new();
    public DossierStatsDto Stats { get; set; } = new();
}

/// <summary>
/// DTO pour l'historique des hospitalisations dans le dossier patient
/// </summary>
public class HospitalisationHistoryDto
{
    public int IdAdmission { get; set; }
    public DateTime DateEntree { get; set; }
    public DateTime? DateSortiePrevue { get; set; }
    public DateTime? DateSortie { get; set; }
    public string Motif { get; set; } = "";
    public string? MotifSortie { get; set; }
    public string? ResumeMedical { get; set; }
    public string? DiagnosticPrincipal { get; set; }
    public string Statut { get; set; } = "";
    public string? Urgence { get; set; }
    public string? MedecinNom { get; set; }
    public string? ServiceNom { get; set; }
    public string? NumeroChambre { get; set; }
    public string? NumeroLit { get; set; }
    public int? DureeJours { get; set; }
}

/// <summary>
/// DTO pour l'historique des orientations dans le dossier patient
/// </summary>
public class OrientationHistoryDto
{
    public int IdOrientation { get; set; }
    public string TypeOrientation { get; set; } = "";
    public string? NomDestinataire { get; set; }
    public string? NomMedecinOriente { get; set; }
    public string? NomSpecialite { get; set; }
    public string Motif { get; set; } = "";
    public bool Prioritaire { get; set; }
    public bool Urgence { get; set; }
    public string Statut { get; set; } = "en_attente";
    public DateTime CreatedAt { get; set; }
    public string? MedecinPrescripteur { get; set; }
    public int? IdConsultation { get; set; }
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
/// Hiérarchie: Catégorie → Spécialité → Nom
/// </summary>
public class ExamenDto
{
    public int IdExamen { get; set; }
    public DateTime DateExamen { get; set; }
    public string Categorie { get; set; } = "";
    public string Specialite { get; set; } = "";
    public string NomExamen { get; set; } = "";
    public string? Resultat { get; set; }
    public string NomMedecin { get; set; } = "";
    public string Statut { get; set; } = "";
    public bool Urgent { get; set; }
    public bool Disponible { get; set; } = true;
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
