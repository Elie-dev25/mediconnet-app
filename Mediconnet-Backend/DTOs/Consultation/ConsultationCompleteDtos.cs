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
    
    // Informations médicales
    public string? GroupeSanguin { get; set; }
    public string? MaladiesChroniques { get; set; }
    public string? AllergiesDetails { get; set; }
    public string? AntecedentsFamiliauxDetails { get; set; }
    public string? OperationsDetails { get; set; }
    
    // Habitudes de vie
    public bool? ConsommationAlcool { get; set; }
    public bool? Tabagisme { get; set; }
    public bool? ActivitePhysique { get; set; }
    
    // Assurance
    public string? NomAssurance { get; set; }
    public string? NumeroCarteAssurance { get; set; }
    public decimal? CouvertureAssurance { get; set; }
    
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
    public string TypeExamen { get; set; } = "";
    public string NomExamen { get; set; } = "";
    public string Statut { get; set; } = "";
    public DateTime DatePrescription { get; set; }
    public DateTime? DateRealisation { get; set; }
    public string? Resultats { get; set; }
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
    
    // Données de la consultation
    public AnamneseDto? Anamnese { get; set; }
    public DiagnosticDto? Diagnostic { get; set; }
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

// Étape 2: Diagnostic
public class DiagnosticDto
{
    public string? ExamenClinique { get; set; }
    public string? DiagnosticPrincipal { get; set; }
    public string? DiagnosticsSecondaires { get; set; }
    public string? NotesCliniques { get; set; }
}

// Étape 3: Prescriptions
public class PrescriptionsDto
{
    public OrdonnanceDto? Ordonnance { get; set; }
    public List<ExamenPrescritDto> Examens { get; set; } = new();
    public List<RecommandationDto> Recommandations { get; set; } = new();
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
    public string NomMedicament { get; set; } = "";
    public string? Dosage { get; set; }
    public string? Frequence { get; set; }
    public string? Duree { get; set; }
    public string? Instructions { get; set; }
    public int? Quantite { get; set; }
}

public class ExamenPrescritDto
{
    public int? IdExamen { get; set; }
    public string TypeExamen { get; set; } = "";
    public string NomExamen { get; set; } = "";
    public string? Description { get; set; }
    public bool Urgence { get; set; }
    public string? Notes { get; set; }
}

public class RecommandationDto
{
    public int? IdRecommandation { get; set; }
    public string Type { get; set; } = "conseil";
    public string? SpecialiteOrientee { get; set; }
    public int? IdMedecinOriente { get; set; }
    public string? Motif { get; set; }
    public string? Description { get; set; }
    public bool Urgence { get; set; }
}

// ==================== REQUESTS ====================

public class SaveAnamneseRequest
{
    public int IdConsultation { get; set; }
    public AnamneseDto Anamnese { get; set; } = new();
}

public class SaveDiagnosticRequest
{
    public int IdConsultation { get; set; }
    public DiagnosticDto Diagnostic { get; set; } = new();
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
