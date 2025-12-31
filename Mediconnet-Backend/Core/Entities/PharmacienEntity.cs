namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité Pharmacien - Mappe à la table 'pharmaciens'
/// </summary>
public class Pharmacien
{
    public int IdPharmacien { get; set; }
    
    public int IdUser { get; set; }
    
    /// <summary>Matricule professionnel du pharmacien</summary>
    public string? Matricule { get; set; }
    
    /// <summary>Numéro d'inscription à l'ordre des pharmaciens</summary>
    public string? NumeroOrdre { get; set; }
    
    /// <summary>Date d'embauche</summary>
    public DateTime? DateEmbauche { get; set; }
    
    /// <summary>Statut actif/inactif</summary>
    public bool Actif { get; set; } = true;
    
    public DateTime? CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public virtual Utilisateur Utilisateur { get; set; } = null!;
}
