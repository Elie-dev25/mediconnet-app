namespace Mediconnet_Backend.Core.Interfaces.Repositories;

/// <summary>
/// Interface Unit of Work pour gérer les transactions
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Sauvegarde toutes les modifications en base de données
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Démarre une transaction
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Valide la transaction en cours
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Annule la transaction en cours
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Récupère un repository pour une entité
    /// </summary>
    IRepository<TEntity> Repository<TEntity>() where TEntity : class;
}
