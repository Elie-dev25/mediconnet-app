namespace Mediconnet_Backend.DTOs.Admin;

/// <summary>
/// DTO pour afficher une affectation de service
/// </summary>
public class AffectationServiceDto
{
    public int IdAffectation { get; set; }
    public int IdUser { get; set; }
    public string TypeUser { get; set; } = string.Empty;
    public int IdService { get; set; }
    public string NomService { get; set; } = string.Empty;
    public DateTime DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public string? MotifChangement { get; set; }
    public int? IdAdminChangement { get; set; }
    public string? NomAdminChangement { get; set; }
    public bool EstActif => DateFin == null;
}

/// <summary>
/// DTO pour l'historique des affectations d'un utilisateur
/// </summary>
public class HistoriqueAffectationsDto
{
    public int IdUser { get; set; }
    public string NomComplet { get; set; } = string.Empty;
    public string TypeUser { get; set; } = string.Empty;
    public AffectationServiceDto? AffectationActuelle { get; set; }
    public List<AffectationServiceDto> Historique { get; set; } = new();
}

/// <summary>
/// Requête pour changer le service d'un utilisateur
/// </summary>
public class ChangerServiceRequest
{
    /// <summary>
    /// ID du nouveau service
    /// </summary>
    public int IdNouveauService { get; set; }

    /// <summary>
    /// Motif du changement (optionnel)
    /// </summary>
    public string? Motif { get; set; }
}

/// <summary>
/// Réponse après un changement de service
/// </summary>
public class ChangerServiceResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public AffectationServiceDto? NouvelleAffectation { get; set; }
}
