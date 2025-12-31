using Mediconnet_Backend.Core.Enums;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité Utilisateur - Mappe à la table 'utilisateurs'
/// </summary>
public class Utilisateur
{
    public int IdUser { get; set; }
    
    public string Nom { get; set; } = string.Empty;
    
    public string Prenom { get; set; } = string.Empty;
    
    public DateTime? Naissance { get; set; }
    
    public string? Sexe { get; set; }
    
    public string? Telephone { get; set; }
    
    public string Email { get; set; } = string.Empty;
    
    public string? SituationMatrimoniale { get; set; }
    
    public string? Adresse { get; set; }
    
    /// <summary>Rôle: patient, medecin, infirmier, administrateur, caissier, accueil</summary>
    public string Role { get; set; } = "patient";
    
    /// <summary>Password hashé (champ ajouté pour l'authentification)</summary>
    public string? PasswordHash { get; set; }
    
    /// <summary>URL ou chemin de la photo de profil (optionnel)</summary>
    public string? Photo { get; set; }

    /// <summary>Indique si l'email a été confirmé</summary>
    public bool EmailConfirmed { get; set; } = false;

    /// <summary>Date de confirmation de l'email</summary>
    public DateTime? EmailConfirmedAt { get; set; }

    /// <summary>Indique si le profil est complété</summary>
    public bool ProfileCompleted { get; set; } = false;

    /// <summary>Date de complétion du profil</summary>
    public DateTime? ProfileCompletedAt { get; set; }

    /// <summary>Indique si l'utilisateur doit changer son mot de passe à la prochaine connexion</summary>
    public bool MustChangePassword { get; set; } = false;

    /// <summary>Nationalité (par défaut Cameroun)</summary>
    public string? Nationalite { get; set; }

    /// <summary>Région d'origine</summary>
    public string? RegionOrigine { get; set; }
    
    public DateTime? CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    // Relations
    public virtual Patient? Patient { get; set; }
    public virtual Medecin? Medecin { get; set; }
    public virtual Infirmier? Infirmier { get; set; }
    public virtual Administrateur? Administrateur { get; set; }
    public virtual Caissier? Caissier { get; set; }
    public virtual Accueil? Accueil { get; set; }
}
