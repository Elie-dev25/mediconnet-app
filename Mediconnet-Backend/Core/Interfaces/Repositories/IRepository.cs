using System.Linq.Expressions;

namespace Mediconnet_Backend.Core.Interfaces.Repositories;

/// <summary>
/// Interface générique pour le Repository Pattern
/// Abstraction de la couche d'accès aux données
/// </summary>
/// <typeparam name="TEntity">Type de l'entité</typeparam>
/// <typeparam name="TKey">Type de la clé primaire</typeparam>
public interface IRepository<TEntity, TKey> where TEntity : class
{
    // ==================== READ ====================
    
    /// <summary>
    /// Récupère une entité par son identifiant
    /// </summary>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Récupère toutes les entités
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Récupère les entités correspondant à un prédicat
    /// </summary>
    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Récupère la première entité correspondant à un prédicat
    /// </summary>
    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Vérifie si une entité existe selon un prédicat
    /// </summary>
    Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compte les entités correspondant à un prédicat
    /// </summary>
    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    // ==================== WRITE ====================

    /// <summary>
    /// Ajoute une nouvelle entité
    /// </summary>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ajoute plusieurs entités
    /// </summary>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Met à jour une entité
    /// </summary>
    void Update(TEntity entity);

    /// <summary>
    /// Met à jour plusieurs entités
    /// </summary>
    void UpdateRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// Supprime une entité
    /// </summary>
    void Remove(TEntity entity);

    /// <summary>
    /// Supprime plusieurs entités
    /// </summary>
    void RemoveRange(IEnumerable<TEntity> entities);

    // ==================== QUERY ====================

    /// <summary>
    /// Retourne un IQueryable pour des requêtes personnalisées
    /// </summary>
    IQueryable<TEntity> Query();

    /// <summary>
    /// Retourne un IQueryable sans tracking pour des lectures optimisées
    /// </summary>
    IQueryable<TEntity> QueryNoTracking();
}

/// <summary>
/// Interface pour les entités avec clé primaire int
/// </summary>
public interface IRepository<TEntity> : IRepository<TEntity, int> where TEntity : class
{
}
