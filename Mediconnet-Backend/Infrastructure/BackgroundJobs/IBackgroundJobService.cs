namespace Mediconnet_Backend.Infrastructure.BackgroundJobs;

/// <summary>
/// Interface pour les services de jobs en arrière-plan
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Planifie un job pour exécution immédiate
    /// </summary>
    string Enqueue<T>(Func<T, Task> methodCall);

    /// <summary>
    /// Planifie un job avec délai
    /// </summary>
    string Schedule<T>(Func<T, Task> methodCall, TimeSpan delay);

    /// <summary>
    /// Planifie un job récurrent
    /// </summary>
    void AddOrUpdateRecurringJob<T>(string jobId, Func<T, Task> methodCall, string cronExpression);

    /// <summary>
    /// Supprime un job récurrent
    /// </summary>
    void RemoveRecurringJob(string jobId);
}
