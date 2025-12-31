namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité Biologiste - Mappe à la table 'biologistes'
/// </summary>
public class Biologiste
{
    public int IdBiologiste { get; set; }
    
    public int IdUser { get; set; }
    
    /// <summary>Matricule professionnel du biologiste</summary>
    public string? Matricule { get; set; }
    
    /// <summary>Spécialisation en biologie (microbiologie, biochimie, etc.)</summary>
    public string? Specialisation { get; set; }
    
    /// <summary>Date d'embauche</summary>
    public DateTime? DateEmbauche { get; set; }
    
    /// <summary>Statut actif/inactif</summary>
    public bool Actif { get; set; } = true;
    
    public DateTime? CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public virtual Utilisateur Utilisateur { get; set; } = null!;
}
