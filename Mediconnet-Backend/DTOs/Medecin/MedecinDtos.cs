namespace Mediconnet_Backend.DTOs.Medecin;

/// <summary>
/// DTO pour le profil médecin
/// </summary>
public class MedecinProfileDto
{
    public int IdUser { get; set; }
    public string Nom { get; set; } = "";
    public string Prenom { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Telephone { get; set; }
    public string? Adresse { get; set; }
    public string? Photo { get; set; }
    public string? Sexe { get; set; }
    public DateTime? Naissance { get; set; }
    public string? NumeroOrdre { get; set; }
    public string? Specialite { get; set; }
    public int? IdSpecialite { get; set; }
    public string? Service { get; set; }
    public int IdService { get; set; }
    public DateTime? CreatedAt { get; set; }
}

/// <summary>
/// DTO pour le dashboard médecin
/// </summary>
public class MedecinDashboardDto
{
    public int TotalPatients { get; set; }
    public int ConsultationsMois { get; set; }
    public int RdvAujourdHui { get; set; }
    public int RdvAVenir { get; set; }
    public int OrdonnancesMois { get; set; }
    public int ExamensMois { get; set; }
}

/// <summary>
/// Requête de mise à jour du profil médecin
/// </summary>
public class UpdateMedecinProfileRequest
{
    public string? Telephone { get; set; }
    public string? Adresse { get; set; }
}
