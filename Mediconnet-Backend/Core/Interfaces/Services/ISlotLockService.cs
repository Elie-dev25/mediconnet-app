namespace Mediconnet_Backend.Core.Interfaces.Services;

/// <summary>
/// Interface pour la gestion des verrous de créneaux horaires
/// Prévient les doubles réservations par verrouillage temporaire
/// </summary>
public interface ISlotLockService
{
    /// <summary>
    /// Tente d'acquérir un verrou sur un créneau
    /// </summary>
    /// <param name="medecinId">ID du médecin</param>
    /// <param name="dateHeure">Date/heure du créneau</param>
    /// <param name="duree">Durée en minutes</param>
    /// <param name="userId">ID de l'utilisateur qui demande le verrou</param>
    /// <returns>Token de verrou si succès, null si le créneau est déjà verrouillé</returns>
    Task<SlotLockResult> AcquireLockAsync(int medecinId, DateTime dateHeure, int duree, int userId);

    /// <summary>
    /// Vérifie si un verrou est valide
    /// </summary>
    Task<bool> ValidateLockAsync(string lockToken, int userId);

    /// <summary>
    /// Libère un verrou
    /// </summary>
    Task<bool> ReleaseLockAsync(string lockToken, int userId);

    /// <summary>
    /// Prolonge un verrou existant
    /// </summary>
    Task<bool> ExtendLockAsync(string lockToken, int userId, int additionalMinutes = 5);

    /// <summary>
    /// Nettoie les verrous expirés
    /// </summary>
    Task<int> CleanupExpiredLocksAsync();

    /// <summary>
    /// Vérifie si un créneau est verrouillé (par quelqu'un d'autre)
    /// </summary>
    Task<bool> IsSlotLockedAsync(int medecinId, DateTime dateHeure, int duree, int? excludeUserId = null);
}

/// <summary>
/// Résultat d'une tentative d'acquisition de verrou
/// </summary>
public class SlotLockResult
{
    public bool Success { get; set; }
    public string? LockToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? LockedByUserId { get; set; }
}
