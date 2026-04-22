using System.Globalization;

namespace Mediconnet_Backend.Helpers;

/// <summary>
/// Helper pour les calculs de créneaux horaires
/// </summary>
public static class TimeSlotHelper
{
    /// <summary>
    /// Calcule l'heure de fin à partir d'une heure de début et d'une durée
    /// </summary>
    /// <param name="heureDebut">Heure de début au format HH:mm</param>
    /// <param name="dureeMinutes">Durée en minutes</param>
    /// <returns>Heure de fin au format HH:mm</returns>
    public static string CalculerHeureFin(string heureDebut, int dureeMinutes)
    {
        if (string.IsNullOrEmpty(heureDebut))
            throw new ArgumentException("L'heure de début ne peut pas être vide", nameof(heureDebut));
        
        if (dureeMinutes < 0)
            throw new ArgumentException("La durée ne peut pas être négative", nameof(dureeMinutes));

        var parts = heureDebut.Split(':');
        if (parts.Length != 2)
            throw new ArgumentException("Format d'heure invalide. Attendu: HH:mm", nameof(heureDebut));

        if (!int.TryParse(parts[0], out var hours) || !int.TryParse(parts[1], out var minutes))
            throw new ArgumentException("Format d'heure invalide. Attendu: HH:mm", nameof(heureDebut));

        var totalMinutes = hours * 60 + minutes + dureeMinutes;
        var endHours = totalMinutes / 60;
        var endMinutes = totalMinutes % 60;

        // Gérer le dépassement de minuit
        endHours = endHours % 24;

        return $"{endHours:D2}:{endMinutes:D2}";
    }

    /// <summary>
    /// Vérifie si deux créneaux horaires se chevauchent
    /// </summary>
    /// <param name="debut1">Heure de début du premier créneau (HH:mm)</param>
    /// <param name="fin1">Heure de fin du premier créneau (HH:mm)</param>
    /// <param name="debut2">Heure de début du second créneau (HH:mm)</param>
    /// <param name="fin2">Heure de fin du second créneau (HH:mm)</param>
    /// <returns>True si les créneaux se chevauchent</returns>
    public static bool HasOverlap(string debut1, string fin1, string debut2, string fin2)
    {
        // Deux créneaux se chevauchent si debut1 < fin2 ET fin1 > debut2
        return string.Compare(debut1, fin2, StringComparison.Ordinal) < 0 
            && string.Compare(fin1, debut2, StringComparison.Ordinal) > 0;
    }

    /// <summary>
    /// Vérifie si un créneau est dans le passé
    /// </summary>
    /// <param name="date">Date du créneau</param>
    /// <param name="heureDebut">Heure de début (HH:mm)</param>
    /// <returns>True si le créneau est dans le passé</returns>
    public static bool IsInPast(DateTime date, string heureDebut)
    {
        var now = DateTime.Now;
        if (date.Date < now.Date)
            return true;
        
        if (date.Date > now.Date)
            return false;

        var currentTime = now.ToString("HH:mm", CultureInfo.InvariantCulture);
        return string.Compare(heureDebut, currentTime, StringComparison.Ordinal) < 0;
    }

    /// <summary>
    /// Vérifie si un créneau est actuellement en cours
    /// </summary>
    /// <param name="date">Date du créneau</param>
    /// <param name="heureDebut">Heure de début (HH:mm)</param>
    /// <param name="heureFin">Heure de fin (HH:mm)</param>
    /// <returns>True si le créneau est en cours</returns>
    public static bool IsCurrentlyActive(DateTime date, string heureDebut, string heureFin)
    {
        var now = DateTime.Now;
        if (date.Date != now.Date)
            return false;

        var currentTime = now.ToString("HH:mm", CultureInfo.InvariantCulture);
        return string.Compare(heureDebut, currentTime, StringComparison.Ordinal) <= 0 
            && string.Compare(heureFin, currentTime, StringComparison.Ordinal) > 0;
    }

    /// <summary>
    /// Parse une heure au format HH:mm en TimeSpan
    /// </summary>
    /// <param name="heure">Heure au format HH:mm</param>
    /// <returns>TimeSpan correspondant</returns>
    public static TimeSpan ParseHeure(string heure)
    {
        if (string.IsNullOrEmpty(heure))
            throw new ArgumentException("L'heure ne peut pas être vide", nameof(heure));

        return TimeSpan.Parse(heure, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Calcule la durée en minutes entre deux heures
    /// </summary>
    /// <param name="heureDebut">Heure de début (HH:mm)</param>
    /// <param name="heureFin">Heure de fin (HH:mm)</param>
    /// <returns>Durée en minutes</returns>
    public static int CalculerDureeMinutes(string heureDebut, string heureFin)
    {
        var debut = ParseHeure(heureDebut);
        var fin = ParseHeure(heureFin);
        
        var duree = fin - debut;
        if (duree.TotalMinutes < 0)
        {
            // Cas où le créneau passe minuit
            duree = duree.Add(TimeSpan.FromHours(24));
        }
        
        return (int)duree.TotalMinutes;
    }
}
