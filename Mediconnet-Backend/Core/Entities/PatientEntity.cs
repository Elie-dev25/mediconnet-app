namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité Patient - Mappe à la table 'patient'
/// </summary>
public class Patient
{
    public int IdUser { get; set; }
    
    public string? NumeroDossier { get; set; }
    
    // Informations personnelles
    public string? Ethnie { get; set; }
    
    // Informations médicales
    public string? GroupeSanguin { get; set; }
    
    public string? Profession { get; set; }
    
    /// <summary>Liste des maladies chroniques (JSON ou séparées par virgule)</summary>
    public string? MaladiesChroniques { get; set; }
    
    /// <summary>A eu des opérations chirurgicales</summary>
    public bool? OperationsChirurgicales { get; set; }
    
    /// <summary>Détails des opérations chirurgicales</summary>
    public string? OperationsDetails { get; set; }
    
    /// <summary>A des allergies connues</summary>
    public bool? AllergiesConnues { get; set; }
    
    /// <summary>Détails des allergies</summary>
    public string? AllergiesDetails { get; set; }
    
    /// <summary>A des antécédents familiaux</summary>
    public bool? AntecedentsFamiliaux { get; set; }
    
    /// <summary>Détails des antécédents familiaux</summary>
    public string? AntecedentsFamiliauxDetails { get; set; }
    
    // Habitudes de vie
    /// <summary>Consomme de l'alcool</summary>
    public bool? ConsommationAlcool { get; set; }
    
    /// <summary>Fréquence de consommation d'alcool</summary>
    public string? FrequenceAlcool { get; set; }
    
    /// <summary>Fumeur</summary>
    public bool? Tabagisme { get; set; }
    
    /// <summary>Pratique une activité physique régulière</summary>
    public bool? ActivitePhysique { get; set; }
    
    // Contacts d'urgence
    public int? NbEnfants { get; set; }
    
    public string? PersonneContact { get; set; }
    
    public string? NumeroContact { get; set; }
    
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    // Déclaration sur l'honneur
    /// <summary>Déclaration sur l'honneur acceptée</summary>
    public bool DeclarationHonneurAcceptee { get; set; } = false;

    /// <summary>Date d'acceptation de la déclaration sur l'honneur</summary>
    public DateTime? DeclarationHonneurAt { get; set; }
    
    // ==================== ASSURANCE ====================
    /// <summary>ID de l'assurance (nullable si patient non assuré)</summary>
    public int? AssuranceId { get; set; }
    
    /// <summary>Numéro de carte d'assurance</summary>
    public string? NumeroCarteAssurance { get; set; }
    
    /// <summary>Date de début de validité de l'assurance</summary>
    public DateTime? DateDebutValidite { get; set; }
    
    /// <summary>Date de fin de validité de l'assurance</summary>
    public DateTime? DateFinValidite { get; set; }
    
    /// <summary>Taux de couverture assurance propre au patient (0-100)</summary>
    public decimal? CouvertureAssurance { get; set; }
    
    // Relations
    public virtual Utilisateur? Utilisateur { get; set; }
    public virtual Assurance? Assurance { get; set; }
}
