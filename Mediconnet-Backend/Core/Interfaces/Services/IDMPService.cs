namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Service Dossier Médical Partagé (DMP) - Interopérabilité avec systèmes nationaux de santé
/// </summary>
public interface IDMPService
{
    // Gestion du DMP
    Task<DMPPatientDto?> GetDMPPatientAsync(int idPatient);
    Task<DMPCreationResult> CreerDMPAsync(int idPatient, CreateDMPRequest request);
    Task<bool> ActivateDMPAsync(int idPatient);
    Task<bool> DesactiverDMPAsync(int idPatient, string motif);
    
    // Synchronisation avec le système national
    Task<SyncResult> SynchroniserAvecDMPNationalAsync(int idPatient);
    Task<SyncResult> ExporterVersDMPNationalAsync(int idPatient, List<string> documentsAExporter);
    Task<ImportResult> ImporterDepuisDMPNationalAsync(int idPatient);
    
    // Documents médicaux
    Task<List<DocumentDMPDto>> GetDocumentsPatientAsync(int idPatient, string? typeDocument = null);
    Task<DocumentDMPDto> AjouterDocumentAsync(int idPatient, AjoutDocumentDMPRequest request);
    Task<byte[]> TelechargerDocumentAsync(int idDocument);
    Task<bool> SupprimerDocumentAsync(int idDocument, string motif);
    
    // Accès et autorisations
    Task<List<AccesDMPDto>> GetHistoriqueAccesAsync(int idPatient);
    Task<bool> AccorderAccesAsync(int idPatient, AccorderAccesRequest request);
    Task<bool> RevoquerAccesAsync(int idPatient, int idProfessionnel);
    Task<List<AutorisationDMPDto>> GetAutorisationsAsync(int idPatient);
    
    // Interopérabilité HL7 FHIR
    Task<string> ExportFHIRPatientAsync(int idPatient);
    Task<string> ExportFHIRDocumentAsync(int idDocument);
    Task<ImportResult> ImportFHIRBundleAsync(string fhirBundle, int idPatient);
}

// DTOs pour le DMP
public class DMPPatientDto
{
    public int IdDMP { get; set; }
    public int IdPatient { get; set; }
    public string NomPatient { get; set; } = string.Empty;
    public string IdentifiantNational { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; }
    public DateTime? DateDerniereSync { get; set; }
    public string Statut { get; set; } = "actif"; // actif, inactif, ferme
    public bool SyncAvecNational { get; set; }
    public DMPStatsDto Stats { get; set; } = new();
}

public class DMPStatsDto
{
    public int NombreDocuments { get; set; }
    public int NombreConsultations { get; set; }
    public int NombreOrdonnances { get; set; }
    public int NombreResultatsLabo { get; set; }
    public int NombreImagerie { get; set; }
    public DateTime? DerniereModification { get; set; }
}

public class CreateDMPRequest
{
    public string? IdentifiantNational { get; set; }
    public bool ConsentementPatient { get; set; }
    public DateTime DateConsentement { get; set; }
    public bool SyncAvecNational { get; set; }
}

public class DMPCreationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? IdDMP { get; set; }
    public string? IdentifiantNational { get; set; }
}

public class SyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int DocumentsSynchronises { get; set; }
    public int Erreurs { get; set; }
    public List<string> Details { get; set; } = new();
    public DateTime DateSync { get; set; }
}

public class ImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int DocumentsImportes { get; set; }
    public int DocumentsIgnores { get; set; }
    public List<string> Erreurs { get; set; } = new();
}

public class DocumentDMPDto
{
    public int IdDocument { get; set; }
    public int IdPatient { get; set; }
    public string TypeDocument { get; set; } = string.Empty; // consultation, ordonnance, resultat_labo, imagerie, compte_rendu, etc.
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DateDocument { get; set; }
    public DateTime DateAjout { get; set; }
    public string? Auteur { get; set; }
    public string? Etablissement { get; set; }
    public string Format { get; set; } = string.Empty; // pdf, image, hl7, fhir
    public long TailleFichier { get; set; }
    public bool Confidentiel { get; set; }
    public string? ReferenceExterne { get; set; }
}

public class AjoutDocumentDMPRequest
{
    public string TypeDocument { get; set; } = string.Empty;
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DateDocument { get; set; }
    public byte[] Contenu { get; set; } = Array.Empty<byte>();
    public string Format { get; set; } = "pdf";
    public bool Confidentiel { get; set; }
    public int? IdConsultation { get; set; }
    public int? IdOrdonnance { get; set; }
}

public class AccesDMPDto
{
    public int IdAcces { get; set; }
    public int IdProfessionnel { get; set; }
    public string NomProfessionnel { get; set; } = string.Empty;
    public string TypeProfessionnel { get; set; } = string.Empty;
    public string? Etablissement { get; set; }
    public DateTime DateAcces { get; set; }
    public string TypeAcces { get; set; } = string.Empty; // lecture, ecriture, export
    public string? DocumentConsulte { get; set; }
}

public class AccorderAccesRequest
{
    public int IdProfessionnel { get; set; }
    public string TypeAcces { get; set; } = "lecture"; // lecture, ecriture, complet
    public DateTime? DateExpiration { get; set; }
    public string? Motif { get; set; }
}

public class AutorisationDMPDto
{
    public int IdAutorisation { get; set; }
    public int IdProfessionnel { get; set; }
    public string NomProfessionnel { get; set; } = string.Empty;
    public string TypeProfessionnel { get; set; } = string.Empty;
    public string? Etablissement { get; set; }
    public string TypeAcces { get; set; } = string.Empty;
    public DateTime DateAutorisation { get; set; }
    public DateTime? DateExpiration { get; set; }
    public bool Actif { get; set; }
}
