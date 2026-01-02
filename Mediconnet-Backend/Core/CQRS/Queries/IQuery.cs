namespace Mediconnet_Backend.Core.CQRS.Queries;

/// <summary>
/// Interface de base pour les requêtes CQRS (lecture)
/// </summary>
/// <typeparam name="TResult">Type du résultat</typeparam>
public interface IQuery<TResult>
{
}

/// <summary>
/// Interface pour les handlers de requêtes
/// </summary>
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
