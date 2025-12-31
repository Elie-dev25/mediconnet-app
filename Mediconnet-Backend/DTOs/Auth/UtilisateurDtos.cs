namespace Mediconnet_Backend.DTOs.Auth;

/// <summary>
/// DTO pour afficher les informations d'un utilisateur
/// </summary>
public class UtilisateurDto
{
    public int IdUser { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Telephone { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool EmailConfirmed { get; set; } = false;
}
