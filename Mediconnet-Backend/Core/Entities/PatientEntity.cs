using System.ComponentModel.DataAnnotations.Schema;

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
    
    // ==================== DOSSIER MÉDICAL ====================
    /// <summary>Indique si le dossier médical est clôturé (prochaine consultation = première consultation)</summary>
    [Column("dossier_cloture")]
    public bool DossierCloture { get; set; } = false;
    
    /// <summary>Date de clôture du dossier</summary>
    [Column("date_cloture_dossier")]
    public DateTime? DateClotureDossier { get; set; }
    
    /// <summary>ID du médecin ayant clôturé le dossier</summary>
    [Column("id_medecin_cloture")]
    public int? IdMedecinCloture { get; set; }
    
    // ==================== ASSURANCE ====================
    /// <summary>ID de l'assurance (nullable si patient non assuré)</summary>
    public int? AssuranceId { get; set; }
    
    /// <summary>Numéro de carte d'assurance</summary>
    public string? NumeroCarteAssurance { get; set; }
    
    /// <summary>Date de début de validité de l'assurance</summary>
    public DateTime? DateDebutValidite { get; set; }
    
    /// <summary>Date de fin de validité de l'assurance</summary>
    public DateTime? DateFinValidite { get; set; }
    
    /// <summary>
    /// Override manuel du taux de couverture (0-100).
    /// Si défini, ce taux prend priorité sur la configuration AssuranceCouverture.
    /// Utilisé pour les cas exceptionnels (négociations spéciales, contrats particuliers).
    /// </summary>
    public decimal? TauxCouvertureOverride { get; set; }
    
    /// <summary>
    /// Ancien champ - conservé pour compatibilité, mappé vers taux_couverture_override en DB.
    /// Utiliser TauxCouvertureOverride à la place.
    /// </summary>
    [Obsolete("Utiliser TauxCouvertureOverride à la place")]
    public decimal? CouvertureAssurance 
    { 
        get => TauxCouvertureOverride; 
        set => TauxCouvertureOverride = value; 
    }
    
    // Relations
    public virtual Utilisateur? Utilisateur { get; set; }
    public virtual Assurance? Assurance { get; set; }
}
