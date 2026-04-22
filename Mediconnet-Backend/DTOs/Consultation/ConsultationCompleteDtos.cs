using System.Text.Json.Serialization;
namespace Mediconnet_Backend.DTOs.Consultation;

// ==================== DOSSIER PATIENT ====================

public class DossierPatientDto
{
    public int IdPatient { get; set; }
    public string NumeroDossier { get; set; } = "";
    
    // Informations utilisateur
    public string Nom { get; set; } = "";
    public string Prenom { get; set; } = "";
    public DateTime? Naissance { get; set; }
    public int? Age { get; set; }
    public string? Sexe { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? Adresse { get; set; }
    public string? Nationalite { get; set; }
    public string? RegionOrigine { get; set; }
    public string? SituationMatrimoniale { get; set; }
    public string? Profession { get; set; }
    public string? Ethnie { get; set; }
    public int? NbEnfants { get; set; }
    
    // Informations mÃ©dicales
    public string? GroupeSanguin { get; set; }
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
    
    // Contact d'urgence
    public string? PersonneContact { get; set; }
    public string? NumeroContact { get; set; }
    
    // Assurance
    public string? NomAssurance { get; set; }
    public string? NumeroCarteAssurance { get; set; }
    public decimal? TauxCouvertureOverride { get; set; }
    public DateTime? DateDebutValidite { get; set; }
    public DateTime? DateFinValidite { get; set; }
    
    // Dates systÃ¨me
    public DateTime? DateCreation { get; set; }
    
    // Historique
    public List<HistoriqueConsultationDto> Consultations { get; set; } = new();
    public List<HistoriqueOrdonnanceDto> Ordonnances { get; set; } = new();
    public List<HistoriqueExamenDto> Examens { get; set; } = new();
}

public class HistoriqueConsultationDto
{
    public int IdConsultation { get; set; }
    public DateTime DateHeure { get; set; }
    public string? Motif { get; set; }
    public string? Diagnostic { get; set; }
    public string? Statut { get; set; }
    public string MedecinNom { get; set; } = "";
    public string? Specialite { get; set; }
}

public class HistoriqueOrdonnanceDto
{
    public int IdOrdonnance { get; set; }
    public DateTime DateCreation { get; set; }
    public string? DureeTraitement { get; set; }
    public List<MedicamentDto> Medicaments { get; set; } = new();
}

public class HistoriqueExamenDto
{
    public int IdExamen { get; set; }
    public string Categorie { get; set; } = "";
    public string Specialite { get; set; } = "";
    public string NomExamen { get; set; } = "";
    public string Statut { get; set; } = "";
    public DateTime DatePrescription { get; set; }
    public DateTime? DateRealisation { get; set; }
    public string? Resultats { get; set; }
}

public class ExamenGynecologiqueDto
{
    public string? InspectionExterne { get; set; }
    public string? ExamenSpeculum { get; set; }
    public string? ToucherVaginal { get; set; }
    public string? AutresObservations { get; set; }
}

public class ExamenChirurgicalDto
{
    public string? ZoneExaminee { get; set; }
    public string? InspectionLocale { get; set; }
    public string? PalpationLocale { get; set; }
    public string? SignesInflammatoires { get; set; }
    public string? CicatricesExistantes { get; set; }
    public string? MobiliteFonction { get; set; }
    public string? ConclusionChirurgicale { get; set; }
    /// <summary>surveillance, traitement_medical, indication_operatoire</summary>
    public string? Decision { get; set; }
    public string? NotesComplementaires { get; set; }
}

public class ExamenAnesthesiqueDto
{
    // AnamnÃ¨se spÃ©cifique
    public string? AntecedentsMedicaux { get; set; }
    public string? ProblemesCardiaques { get; set; }
    public string? ProblemesRespiratoires { get; set; }
    public string? AllergiesAnesthesie { get; set; }
    public string? AntecedentsChirurgicaux { get; set; }
    public string? ProblemesAnesthesiePrecedente { get; set; }
    public string? MedicamentsEnCours { get; set; }
    public bool? ApneeSommeil { get; set; }
    public bool? TroublesCoagulation { get; set; }
    
    // Examen clinique
    public string? AuscultationCardiaque { get; set; }
    public string? AuscultationPulmonaire { get; set; }
    
    // Voies aÃ©riennes (critique)
    public string? OuvertureBouche { get; set; }
    public int? Mallampati { get; set; }
    public string? EtatDents { get; set; }
    public string? MobiliteCou { get; set; }
    public decimal? DistanceThyroMentonniere { get; set; }
    public bool? IntubationDifficilePrevue { get; set; }
    public string? NotesVoiesAeriennes { get; set; }
    
    // Ã‰valuation du risque
    public int? ClassificationASA { get; set; }
    public string? NiveauRisque { get; set; }
    public string? RisqueCardiaque { get; set; }
    public string? RisqueRespiratoire { get; set; }
    public string? RisqueAllergique { get; set; }
    public string? RisqueHemorragique { get; set; }
    
    // Choix anesthÃ©sie
    public string? TypeAnesthesie { get; set; }
    public string? SousTypeAnesthesie { get; set; }
    public string? JustificationAnesthesie { get; set; }
    public string? ExplicationPatient { get; set; }
    public bool? ConsentementObtenu { get; set; }
    
    // Consignes prÃ©opÃ©ratoires
    public int? DureeJeune { get; set; }
    public string? InstructionsJeune { get; set; }
    public bool? ArretTabac { get; set; }
    public string? InstructionsHygiene { get; set; }
    public string? AutresConsignes { get; set; }
    
    // Conclusion
    public string? Aptitude { get; set; }
    public string? Reserves { get; set; }
    public string? MotifNonAptitude { get; set; }
    public string? Recommandations { get; set; }
}

// ==================== CONSULTATION MULTI-ETAPES ====================

public class ConsultationEnCoursDto
{
    public int IdConsultation { get; set; }
    public int IdPatient { get; set; }
    public string PatientNom { get; set; } = "";
    public string PatientPrenom { get; set; } = "";
    public DateTime DateHeure { get; set; }
    public string? Motif { get; set; }
    public string? Statut { get; set; }
    public bool IsPremiereConsultation { get; set; }
    public int SpecialiteId { get; set; }
    
    /// <summary>Ã‰tape actuelle de la consultation (pour reprise aprÃ¨s pause)</summary>
    public string? EtapeActuelle { get; set; }
    
    // DonnÃ©es de la consultation (workflow mis Ã  jour)
    public AnamneseDto? Anamnese { get; set; }
    public ExamenCliniqueDto? ExamenClinique { get; set; }
    public ExamenGynecologiqueDto? ExamenGynecologique { get; set; }
    public ExamenChirurgicalDto? ExamenChirurgical { get; set; }
    public ExamenAnesthesiqueDto? ExamenAnesthesique { get; set; }
    public DiagnosticDto? Diagnostic { get; set; }
    public PlanTraitementDto? PlanTraitement { get; set; }
    public ConclusionDto? Conclusion { get; set; }
    
    // ConservÃ© pour compatibilitÃ©
    public PrescriptionsDto? Prescriptions { get; set; }
}

// Ã‰tape 1: AnamnÃ¨se
public class AnamneseDto
{
    public string? MotifConsultation { get; set; }
    public string? HistoireMaladie { get; set; }
    public string? AntecedentsPersonnels { get; set; }
    public string? AntecedentsFamiliaux { get; set; }
    public string? AllergiesConnues { get; set; }
    public string? TraitementsEnCours { get; set; }
    public string? HabitudesVie { get; set; }
    public List<QuestionReponseDto> QuestionsReponses { get; set; } = new();
    
    // ParamÃ¨tres vitaux
    public ParametresVitauxDto? ParametresVitaux { get; set; }
}

public class QuestionReponseDto
{
    public string QuestionId { get; set; } = "";
    public string Question { get; set; } = "";
    public string Reponse { get; set; } = "";
}

public class ParametresVitauxDto
{
    public decimal? Poids { get; set; }
    public decimal? Taille { get; set; }
    public decimal? Temperature { get; set; }
    public string? TensionArterielle { get; set; }
    public int? FrequenceCardiaque { get; set; }
    public int? FrequenceRespiratoire { get; set; }
    public decimal? SaturationOxygene { get; set; }
    public decimal? Glycemie { get; set; }
}

// Ã‰tape 2: Examen Clinique (NOUVEAU)
public class ExamenCliniqueDto
{
    // Constantes vitales (affichÃ©es si prises par infirmier, sinon saisies par mÃ©decin)
    public ParametresVitauxDto? ParametresVitaux { get; set; }
    [JsonRequired]
    public bool ParametresPrisParInfirmier { get; set; }
    public string? InfirmierNom { get; set; }
    public DateTime? DatePriseParametres { get; set; }
    
    // Examen physique
    public string? Inspection { get; set; }
    public string? Palpation { get; set; }
    public string? Auscultation { get; set; }
    public string? Percussion { get; set; }
    public string? AutresObservations { get; set; }
}

// Ã‰tape 3: Diagnostic et Orientation
public class DiagnosticDto
{
    public string? ExamenClinique { get; set; }
    public string? DiagnosticPrincipal { get; set; }
    public string? DiagnosticsSecondaires { get; set; }
    public string? HypothesesDiagnostiques { get; set; }
    public string? NotesCliniques { get; set; }
    
    // RÃ©capitulatif patient (donnÃ©es du compte)
    public RecapitulatifPatientDto? RecapitulatifPatient { get; set; }
}

// RÃ©capitulatif des donnÃ©es patient pour l'Ã©tape diagnostic
public class RecapitulatifPatientDto
{
    // Informations personnelles
    public string? RegionOrigine { get; set; }
    public string? SituationMatrimoniale { get; set; }
    public string? Profession { get; set; }
    public int? NbEnfants { get; set; }
    public string? Ethnie { get; set; }
    // Informations mÃ©dicales
    public string? GroupeSanguin { get; set; }
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
    // Diagnostics prÃ©cÃ©dents
    public List<DiagnosticPrecedentDto> DiagnosticsPrecedents { get; set; } = new();
}

// Diagnostic prÃ©cÃ©dent pour l'historique
public class DiagnosticPrecedentDto
{
    public DateTime Date { get; set; }
    public string Diagnostic { get; set; } = string.Empty;
    public string MedecinNom { get; set; } = string.Empty;
    public string? MedecinPrenom { get; set; }
    public string? Specialite { get; set; }
}

// Ã‰tape 4: Plan de Traitement (NOUVEAU)
public class PlanTraitementDto
{
    public string? ExplicationDiagnostic { get; set; }
    public string? OptionsTraitement { get; set; }
    public OrdonnanceDto? Ordonnance { get; set; }
    public List<ExamenPrescritDto> ExamensPrescrits { get; set; } = new();
    // Orientation spÃ©cialiste
    public string? OrientationSpecialiste { get; set; }
    public string? MotifOrientation { get; set; }
    public int? IdSpecialisteOriente { get; set; }
    // DÃ©cision chirurgicale (uniquement pour les chirurgiens)
    public string? DecisionChirurgicale { get; set; }
}

// Ã‰tape 5: Conclusion (NOUVEAU)
public class ConclusionDto
{
    public string? ResumeConsultation { get; set; }
    public string? QuestionsPatient { get; set; }
    public string? ConsignesPatient { get; set; }
    public string? Recommandations { get; set; }
    // Planification suivi
    public string? TypeSuivi { get; set; } // rdv, appel, aucun
    public DateTime? DateSuiviPrevue { get; set; }
    public string? NotesSuivi { get; set; }
}

// Ã‰tape 3: Prescriptions
public class PrescriptionsDto
{
    public OrdonnanceDto? Ordonnance { get; set; }
    public List<ExamenPrescritDto> Examens { get; set; } = new();
    public List<OrientationPreConsultationDto> Orientations { get; set; } = new();
}

public class OrdonnanceDto
{
    public int? IdOrdonnance { get; set; }
    public string? Notes { get; set; }
    public string? DureeTraitement { get; set; }
    public List<MedicamentDto> Medicaments { get; set; } = new();
}

public class MedicamentDto
{
    public int? IdPrescription { get; set; }
    public int? IdMedicament { get; set; }
    public string NomMedicament { get; set; } = "";
    public string? Dosage { get; set; }
    public string? Posologie { get; set; }
    public string? Frequence { get; set; }
    public string? Duree { get; set; }
    public string? VoieAdministration { get; set; }
    public string? FormePharmaceutique { get; set; }
    public string? Instructions { get; set; }
    public int? Quantite { get; set; }
}

public class ExamenPrescritDto
{
    public int? IdExamen { get; set; }
    public string Categorie { get; set; } = "";
    public string Specialite { get; set; } = "";
    public string NomExamen { get; set; } = "";
    public string? Description { get; set; }
    public bool Urgence { get; set; }
    public string? Notes { get; set; }
    public bool Disponible { get; set; } = true;
    public int? IdLaboratoire { get; set; }
    public string? NomLaboratoire { get; set; }
}

public class LaboratoireDto
{
    public int IdLabo { get; set; }
    public string NomLabo { get; set; } = "";
    public string? Contact { get; set; }
    public string? Adresse { get; set; }
    public string? Telephone { get; set; }
    public string? Type { get; set; }
}

// ==================== ORIENTATION PRE-CONSULTATION (UNIFIÃ‰) ====================

/// <summary>
/// DTO pour afficher une orientation (lecture)
/// </summary>
public class OrientationPreConsultationDto
{
    public int IdOrientation { get; set; }
    public int IdConsultation { get; set; }
    public int IdPatient { get; set; }
    
    /// <summary>Type: medecin_interne, medecin_externe, hopital, service_interne, laboratoire</summary>
    public string TypeOrientation { get; set; } = "medecin_interne";
    
    // Destination
    public int? IdSpecialite { get; set; }
    public string? NomSpecialite { get; set; }
    public int? IdMedecinOriente { get; set; }
    public string? NomMedecinOriente { get; set; }
    public string? NomDestinataire { get; set; }
    public string? SpecialiteTexte { get; set; }
    public string? AdresseDestinataire { get; set; }
    public string? TelephoneDestinataire { get; set; }
    
    // DÃ©tails
    public string Motif { get; set; } = "";
    public string? Notes { get; set; }
    public bool Urgence { get; set; }
    public bool Prioritaire { get; set; }
    
    // Suivi
    public string Statut { get; set; } = "en_attente";
    public DateTime DateOrientation { get; set; }
    public DateTime? DateRdvPropose { get; set; }
    public int? IdRdvCree { get; set; }
    
    // MÃ©tadonnÃ©es
    public string? MedecinPrescripteur { get; set; }
    public DateTime? CreatedAt { get; set; }
}

/// <summary>
/// RequÃªte pour crÃ©er une orientation
/// </summary>
public class CreateOrientationRequest
{
    /// <summary>Type: medecin_interne, medecin_externe, hopital, service_interne, laboratoire</summary>
    public string TypeOrientation { get; set; } = "medecin_interne";
    
    // Pour mÃ©decin interne
    public int? IdSpecialite { get; set; }
    public int? IdMedecinOriente { get; set; }
    
    // Pour mÃ©decin externe / hÃ´pital
    public string? NomDestinataire { get; set; }
    public string? SpecialiteTexte { get; set; }
    public string? AdresseDestinataire { get; set; }
    public string? TelephoneDestinataire { get; set; }
    
    // DÃ©tails (obligatoires)
    public string Motif { get; set; } = "";
    public string? Notes { get; set; }
    [JsonRequired]
    public bool Urgence { get; set; }
    [JsonRequired]
    public bool Prioritaire { get; set; }
    
    public DateTime? DateRdvPropose { get; set; }
}

/// <summary>
/// RequÃªte pour mettre Ã  jour le statut d'une orientation
/// </summary>
public class UpdateOrientationStatutRequest
{
    public string Statut { get; set; } = "en_attente";
    public DateTime? DateRdvPropose { get; set; }
    public int? IdRdvCree { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// RequÃªte pour crÃ©er un RDV liÃ© Ã  une orientation
/// </summary>
public class CreerRdvOrientationRequest
{
    public string DateHeure { get; set; } = "";
    public int? Duree { get; set; }
    public string? Motif { get; set; }
    public string? Notes { get; set; }
}

public class SpecialiteDto
{
    public int IdSpecialite { get; set; }
    public string NomSpecialite { get; set; } = "";
}

public class MedecinSpecialisteDto
{
    public int IdUser { get; set; }
    public string Nom { get; set; } = "";
    public string Prenom { get; set; } = "";
    public string NomComplet => $"Dr. {Prenom} {Nom}";
    public int IdSpecialite { get; set; }
    public string? NomSpecialite { get; set; }
}

// ==================== REQUESTS ====================

public class SaveAnamneseRequest
{
    public int IdConsultation { get; set; }
    public AnamneseDto Anamnese { get; set; } = new();
}

public class SaveExamenCliniqueRequest
{
    public int IdConsultation { get; set; }
    public ExamenCliniqueDto ExamenClinique { get; set; } = new();
}

public class SaveExamenGynecologiqueRequest
{
    public int IdConsultation { get; set; }
    public ExamenGynecologiqueDto ExamenGynecologique { get; set; } = new();
}

public class SaveExamenChirurgicalRequest
{
    public int IdConsultation { get; set; }
    public ExamenChirurgicalDto ExamenChirurgical { get; set; } = new();
}

public class SaveDiagnosticRequest
{
    public int IdConsultation { get; set; }
    public DiagnosticDto Diagnostic { get; set; } = new();
}

public class SavePlanTraitementRequest
{
    public int IdConsultation { get; set; }
    public PlanTraitementDto PlanTraitement { get; set; } = new();
}

public class SaveConclusionRequest
{
    public int IdConsultation { get; set; }
    public ConclusionDto Conclusion { get; set; } = new();
}

public class SavePrescriptionsRequest
{
    public int IdConsultation { get; set; }
    public PrescriptionsDto Prescriptions { get; set; } = new();
}

public class ValiderConsultationRequest
{
    [JsonRequired]
    public int IdConsultation { get; set; }
    public string? Conclusion { get; set; }
    [JsonRequired]
    public bool Imprimer { get; set; }
}

public class ConsultationRecapitulatifDto
{
    public ConsultationEnCoursDto Consultation { get; set; } = new();
    public DossierPatientDto Patient { get; set; } = new();
}

// ==================== CONSULTATION DETAILS (pour affichage) ====================

public class ConsultationDetailDto
{
    public int IdConsultation { get; set; }
    public int IdPatient { get; set; }
    public string PatientNom { get; set; } = "";
    public string PatientPrenom { get; set; } = "";
    public string? NumeroDossier { get; set; }
    public string DateConsultation { get; set; } = "";
    public int? Duree { get; set; }
    public string? Motif { get; set; }
    public string Statut { get; set; } = "a_faire";
    public string? Anamnese { get; set; }
    public string? NotesCliniques { get; set; }
    public string? Diagnostic { get; set; }
    public string? Conclusion { get; set; }
    public string? Recommandations { get; set; }
    public OrdonnanceDto? Ordonnance { get; set; }
    public List<ExamenPrescritDetailDto> ExamensPrescrits { get; set; } = new();
    public List<QuestionReponseDto> Questionnaire { get; set; } = new();
    public ParametresVitauxDto? ParametresVitaux { get; set; }
    public ExamenCliniqueDto? ExamenClinique { get; set; }
    public ExamenGynecologiqueDto? ExamenGynecologique { get; set; }
    public ExamenChirurgicalDto? ExamenChirurgical { get; set; }
    public ExamenAnesthesiqueDto? ExamenAnesthesique { get; set; }
    public PlanTraitementDto? PlanTraitement { get; set; }
    public ConclusionDto? ConclusionDetaillee { get; set; }
    /// <summary>
    /// RDV de suivi crÃ©Ã© aprÃ¨s cette consultation
    /// </summary>
    public RdvSuiviDetailDto? RdvSuivi { get; set; }
}

/// <summary>
/// DÃ©tails d'un RDV de suivi crÃ©Ã© aprÃ¨s une consultation
/// </summary>
public class RdvSuiviDetailDto
{
    public int IdRendezVous { get; set; }
    public DateTime DateHeure { get; set; }
    public string? Motif { get; set; }
    public string Statut { get; set; } = "";
    public string? MedecinNom { get; set; }
    public string? ServiceNom { get; set; }
}

public class ExamenPrescritDetailDto
{
    public int? IdExamen { get; set; }
    public string NomExamen { get; set; } = "";
    public string Categorie { get; set; } = "";
    public string Specialite { get; set; } = "";
    public string? Instructions { get; set; }
    public string? Statut { get; set; }
    public bool Disponible { get; set; } = true;
}

// ==================== REQUÃŠTES D'ACTION ====================

/// <summary>
/// RequÃªte pour annuler une consultation
/// </summary>
public class AnnulerConsultationRequest
{
    /// <summary>Motif d'annulation (obligatoire)</summary>
    public string Motif { get; set; } = "";
}
