using Mediconnet_Backend.Core.Enums;

namespace Mediconnet_Backend.Core.Services;

/// <summary>
/// Service de validation des transitions de statuts pour les entités médicales.
/// Empêche les transitions invalides entre statuts.
/// </summary>
public static class StatutTransitionValidator
{
    #region Consultation
    
    private static readonly Dictionary<string, HashSet<string>> ConsultationTransitionsValides = new()
    {
        ["planifie"] = new() { "en_cours", "terminee", "annulee" }, // Permet validation directe si consultation remplie
        ["en_cours"] = new() { "en_pause", "terminee", "annulee" },
        ["en_pause"] = new() { "en_cours", "terminee", "annulee" }, // Peut reprendre, terminer ou annuler
        ["terminee"] = new(), // Aucune transition possible depuis terminée
        ["annulee"] = new()   // Aucune transition possible depuis annulée
    };

    /// <summary>
    /// Vérifie si une transition de statut de consultation est valide
    /// </summary>
    public static bool IsConsultationTransitionValid(string? from, string? to)
    {
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to)) return false;
        var fromLower = from.ToLower();
        var toLower = to.ToLower();
        
        return ConsultationTransitionsValides.TryGetValue(fromLower, out var valid) 
               && valid.Contains(toLower);
    }

    /// <summary>
    /// Obtient les transitions valides depuis un statut de consultation
    /// </summary>
    public static IEnumerable<string> GetValidConsultationTransitions(string? from)
    {
        if (string.IsNullOrEmpty(from)) return Enumerable.Empty<string>();
        var fromLower = from.ToLower();
        
        return ConsultationTransitionsValides.TryGetValue(fromLower, out var valid) 
            ? valid 
            : Enumerable.Empty<string>();
    }

    #endregion

    #region Examen
    
    private static readonly Dictionary<string, HashSet<string>> ExamenTransitionsValides = new()
    {
        ["prescrit"] = new() { "en_cours", "annule" },
        ["en_cours"] = new() { "termine", "annule" },
        ["termine"] = new(),  // Aucune transition possible depuis terminé
        ["annule"] = new()    // Aucune transition possible depuis annulé
    };

    /// <summary>
    /// Vérifie si une transition de statut d'examen est valide
    /// </summary>
    public static bool IsExamenTransitionValid(string? from, string? to)
    {
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to)) return false;
        var fromLower = from.ToLower();
        var toLower = to.ToLower();
        
        // Gérer les alias
        if (toLower == "realise") toLower = "termine";
        
        return ExamenTransitionsValides.TryGetValue(fromLower, out var valid) 
               && valid.Contains(toLower);
    }

    /// <summary>
    /// Obtient les transitions valides depuis un statut d'examen
    /// </summary>
    public static IEnumerable<string> GetValidExamenTransitions(string? from)
    {
        if (string.IsNullOrEmpty(from)) return Enumerable.Empty<string>();
        var fromLower = from.ToLower();
        
        return ExamenTransitionsValides.TryGetValue(fromLower, out var valid) 
            ? valid 
            : Enumerable.Empty<string>();
    }

    #endregion

    #region Hospitalisation
    
    private static readonly Dictionary<string, HashSet<string>> HospitalisationTransitionsValides = new()
    {
        ["en_attente"] = new() { "en_cours", "annule" },
        ["en_cours"] = new() { "termine" },
        ["termine"] = new(),  // Aucune transition possible depuis terminé
        ["annule"] = new()    // Aucune transition possible depuis annulé
    };

    /// <summary>
    /// Vérifie si une transition de statut d'hospitalisation est valide
    /// </summary>
    public static bool IsHospitalisationTransitionValid(string? from, string? to)
    {
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to)) return false;
        var fromLower = from.ToLower();
        var toLower = to.ToLower();
        
        return HospitalisationTransitionsValides.TryGetValue(fromLower, out var valid) 
               && valid.Contains(toLower);
    }

    /// <summary>
    /// Obtient les transitions valides depuis un statut d'hospitalisation
    /// </summary>
    public static IEnumerable<string> GetValidHospitalisationTransitions(string? from)
    {
        if (string.IsNullOrEmpty(from)) return Enumerable.Empty<string>();
        var fromLower = from.ToLower();
        
        return HospitalisationTransitionsValides.TryGetValue(fromLower, out var valid) 
            ? valid 
            : Enumerable.Empty<string>();
    }

    #endregion

    #region Soin
    
    private static readonly Dictionary<string, HashSet<string>> SoinTransitionsValides = new()
    {
        ["prescrit"] = new() { "en_cours", "annule" },
        ["en_cours"] = new() { "termine", "annule" },
        ["termine"] = new(),  // Aucune transition possible depuis terminé
        ["annule"] = new()    // Aucune transition possible depuis annulé
    };

    /// <summary>
    /// Vérifie si une transition de statut de soin est valide
    /// </summary>
    public static bool IsSoinTransitionValid(string? from, string? to)
    {
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to)) return false;
        var fromLower = from.ToLower();
        var toLower = to.ToLower();
        
        return SoinTransitionsValides.TryGetValue(fromLower, out var valid) 
               && valid.Contains(toLower);
    }

    #endregion

    #region Rendez-vous
    
    private static readonly Dictionary<string, HashSet<string>> RendezVousTransitionsValides = new()
    {
        ["en_attente"] = new() { "confirme", "planifie", "annule" },
        ["confirme"] = new() { "planifie", "annule" },
        ["planifie"] = new() { "termine", "annule" },
        ["termine"] = new(),  // Aucune transition possible depuis terminé
        ["annule"] = new()    // Aucune transition possible depuis annulé
    };

    /// <summary>
    /// Vérifie si une transition de statut de rendez-vous est valide
    /// </summary>
    public static bool IsRendezVousTransitionValid(string? from, string? to)
    {
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to)) return false;
        var fromLower = from.ToLower();
        var toLower = to.ToLower();
        
        return RendezVousTransitionsValides.TryGetValue(fromLower, out var valid) 
               && valid.Contains(toLower);
    }

    #endregion

    #region Messages d'erreur

    /// <summary>
    /// Génère un message d'erreur pour une transition invalide
    /// </summary>
    public static string GetTransitionErrorMessage(string entityType, string? from, string? to)
    {
        var validTransitions = entityType.ToLower() switch
        {
            "consultation" => GetValidConsultationTransitions(from),
            "examen" => GetValidExamenTransitions(from),
            "hospitalisation" => GetValidHospitalisationTransitions(from),
            _ => Enumerable.Empty<string>()
        };

        var validStr = validTransitions.Any() 
            ? string.Join(", ", validTransitions) 
            : "aucune";

        return $"Transition de statut invalide pour {entityType}: '{from}' → '{to}'. " +
               $"Transitions valides depuis '{from}': {validStr}";
    }

    #endregion
}
