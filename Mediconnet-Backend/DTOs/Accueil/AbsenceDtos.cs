namespace Mediconnet_Backend.DTOs.Accueil;

/// <summary>
/// DTO pour marquer un patient comme absent
/// </summary>
public class MarquerAbsentRequest
{
    /// <summary>
    /// Motif de l'absence (optionnel)
    /// </summary>
    public string? Motif { get; set; }
}

/// <summary>
/// DTO pour marquer plusieurs RDV comme absents en lot
/// </summary>
public class MarquerAbsentsLotRequest
{
    /// <summary>
    /// Liste des IDs de rendez-vous à marquer comme absents
    /// </summary>
    public List<int> IdsRdv { get; set; } = new();
}

/// <summary>
/// DTO pour la réponse de marquage d'absence
/// </summary>
public class MarquerAbsentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RdvId { get; set; }
    public string PatientNom { get; set; } = string.Empty;
    public string AncienStatut { get; set; } = string.Empty;
    public string NouveauStatut { get; set; } = "absent";
}

/// <summary>
/// DTO pour les statistiques d'absences
/// </summary>
public class StatsAbsencesDto
{
    public int TotalAbsences { get; set; }
    public int TotalRdv { get; set; }
    public double TauxAbsence { get; set; }
    public List<AbsenceParServiceDto> ParService { get; set; } = new();
    public List<AbsenceParJourDto> ParJour { get; set; } = new();
    public List<PatientAbsencesFrequentesDto> PatientsFrequents { get; set; } = new();
}

public class AbsenceParServiceDto
{
    public string Service { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class AbsenceParJourDto
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class PatientAbsencesFrequentesDto
{
    public int PatientId { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public int Absences { get; set; }
}
