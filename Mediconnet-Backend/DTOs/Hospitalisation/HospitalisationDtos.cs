using System.Text.Json.Serialization;

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
    public DateTime? DateSortiePrevue { get; set; }
    public DateTime? DateSortie { get; set; }
    public string? Motif { get; set; }
    public string? MotifSortie { get; set; }
    public string? ResumeMedical { get; set; }
    public string? DiagnosticPrincipal { get; set; }
    public string? Statut { get; set; }
    public string? Urgence { get; set; }
    public int IdPatient { get; set; }
    public string? PatientNom { get; set; }
    public string? PatientPrenom { get; set; }
    public string? PatientNumeroDossier { get; set; }
    public int IdLit { get; set; }
    public string? NumeroLit { get; set; }
    public string? NumeroChambre { get; set; }
    public int? IdService { get; set; }
    public string? ServiceNom { get; set; }
    public int? IdMedecin { get; set; }
    public string? MedecinNom { get; set; }
    public int? IdConsultation { get; set; }
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
    public DateTime? DateSortiePrevue { get; set; }
    public int? IdConsultation { get; set; }
    public int? IdMedecin { get; set; }
}

/// <summary>
/// Requête pour ordonner une hospitalisation depuis une consultation (médecin)
/// Le médecin ne choisit PAS la chambre/lit - c'est le Major qui l'attribue
/// </summary>
public class OrdonnerHospitalisationRequest
{
    public int IdConsultation { get; set; }
    public int IdPatient { get; set; }
    public string Motif { get; set; } = string.Empty;
    public string? Urgence { get; set; }
    public string? DiagnosticPrincipal { get; set; }
    public List<SoinComplementaireDto>? Soins { get; set; }
    public DateTime? DateSortiePrevue { get; set; }
    /// <summary>
    /// Service cible pour l'hospitalisation (optionnel, par défaut = service du médecin)
    /// </summary>
    public int? IdServiceCible { get; set; }
}

/// <summary>
/// Requête pour attribuer un lit à une hospitalisation en attente (Major)
/// </summary>
public class AttribuerLitRequest
{
    public int IdAdmission { get; set; }
    public int IdLit { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO pour un soin complémentaire
/// </summary>
public class SoinComplementaireDto
{
    public string TypeSoin { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Frequence { get; set; }
    public string? Duree { get; set; }
    public string Priorite { get; set; } = "normale";
    public string? Instructions { get; set; }
}

/// <summary>
/// DTO pour une prescription d'examen dans le contexte hospitalisation
/// </summary>
public class ExamenPrescriptionDto
{
    public string TypeExamen { get; set; } = string.Empty;
    public string NomExamen { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Urgence { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO pour une prescription de médicament dans le contexte hospitalisation
/// </summary>
public class MedicamentPrescriptionDto
{
    public string NomMedicament { get; set; } = string.Empty;
    public string? Dosage { get; set; }
    public string? Posologie { get; set; }
    public string? FormePharmaceutique { get; set; }
    public string? VoieAdministration { get; set; }
    public string? DureeTraitement { get; set; }
    public string? Instructions { get; set; }
    public int? Quantite { get; set; }
}

/// <summary>
/// Requête complète pour ordonner une hospitalisation avec prescriptions (workflow multi-étapes)
/// </summary>
public class OrdonnerHospitalisationCompleteRequest
{
    public int IdPatient { get; set; }
    public int? IdConsultation { get; set; }
    public string Motif { get; set; } = string.Empty;
    public string Urgence { get; set; } = "normale";
    public string? DiagnosticPrincipal { get; set; }
    [JsonPropertyName("soinsComplementaires")]
    public List<SoinComplementaireDto>? Soins { get; set; }
    public DateTime? DateSortiePrevue { get; set; }
    public List<ExamenPrescriptionDto>? Examens { get; set; }
    public List<MedicamentPrescriptionDto>? Medicaments { get; set; }
    /// <summary>
    /// Service cible pour l'hospitalisation (optionnel, par défaut = service du médecin)
    /// </summary>
    public int? IdServiceCible { get; set; }
}

/// <summary>
/// DEPRECATED: Ancienne requête avec sélection du lit par le médecin
/// Conservée pour rétrocompatibilité
/// </summary>
public class DemandeHospitalisationRequest
{
    public int IdConsultation { get; set; }
    public int IdPatient { get; set; }
    public int IdLit { get; set; }
    public string Motif { get; set; } = string.Empty;
    public string? Urgence { get; set; }
    public string? Notes { get; set; }
    public DateTime? DateSortiePrevue { get; set; }
}

/// <summary>
/// Réponse après création d'une hospitalisation (enrichie avec détails facturation)
/// </summary>
public class HospitalisationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? IdAdmission { get; set; }
    public HospitalisationDto? Hospitalisation { get; set; }
    public HospitalisationCreatedData? Data { get; set; }
}

/// <summary>
/// Données détaillées après création d'une hospitalisation
/// </summary>
public class HospitalisationCreatedData
{
    public int IdAdmission { get; set; }
    public int IdPatient { get; set; }
    public int IdLit { get; set; }
    public string? NumeroChambre { get; set; }
    public string? NumeroLit { get; set; }
    public string? StandardNom { get; set; }
    public decimal PrixJournalier { get; set; }
    public DateTime DateEntree { get; set; }
    public DateTime? DateSortiePrevue { get; set; }
    public string? Motif { get; set; }
    public string? Statut { get; set; }
    public int? IdFacture { get; set; }
    public string? NumeroFacture { get; set; }
    public decimal MontantEstime { get; set; }
    public int DureeEstimeeJours { get; set; }
}

/// <summary>
/// Requête pour terminer une hospitalisation
/// </summary>
public class TerminerHospitalisationRequest
{
    public int IdAdmission { get; set; }
    public DateTime? DateSortie { get; set; }
    public string? MotifSortie { get; set; }
    public string ResumeMedical { get; set; } = string.Empty;
    public string? NotesDepart { get; set; }
}

/// <summary>
/// Filtre pour rechercher des hospitalisations
/// </summary>
public class FiltreHospitalisationRequest
{
    public string? Statut { get; set; }
    public int? IdPatient { get; set; }
    public int? IdMedecin { get; set; }
    public int? IdService { get; set; }
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
