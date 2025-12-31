namespace Mediconnet_Backend.DTOs.Medecin;

/// <summary>
/// DTO pour un patient du médecin (liste)
/// </summary>
public class MedecinPatientDto
{
    public int IdPatient { get; set; }
    public int IdUser { get; set; }
    public string Nom { get; set; } = "";
    public string Prenom { get; set; } = "";
    public string? NumeroDossier { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? Sexe { get; set; }
    public int? Age { get; set; }
    public DateTime? DerniereVisite { get; set; }
    public DateTime? ProchaineVisite { get; set; }
    public int NombreConsultations { get; set; }
    public string? GroupeSanguin { get; set; }
}

/// <summary>
/// DTO pour le détail d'un patient
/// </summary>
public class MedecinPatientDetailDto
{
    public int IdPatient { get; set; }
    public int IdUser { get; set; }
    public string Nom { get; set; } = "";
    public string Prenom { get; set; } = "";
    public string? NumeroDossier { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? Sexe { get; set; }
    public DateTime? Naissance { get; set; }
    public string? Adresse { get; set; }
    public string? GroupeSanguin { get; set; }
    public string? Ethnie { get; set; }
    public string? PersonneContact { get; set; }
    public string? NumeroContact { get; set; }
    public string? Profession { get; set; }
    
    // Historique
    public List<ConsultationHistoriqueDto> DernieresConsultations { get; set; } = new();
    public List<RendezVousHistoriqueDto> ProchainsRdv { get; set; } = new();
}

/// <summary>
/// DTO pour l'historique des consultations d'un patient
/// </summary>
public class ConsultationHistoriqueDto
{
    public int IdConsultation { get; set; }
    public DateTime DateConsultation { get; set; }
    public string Motif { get; set; } = "";
    public string? Diagnostic { get; set; }
}

/// <summary>
/// DTO pour les prochains RDV d'un patient
/// </summary>
public class RendezVousHistoriqueDto
{
    public int IdRendezVous { get; set; }
    public DateTime DateHeure { get; set; }
    public string Motif { get; set; } = "";
    public string Statut { get; set; } = "";
}

/// <summary>
/// Statistiques des patients du médecin
/// </summary>
public class MedecinPatientStatsDto
{
    public int TotalPatients { get; set; }
    public int NouveauxCeMois { get; set; }
    public int AvecRdvPlanifie { get; set; }
}
