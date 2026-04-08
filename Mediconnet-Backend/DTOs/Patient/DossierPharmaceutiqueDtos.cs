namespace Mediconnet_Backend.DTOs.Patient;

/// <summary>
/// DTO pour le dossier pharmaceutique complet du patient
/// Regroupe toutes les ordonnances avec leur statut de délivrance
/// </summary>
public class DossierPharmaceutiqueDto
{
    public int IdPatient { get; set; }
    public string NomPatient { get; set; } = "";
    public int TotalOrdonnances { get; set; }
    public int OrdonnancesActives { get; set; }
    public int OrdonnancesDelivrees { get; set; }
    public int OrdonnancesPartielles { get; set; }
    public List<OrdonnancePatientDto> Ordonnances { get; set; } = new();
}

/// <summary>
/// DTO d'une ordonnance vue par le patient
/// Inclut les informations de délivrance
/// </summary>
public class OrdonnancePatientDto
{
    public int IdOrdonnance { get; set; }
    public DateTime DatePrescription { get; set; }
    
    // Médecin prescripteur
    public int IdMedecin { get; set; }
    public string NomMedecin { get; set; } = "";
    public string? SpecialiteMedecin { get; set; }
    
    // Contexte
    public string TypeContexte { get; set; } = ""; // consultation, hospitalisation, directe
    public string? Service { get; set; }
    public int? IdConsultation { get; set; }
    public int? IdHospitalisation { get; set; }
    
    // Diagnostic/Motif
    public string? Diagnostic { get; set; }
    public string? Notes { get; set; }
    
    // Statut
    public string Statut { get; set; } = "active"; // active, dispensee, partielle, annulee, expiree
    public string StatutDelivrance { get; set; } = "non_delivre"; // non_delivre, en_attente, partiel, delivre
    public DateTime? DateExpiration { get; set; }
    public bool EstExpire { get; set; }
    
    // Renouvellement
    public bool Renouvelable { get; set; }
    public int? NombreRenouvellements { get; set; }
    public int? RenouvellementRestants { get; set; }
    
    // Médicaments
    public List<MedicamentOrdonnanceDto> Medicaments { get; set; } = new();
    
    // Informations de délivrance
    public DateTime? DateDelivrance { get; set; }
    public string? NomPharmacien { get; set; }
}

/// <summary>
/// DTO d'un médicament prescrit avec son statut de délivrance
/// </summary>
public class MedicamentOrdonnanceDto
{
    public int IdPrescriptionMed { get; set; }
    public int? IdMedicament { get; set; }
    
    // Informations du médicament
    public string NomMedicament { get; set; } = "";
    public string? Dosage { get; set; }
    public string? FormePharmaceutique { get; set; }
    public string? VoieAdministration { get; set; }
    public bool EstHorsCatalogue { get; set; }
    
    // Prescription
    public int QuantitePrescrite { get; set; }
    public string? Posologie { get; set; }
    public string? Frequence { get; set; }
    public string? DureeTraitement { get; set; }
    public string? Instructions { get; set; }
    
    // Délivrance
    public int QuantiteDelivree { get; set; }
    public string StatutDelivrance { get; set; } = "non_delivre"; // non_delivre, partiel, delivre
    public DateTime? DateDelivrance { get; set; }
}

/// <summary>
/// Filtre pour récupérer les ordonnances du patient
/// </summary>
public class FiltreOrdonnancesPatientRequest
{
    public string? Statut { get; set; } // active, dispensee, partielle, annulee, expiree
    public string? TypeContexte { get; set; } // consultation, hospitalisation, directe
    public DateTime? DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public int? IdMedecin { get; set; }
    public string? Tri { get; set; } = "date_desc"; // date_desc, date_asc, medecin
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
