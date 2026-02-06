namespace Mediconnet_Backend.DTOs;

/// <summary>
/// DTO pour la liste des standards de chambre
/// </summary>
public class StandardChambreDto
{
    public int IdStandard { get; set; }
    public string Nom { get; set; } = "";
    public string? Description { get; set; }
    public decimal PrixJournalier { get; set; }
    public List<string> Privileges { get; set; } = new();
    public string? Localisation { get; set; }
    public bool Actif { get; set; }
    public int NombreChambres { get; set; }
    public int ChambresDisponibles { get; set; }
}

/// <summary>
/// DTO pour créer un standard de chambre
/// </summary>
public class CreateStandardChambreRequest
{
    public string Nom { get; set; } = "";
    public string? Description { get; set; }
    public decimal PrixJournalier { get; set; }
    public List<string> Privileges { get; set; } = new();
    public string? Localisation { get; set; }
}

/// <summary>
/// DTO pour mettre à jour un standard de chambre
/// </summary>
public class UpdateStandardChambreRequest
{
    public string? Nom { get; set; }
    public string? Description { get; set; }
    public decimal? PrixJournalier { get; set; }
    public List<string>? Privileges { get; set; }
    public string? Localisation { get; set; }
    public bool? Actif { get; set; }
}

/// <summary>
/// DTO pour la sélection de standard lors de l'hospitalisation
/// </summary>
public class StandardChambreSelectDto
{
    public int IdStandard { get; set; }
    public string Nom { get; set; } = "";
    public decimal PrixJournalier { get; set; }
    public List<string> Privileges { get; set; } = new();
    public string? Localisation { get; set; }
    public string DisplayText => $"{Nom} – {PrixJournalier:N0} FCFA";
}
