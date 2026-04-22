using System.Text.Json.Serialization;
namespace Mediconnet_Backend.DTOs.Consultation;

/// <summary>
/// DTO pour afficher une programmation d'intervention
/// </summary>
public class ProgrammationInterventionDto
{
    public int IdProgrammation { get; set; }
    public int IdConsultation { get; set; }
    public int IdPatient { get; set; }
    public int IdChirurgien { get; set; }
    
    // Infos patient
    public string? PatientNom { get; set; }
    public string? PatientPrenom { get; set; }
    
    // Infos chirurgien
    public string? ChirurgienNom { get; set; }
    public string? ChirurgienPrenom { get; set; }
    public string? Specialite { get; set; }
    
    // DÃ©tails intervention
    public string TypeIntervention { get; set; } = "programmee";
    public string? ClassificationAsa { get; set; }
    public string? RisqueOperatoire { get; set; }
    public bool ConsentementEclaire { get; set; }
    public DateTime? DateConsentement { get; set; }
    public string? IndicationOperatoire { get; set; }
    public string? TechniquePrevue { get; set; }
    public DateTime? DatePrevue { get; set; }
    public string? HeureDebut { get; set; }
    public int? DureeEstimee { get; set; }
    public string? NotesAnesthesie { get; set; }
    public string? BilanPreoperatoire { get; set; }
    public string? InstructionsPatient { get; set; }
    public string Statut { get; set; } = "en_attente";
    public string? MotifAnnulation { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO pour crÃ©er une programmation d'intervention
/// </summary>
public class CreateProgrammationInterventionRequest
{
    [JsonRequired]
    public int IdConsultation { get; set; }
    public string TypeIntervention { get; set; } = "programmee";
    public string? ClassificationAsa { get; set; }
    public string? RisqueOperatoire { get; set; }
    [JsonRequired]
    public bool ConsentementEclaire { get; set; }
    public DateTime? DateConsentement { get; set; }
    public string? IndicationOperatoire { get; set; }
    public string? TechniquePrevue { get; set; }
    public DateTime? DatePrevue { get; set; }
    public string? HeureDebut { get; set; }
    public int? DureeEstimee { get; set; }
    public string? NotesAnesthesie { get; set; }
    public string? BilanPreoperatoire { get; set; }
    public string? InstructionsPatient { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO pour mettre Ã  jour une programmation d'intervention
/// </summary>
public class UpdateProgrammationInterventionRequest
{
    public string? TypeIntervention { get; set; }
    public string? ClassificationAsa { get; set; }
    public string? RisqueOperatoire { get; set; }
    public bool? ConsentementEclaire { get; set; }
    public DateTime? DateConsentement { get; set; }
    public string? IndicationOperatoire { get; set; }
    public string? TechniquePrevue { get; set; }
    public DateTime? DatePrevue { get; set; }
    public string? HeureDebut { get; set; }
    public int? DureeEstimee { get; set; }
    public string? NotesAnesthesie { get; set; }
    public string? BilanPreoperatoire { get; set; }
    public string? InstructionsPatient { get; set; }
    public string? Statut { get; set; }
    public string? MotifAnnulation { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO simplifiÃ© pour les listes
/// </summary>
public class ProgrammationInterventionListDto
{
    public int IdProgrammation { get; set; }
    public string? PatientNom { get; set; }
    public string? PatientPrenom { get; set; }
    public string? IndicationOperatoire { get; set; }
    public string? TechniquePrevue { get; set; }
    public DateTime? DatePrevue { get; set; }
    public string? HeureDebut { get; set; }
    public int? DureeEstimee { get; set; }
    public string Statut { get; set; } = "en_attente";
    public string TypeIntervention { get; set; } = "programmee";
    public string? ClassificationAsa { get; set; }
    public bool ConsentementEclaire { get; set; }
}
