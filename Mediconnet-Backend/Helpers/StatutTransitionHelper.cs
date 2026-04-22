namespace Mediconnet_Backend.Helpers;

/// <summary>
/// Helper pour la validation des transitions de statuts
/// </summary>
public static class StatutTransitionHelper
{
    // Constantes pour les statuts communs
    private const string Planifiee = "planifiee";
    private const string EnCours = "en_cours";
    private const string EnPause = "en_pause";
    private const string Terminee = "terminee";
    private const string Annulee = "annulee";
    private const string EnAttente = "en_attente";
    private const string EnAttenteLit = "en_attente_lit";
    private const string Admis = "admis";
    private const string Sortie = "sortie";
    private const string Confirmee = "confirmee";
    private const string Acceptee = "acceptee";
    private const string Refusee = "refusee";
    private const string ContreProposition = "contre_proposition";
    private const string Programmee = "programmee";

    /// <summary>
    /// Transitions valides pour les consultations
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> ConsultationTransitions = new()
    {
        [Planifiee] = new HashSet<string> { EnCours, Annulee },
        [EnCours] = new HashSet<string> { EnPause, Terminee, Annulee },
        [EnPause] = new HashSet<string> { EnCours, Terminee, Annulee },
        [Terminee] = new HashSet<string>(), // Pas de transition depuis terminée
        [Annulee] = new HashSet<string>()   // Pas de transition depuis annulée
    };

    /// <summary>
    /// Transitions valides pour les hospitalisations
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> HospitalisationTransitions = new()
    {
        [EnAttente] = new HashSet<string> { EnAttenteLit, Admis, Annulee },
        [EnAttenteLit] = new HashSet<string> { Admis, Annulee },
        [Admis] = new HashSet<string> { EnCours, Sortie, Annulee },
        [EnCours] = new HashSet<string> { Sortie, Annulee },
        [Sortie] = new HashSet<string>(),
        [Annulee] = new HashSet<string>()
    };

    /// <summary>
    /// Transitions valides pour les réservations de bloc opératoire
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> ReservationBlocTransitions = new()
    {
        [Planifiee] = new HashSet<string> { Confirmee, Annulee },
        [Confirmee] = new HashSet<string> { EnCours, Annulee },
        [EnCours] = new HashSet<string> { Terminee, Annulee },
        [Terminee] = new HashSet<string>(),
        [Annulee] = new HashSet<string>()
    };

    /// <summary>
    /// Transitions valides pour les coordinations d'intervention
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> CoordinationTransitions = new()
    {
        [EnAttente] = new HashSet<string> { Acceptee, Refusee, ContreProposition },
        [ContreProposition] = new HashSet<string> { Acceptee, Refusee, EnAttente },
        [Acceptee] = new HashSet<string> { Programmee, Annulee },
        [Refusee] = new HashSet<string>(),
        [Programmee] = new HashSet<string> { Terminee, Annulee },
        [Terminee] = new HashSet<string>(),
        [Annulee] = new HashSet<string>()
    };

    /// <summary>
    /// Vérifie si une transition de statut de consultation est valide
    /// </summary>
    public static bool IsValidConsultationTransition(string? currentStatut, string newStatut)
    {
        if (string.IsNullOrEmpty(currentStatut))
            return newStatut == Planifiee || newStatut == EnCours;

        return ConsultationTransitions.TryGetValue(currentStatut, out var validTransitions) 
            && validTransitions.Contains(newStatut);
    }

    /// <summary>
    /// Vérifie si une transition de statut d'hospitalisation est valide
    /// </summary>
    public static bool IsValidHospitalisationTransition(string? currentStatut, string newStatut)
    {
        if (string.IsNullOrEmpty(currentStatut))
            return newStatut == EnAttente;

        return HospitalisationTransitions.TryGetValue(currentStatut, out var validTransitions) 
            && validTransitions.Contains(newStatut);
    }

    /// <summary>
    /// Vérifie si une transition de statut de réservation bloc est valide
    /// </summary>
    public static bool IsValidReservationBlocTransition(string? currentStatut, string newStatut)
    {
        if (string.IsNullOrEmpty(currentStatut))
            return newStatut == Planifiee;

        return ReservationBlocTransitions.TryGetValue(currentStatut, out var validTransitions) 
            && validTransitions.Contains(newStatut);
    }

    /// <summary>
    /// Vérifie si une transition de statut de coordination est valide
    /// </summary>
    public static bool IsValidCoordinationTransition(string? currentStatut, string newStatut)
    {
        if (string.IsNullOrEmpty(currentStatut))
            return newStatut == EnAttente;

        return CoordinationTransitions.TryGetValue(currentStatut, out var validTransitions) 
            && validTransitions.Contains(newStatut);
    }

    /// <summary>
    /// Obtient les statuts vers lesquels on peut transitionner depuis un statut donné
    /// </summary>
    public static IReadOnlyCollection<string> GetValidNextStatuts(string entityType, string? currentStatut)
    {
        var transitions = entityType.ToLowerInvariant() switch
        {
            "consultation" => ConsultationTransitions,
            "hospitalisation" => HospitalisationTransitions,
            "reservation_bloc" => ReservationBlocTransitions,
            "coordination" => CoordinationTransitions,
            _ => throw new ArgumentException($"Type d'entité inconnu: {entityType}", nameof(entityType))
        };

        if (string.IsNullOrEmpty(currentStatut))
            return Array.Empty<string>();

        return transitions.TryGetValue(currentStatut, out var validTransitions) 
            ? validTransitions.ToArray() 
            : Array.Empty<string>();
    }

    /// <summary>
    /// Vérifie si un statut est un statut final (pas de transition possible)
    /// </summary>
    public static bool IsFinalStatut(string entityType, string statut)
    {
        var nextStatuts = GetValidNextStatuts(entityType, statut);
        return nextStatuts.Count == 0;
    }
}
