namespace Mediconnet_Backend.DTOs.Documents;

/// <summary>
/// DTO pour l'upload d'un document médical
/// </summary>
public class UploadDocumentRequest
{
    public int IdPatient { get; set; }
    public string TypeDocument { get; set; } = "autre";
    public string? SousType { get; set; }
    public string? Description { get; set; }
    public DateTime? DateDocument { get; set; }
    public string NiveauConfidentialite { get; set; } = "normal";
    public bool AccesPatient { get; set; } = true;
    public int? IdConsultation { get; set; }
    public int? IdBulletinExamen { get; set; }
    public int? IdHospitalisation { get; set; }
    public int? IdDmp { get; set; }
    public List<string>? Tags { get; set; }
}

/// <summary>
/// DTO de réponse pour un document médical
/// </summary>
public class DocumentMedicalDto
{
    public string Uuid { get; set; } = string.Empty;
    public string NomFichierOriginal { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long TailleOctets { get; set; }
    public string TypeDocument { get; set; } = string.Empty;
    public string? SousType { get; set; }
    public string NiveauConfidentialite { get; set; } = "normal";
    public bool AccesPatient { get; set; }
    public int IdPatient { get; set; }
    public string? PatientNom { get; set; }
    public int? IdConsultation { get; set; }
    public int? IdBulletinExamen { get; set; }
    public int? IdHospitalisation { get; set; }
    public int? IdDmp { get; set; }
    public int IdCreateur { get; set; }
    public string? CreateurNom { get; set; }
    public DateTime? DateDocument { get; set; }
    public string? Description { get; set; }
    public string Statut { get; set; } = "actif";
    public DateTime CreatedAt { get; set; }
    public bool HashPresent { get; set; }
}

/// <summary>
/// DTO pour la liste des documents
/// </summary>
public class DocumentListResponse
{
    public List<DocumentMedicalDto> Documents { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// DTO pour les statistiques de documents
/// </summary>
public class DocumentStatsDto
{
    public string TypeDocument { get; set; } = string.Empty;
    public int NombreDocuments { get; set; }
    public long TailleTotaleOctets { get; set; }
    public int AvecHash { get; set; }
    public int SansHash { get; set; }
    public int Actifs { get; set; }
    public int Archives { get; set; }
    public int EnQuarantaine { get; set; }
}

/// <summary>
/// DTO pour le résultat de vérification d'intégrité
/// </summary>
public class IntegrityVerificationDto
{
    public string DocumentUuid { get; set; } = string.Empty;
    public string Statut { get; set; } = string.Empty;
    public string? HashAttendu { get; set; }
    public string? HashCalcule { get; set; }
    public ulong? TailleAttendue { get; set; }
    public ulong? TailleReelle { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Message { get; set; }
}
