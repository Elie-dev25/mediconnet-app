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
    
    // Informations médicales
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
    
    // Dates système
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
    
    /// <summary>Étape actuelle de la consultation (pour reprise après pause)</summary>
    public string? EtapeActuelle { get; set; }
    
    // Données de la consultation (workflow mis à jour)
    public AnamneseDto? Anamnese { get; set; }
    public ExamenCliniqueDto? ExamenClinique { get; set; }
    public ExamenGynecologiqueDto? ExamenGynecologique { get; set; }
    public DiagnosticDto? Diagnostic { get; set; }
    public PlanTraitementDto? PlanTraitement { get; set; }
    public ConclusionDto? Conclusion { get; set; }
    
    // Conservé pour compatibilité
    public PrescriptionsDto? Prescriptions { get; set; }
}

// Étape 1: Anamnèse
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
    
    // Paramètres vitaux
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

// Étape 2: Examen Clinique (NOUVEAU)
public class ExamenCliniqueDto
{
    // Constantes vitales (affichées si prises par infirmier, sinon saisies par médecin)
    public ParametresVitauxDto? ParametresVitaux { get; set; }
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

// Étape 3: Diagnostic et Orientation
public class DiagnosticDto
{
    public string? ExamenClinique { get; set; }
    public string? DiagnosticPrincipal { get; set; }
    public string? DiagnosticsSecondaires { get; set; }
    public string? HypothesesDiagnostiques { get; set; }
    public string? NotesCliniques { get; set; }
    
    // Récapitulatif patient (données du compte)
    public RecapitulatifPatientDto? RecapitulatifPatient { get; set; }
}

// Récapitulatif des données patient pour l'étape diagnostic
public class RecapitulatifPatientDto
{
    // Informations personnelles
    public string? RegionOrigine { get; set; }
    public string? SituationMatrimoniale { get; set; }
    public string? Profession { get; set; }
    public int? NbEnfants { get; set; }
    public string? Ethnie { get; set; }
    // Informations médicales
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
    // Diagnostics précédents
    public List<DiagnosticPrecedentDto> DiagnosticsPrecedents { get; set; } = new();
}

// Diagnostic précédent pour l'historique
public class DiagnosticPrecedentDto
{
    public DateTime Date { get; set; }
    public string Diagnostic { get; set; } = string.Empty;
    public string MedecinNom { get; set; } = string.Empty;
    public string? MedecinPrenom { get; set; }
    public string? Specialite { get; set; }
}

// Étape 4: Plan de Traitement (NOUVEAU)
public class PlanTraitementDto
{
    public string? ExplicationDiagnostic { get; set; }
    public string? OptionsTraitement { get; set; }
    public OrdonnanceDto? Ordonnance { get; set; }
    public List<ExamenPrescritDto> ExamensPrescrits { get; set; } = new();
    // Orientation spécialiste
    public string? OrientationSpecialiste { get; set; }
    public string? MotifOrientation { get; set; }
    public int? IdSpecialisteOriente { get; set; }
}

// Étape 5: Conclusion (NOUVEAU)
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

// Étape 3: Prescriptions
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

// ==================== ORIENTATION PRE-CONSULTATION (UNIFIÉ) ====================

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
    
    // Détails
    public string Motif { get; set; } = "";
    public string? Notes { get; set; }
    public bool Urgence { get; set; }
    public bool Prioritaire { get; set; }
    
    // Suivi
    public string Statut { get; set; } = "en_attente";
    public DateTime DateOrientation { get; set; }
    public DateTime? DateRdvPropose { get; set; }
    public int? IdRdvCree { get; set; }
    
    // Métadonnées
    public string? MedecinPrescripteur { get; set; }
    public DateTime? CreatedAt { get; set; }
}

/// <summary>
/// Requête pour créer une orientation
/// </summary>
public class CreateOrientationRequest
{
    /// <summary>Type: medecin_interne, medecin_externe, hopital, service_interne, laboratoire</summary>
    public string TypeOrientation { get; set; } = "medecin_interne";
    
    // Pour médecin interne
    public int? IdSpecialite { get; set; }
    public int? IdMedecinOriente { get; set; }
    
    // Pour médecin externe / hôpital
    public string? NomDestinataire { get; set; }
    public string? SpecialiteTexte { get; set; }
    public string? AdresseDestinataire { get; set; }
    public string? TelephoneDestinataire { get; set; }
    
    // Détails (obligatoires)
    public string Motif { get; set; } = "";
    public string? Notes { get; set; }
    public bool Urgence { get; set; }
    public bool Prioritaire { get; set; }
    
    public DateTime? DateRdvPropose { get; set; }
}

/// <summary>
/// Requête pour mettre à jour le statut d'une orientation
/// </summary>
public class UpdateOrientationStatutRequest
{
    public string Statut { get; set; } = "en_attente";
    public DateTime? DateRdvPropose { get; set; }
    public int? IdRdvCree { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Requête pour créer un RDV lié à une orientation
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
    public decimal CoutConsultation { get; set; }
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
    public int IdConsultation { get; set; }
    public string? Conclusion { get; set; }
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
    public PlanTraitementDto? PlanTraitement { get; set; }
    public ConclusionDto? ConclusionDetaillee { get; set; }
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

// ==================== REQUÊTES D'ACTION ====================

/// <summary>
/// Requête pour annuler une consultation
/// </summary>
public class AnnulerConsultationRequest
{
    /// <summary>Motif d'annulation (obligatoire)</summary>
    public string Motif { get; set; } = "";
}
