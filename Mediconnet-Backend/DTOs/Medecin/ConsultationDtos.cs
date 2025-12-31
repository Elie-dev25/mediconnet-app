namespace Mediconnet_Backend.DTOs.Medecin;

/// <summary>
/// DTO pour une consultation
/// </summary>
public class ConsultationDto
{
    public int IdConsultation { get; set; }
    public int IdRendezVous { get; set; }
    public int IdPatient { get; set; }
    public string PatientNom { get; set; } = "";
    public string PatientPrenom { get; set; } = "";
    public string? NumeroDossier { get; set; }
    public DateTime DateConsultation { get; set; }
    public string Motif { get; set; } = "";
    public string? Diagnostic { get; set; }
    public string? Notes { get; set; }
    public string Statut { get; set; } = ""; // en_cours, terminee
    public int Duree { get; set; }
    public bool HasOrdonnance { get; set; }
    public bool HasExamens { get; set; }
    public bool IsPremiereConsultation { get; set; }
    public int SpecialiteId { get; set; }
}

/// <summary>
/// DTO pour créer/terminer une consultation
/// </summary>
public class CreateConsultationRequest
{
    public int IdRendezVous { get; set; }
    public string Motif { get; set; } = "";
    public string? Notes { get; set; }
}

/// <summary>
/// DTO pour terminer une consultation avec diagnostic
/// </summary>
public class TerminerConsultationRequest
{
    public string? Diagnostic { get; set; }
    public string? Notes { get; set; }
    public string? Recommandations { get; set; }
}

/// <summary>
/// DTO pour les statistiques de consultations
/// </summary>
public class ConsultationStatsDto
{
    public int TotalConsultations { get; set; }
    public int ConsultationsAujourdHui { get; set; }
    public int ConsultationsSemaine { get; set; }
    public int ConsultationsMois { get; set; }
    public int EnAttente { get; set; }
    public int Terminees { get; set; }
}
