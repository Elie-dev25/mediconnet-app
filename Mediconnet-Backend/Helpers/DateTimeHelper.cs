namespace Mediconnet_Backend.Helpers;

/// <summary>
/// Helper pour la gestion des dates et fuseaux horaires
/// Utilise le fuseau horaire du Cameroun (UTC+1) comme référence
/// </summary>
public static class DateTimeHelper
{
    /// <summary>
    /// Fuseau horaire du Cameroun (West Africa Time - UTC+1)
    /// </summary>
    private static readonly TimeZoneInfo CameroonTimeZone = TimeZoneInfo.CreateCustomTimeZone(
        "Cameroon Standard Time",
        TimeSpan.FromHours(1),
        "Cameroon Standard Time",
        "Cameroon Standard Time"
    );

    /// <summary>
    /// Obtient l'heure actuelle au Cameroun (UTC+1)
    /// </summary>
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CameroonTimeZone);

    /// <summary>
    /// Obtient la date du jour au Cameroun (UTC+1)
    /// </summary>
    public static DateTime Today => Now.Date;

    /// <summary>
    /// Convertit une date UTC vers l'heure du Cameroun
    /// </summary>
    public static DateTime FromUtc(DateTime utcDateTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, CameroonTimeZone);
    }

    /// <summary>
    /// Convertit une date locale Cameroun vers UTC
    /// </summary>
    public static DateTime ToUtc(DateTime cameroonDateTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(cameroonDateTime, DateTimeKind.Unspecified), 
            CameroonTimeZone
        );
    }

    /// <summary>
    /// Vérifie si un créneau horaire est passé (à la minute près)
    /// Un créneau est considéré passé si son heure de début est strictement inférieure à l'heure actuelle
    /// Exemple: Si maintenant = 19h31, le créneau 19h30 est passé, mais 19h31 ne l'est pas encore
    /// </summary>
    public static bool IsSlotPassed(DateTime slotDateTime)
    {
        var now = Now;
        // Comparer à la minute près (ignorer les secondes et millisecondes)
        var slotMinute = new DateTime(slotDateTime.Year, slotDateTime.Month, slotDateTime.Day, 
                                       slotDateTime.Hour, slotDateTime.Minute, 0);
        var nowMinute = new DateTime(now.Year, now.Month, now.Day, 
                                      now.Hour, now.Minute, 0);
        
        return slotMinute < nowMinute;
    }

    /// <summary>
    /// Vérifie si une date/heure est dans le futur (permettant le jour même)
    /// </summary>
    public static bool IsFuture(DateTime dateTime)
    {
        return !IsSlotPassed(dateTime);
    }

    /// <summary>
    /// Vérifie si une date est aujourd'hui au Cameroun
    /// </summary>
    public static bool IsToday(DateTime dateTime)
    {
        return dateTime.Date == Today;
    }
}
