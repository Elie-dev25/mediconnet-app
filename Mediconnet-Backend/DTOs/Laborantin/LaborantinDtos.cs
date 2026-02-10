using System.ComponentModel.DataAnnotations;

namespace Mediconnet_Backend.DTOs.Laborantin;

/// <summary>
/// DTO pour un examen dans la liste du laborantin
/// </summary>
public class ExamenLaborantinDto
{
    public int IdBulletinExamen { get; set; }
    public DateTime DateDemande { get; set; }
    public string? TypeExamen { get; set; }
    public string? NomExamen { get; set; }
    public string? Categorie { get; set; }
    public string? Specialite { get; set; }
    public string? Instructions { get; set; }
    public bool Urgence { get; set; }
    public string Statut { get; set; } = "prescrit";
    
    // Patient
    public int? IdPatient { get; set; }
    public string? PatientNom { get; set; }
    public string? PatientPrenom { get; set; }
    public string? PatientNumeroDossier { get; set; }
    public DateTime? PatientDateNaissance { get; set; }
    public string? PatientSexe { get; set; }
    
    // Médecin prescripteur
    public int? IdMedecin { get; set; }
    public string? MedecinNom { get; set; }
    public string? MedecinPrenom { get; set; }
    public string? MedecinSpecialite { get; set; }
    
    // Laboratoire
    public int? IdLabo { get; set; }
    public string? NomLabo { get; set; }
    
    // Résultat (si disponible)
    public DateTime? DateResultat { get; set; }
    public string? ResultatTexte { get; set; }
    public bool HasResultat { get; set; }
    public string? DocumentResultatUuid { get; set; }
}

/// <summary>
/// DTO pour les détails complets d'un examen
/// </summary>
public class ExamenDetailsDto
{
    public int IdBulletinExamen { get; set; }
    public DateTime DateDemande { get; set; }
    public string? TypeExamen { get; set; }
    public string? NomExamen { get; set; }
    public string? Description { get; set; }
    public string? Categorie { get; set; }
    public string? Specialite { get; set; }
    public string? Instructions { get; set; }
    public bool Urgence { get; set; }
    public string Statut { get; set; } = "prescrit";
    public decimal? Prix { get; set; }
    
    // Patient complet
    public PatientExamenDto? Patient { get; set; }
    
    // Médecin prescripteur
    public MedecinExamenDto? Medecin { get; set; }
    
    // Laboratoire
    public LaboratoireDto? Laboratoire { get; set; }
    
    // Consultation/Hospitalisation source
    public int? IdConsultation { get; set; }
    public int? IdHospitalisation { get; set; }
    public DateTime? DateConsultation { get; set; }
    
    // Résultat
    public ResultatExamenDto? Resultat { get; set; }
    
    // Historique des examens du patient (même type)
    public List<HistoriqueExamenDto> Historique { get; set; } = new();
}

public class PatientExamenDto
{
    public int IdPatient { get; set; }
    public string Nom { get; set; } = "";
    public string Prenom { get; set; } = "";
    public string? NumeroDossier { get; set; }
    public DateTime? DateNaissance { get; set; }
    public string? Sexe { get; set; }
    public string? Telephone { get; set; }
    public string? GroupeSanguin { get; set; }
    public string? Allergies { get; set; }
}

public class MedecinExamenDto
{
    public int IdMedecin { get; set; }
    public string Nom { get; set; } = "";
    public string Prenom { get; set; } = "";
    public string? Specialite { get; set; }
    public string? Telephone { get; set; }
}

public class LaboratoireDto
{
    public int IdLabo { get; set; }
    public string NomLabo { get; set; } = "";
    public string? Contact { get; set; }
    public string? Adresse { get; set; }
    public string? Telephone { get; set; }
    public string? Type { get; set; }
}

public class ResultatExamenDto
{
    public DateTime? DateResultat { get; set; }
    public string? ResultatTexte { get; set; }
    public string? CommentaireLabo { get; set; }
    public int? IdLaborantin { get; set; }
    public string? LaborantinNom { get; set; }
    public List<DocumentResultatDto> Documents { get; set; } = new();
}

public class DocumentResultatDto
{
    public string Uuid { get; set; } = "";
    public string NomFichier { get; set; } = "";
    public string? MimeType { get; set; }
    public long TailleOctets { get; set; }
    public string? DateUpload { get; set; }
    public string? Description { get; set; }
}

public class HistoriqueExamenDto
{
    public int IdBulletinExamen { get; set; }
    public DateTime DateDemande { get; set; }
    public string? NomExamen { get; set; }
    public string Statut { get; set; } = "";
    public DateTime? DateResultat { get; set; }
    public bool HasResultat { get; set; }
}

/// <summary>
/// Request pour enregistrer un résultat d'examen
/// </summary>
public class EnregistrerResultatRequest
{
    [Required]
    public string ResultatTexte { get; set; } = "";
    
    public string? Commentaire { get; set; }
}

/// <summary>
/// DTO pour les statistiques du dashboard laborantin
/// </summary>
public class LaborantinStatsDto
{
    public int ExamensEnAttente { get; set; }
    public int ExamensEnCours { get; set; }
    public int ExamensTerminesAujourdhui { get; set; }
    public int Urgences { get; set; }
    public int TotalExamensJour { get; set; }
}

/// <summary>
/// DTO pour la liste paginée des examens
/// </summary>
public class ExamensListResponse
{
    public List<ExamenLaborantinDto> Examens { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// DTO pour les détails d'un résultat d'examen (consultation patient/médecin)
/// </summary>
public class ResultatExamenDetailDto
{
    public int IdBulletinExamen { get; set; }
    public DateTime DateDemande { get; set; }
    public DateTime? DateResultat { get; set; }
    public string Statut { get; set; } = "";
    public bool Urgence { get; set; }
    public string NomExamen { get; set; } = "";
    public string? Categorie { get; set; }
    public string? Specialite { get; set; }
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public string? ResultatTexte { get; set; }
    public string? CommentaireLabo { get; set; }
    public LaboratoireInfoDto? Laboratoire { get; set; }
    public PersonneInfoDto? Patient { get; set; }
    public PersonneInfoDto? Medecin { get; set; }
    public List<DocumentResultatDto> Documents { get; set; } = new();
}

/// <summary>
/// DTO pour la liste des résultats d'examens
/// </summary>
public class ResultatExamenListDto
{
    public int IdBulletinExamen { get; set; }
    public DateTime DateDemande { get; set; }
    public DateTime? DateResultat { get; set; }
    public string NomExamen { get; set; } = "";
    public string? Specialite { get; set; }
    public string? NomLabo { get; set; }
    public bool HasDocuments { get; set; }
}

/// <summary>
/// DTO pour les informations du laboratoire
/// </summary>
public class LaboratoireInfoDto
{
    public int IdLabo { get; set; }
    public string NomLabo { get; set; } = "";
    public string? Telephone { get; set; }
}

/// <summary>
/// DTO pour les informations d'une personne (patient/médecin)
/// </summary>
public class PersonneInfoDto
{
    public int IdUser { get; set; }
    public string Nom { get; set; } = "";
    public string Prenom { get; set; } = "";
    public DateTime? DateNaissance { get; set; }
}
