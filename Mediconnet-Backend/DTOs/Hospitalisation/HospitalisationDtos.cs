namespace Mediconnet_Backend.DTOs.Hospitalisation;

/// <summary>
/// DTO pour une chambre
/// </summary>
public class ChambreDto
{
    public int IdChambre { get; set; }
    public string? Numero { get; set; }
    public int? Capacite { get; set; }
    public string? Etat { get; set; }
    public string? Statut { get; set; }
    public int LitsDisponibles { get; set; }
    public int LitsOccupes { get; set; }
    public List<LitDto>? Lits { get; set; }
}

/// <summary>
/// DTO pour un lit
/// </summary>
public class LitDto
{
    public int IdLit { get; set; }
    public string? Numero { get; set; }
    public string? Statut { get; set; }
    public int IdChambre { get; set; }
    public string? NumeroChambre { get; set; }
    public bool EstDisponible { get; set; }
}

/// <summary>
/// DTO pour une hospitalisation
/// </summary>
public class HospitalisationDto
{
    public int IdAdmission { get; set; }
    public DateTime DateEntree { get; set; }
    public DateTime? DateSortie { get; set; }
    public string? Motif { get; set; }
    public string? Statut { get; set; }
    public int IdPatient { get; set; }
    public string? PatientNom { get; set; }
    public string? PatientPrenom { get; set; }
    public string? PatientNumeroDossier { get; set; }
    public int IdLit { get; set; }
    public string? NumeroLit { get; set; }
    public string? NumeroChambre { get; set; }
    public int? DureeJours { get; set; }
}

/// <summary>
/// Requête pour créer une demande d'hospitalisation
/// </summary>
public class CreerHospitalisationRequest
{
    public int IdPatient { get; set; }
    public int IdLit { get; set; }
    public string? Motif { get; set; }
    public DateTime? DateEntreePrevue { get; set; }
    public int? IdConsultation { get; set; }
}

/// <summary>
/// Requête pour créer une hospitalisation depuis une consultation
/// </summary>
public class DemandeHospitalisationRequest
{
    public int IdConsultation { get; set; }
    public int IdPatient { get; set; }
    public string Motif { get; set; } = string.Empty;
    public string? Urgence { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Réponse après création d'une hospitalisation
/// </summary>
public class HospitalisationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? IdAdmission { get; set; }
    public HospitalisationDto? Hospitalisation { get; set; }
}

/// <summary>
/// Requête pour terminer une hospitalisation
/// </summary>
public class TerminerHospitalisationRequest
{
    public int IdAdmission { get; set; }
    public DateTime? DateSortie { get; set; }
    public string? NotesDepart { get; set; }
}

/// <summary>
/// Filtre pour rechercher des hospitalisations
/// </summary>
public class FiltreHospitalisationRequest
{
    public string? Statut { get; set; }
    public int? IdPatient { get; set; }
    public DateTime? DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
}

/// <summary>
/// Liste des lits disponibles
/// </summary>
public class LitsDisponiblesResponse
{
    public bool Success { get; set; } = true;
    public List<LitDto> Lits { get; set; } = new();
    public int TotalDisponibles { get; set; }
}

/// <summary>
/// Liste des chambres avec leurs lits
/// </summary>
public class ChambresResponse
{
    public bool Success { get; set; } = true;
    public List<ChambreDto> Chambres { get; set; } = new();
    public int TotalChambres { get; set; }
    public int TotalLits { get; set; }
    public int LitsDisponibles { get; set; }
}
