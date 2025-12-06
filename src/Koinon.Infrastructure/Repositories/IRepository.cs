using System.Linq.Expressions;
using Koinon.Domain.Entities;

namespace Koinon.Infrastructure.Repositories;

/// <summary>
/// Generic repository interface for data access operations.
/// Provides standard CRUD operations and query capabilities for entities.
/// All methods are async and support cancellation tokens for high-performance scenarios.
/// </summary>
/// <typeparam name="T">The entity type, must inherit from Entity.</typeparam>
public interface IRepository<T> where T : Entity
{
    /// <summary>
    /// Retrieves an entity by its integer ID.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves an entity by its URL-safe IdKey.
    /// </summary>
    /// <param name="idKey">The URL-safe Base64-encoded ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    Task<T?> GetByIdKeyAsync(string idKey, CancellationToken ct = default);

    /// <summary>
    /// Retrieves an entity by its globally unique identifier.
    /// </summary>
    /// <param name="guid">The entity GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    Task<T?> GetByGuidAsync(Guid guid, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all entities of type T.
    /// Uses AsNoTracking for read-only scenarios.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of all entities.</returns>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Finds entities matching the specified predicate.
    /// Uses AsNoTracking for read-only scenarios.
    /// </summary>
    /// <param name="predicate">The filter expression.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of matching entities.</returns>
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    /// <summary>
    /// Provides direct IQueryable access for complex queries.
    /// Uses AsNoTracking by default.
    /// </summary>
    /// <returns>A queryable collection of entities.</returns>
    IQueryable<T> Query();

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The added entity with generated ID.</returns>
    Task<T> AddAsync(T entity, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(T entity, CancellationToken ct = default);

    /// <summary>
    /// Deletes an entity from the repository.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(T entity, CancellationToken ct = default);

    /// <summary>
    /// Checks if an entity with the specified ID exists.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the entity exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Returns the total count of entities of type T.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The total count.</returns>
    Task<int> CountAsync(CancellationToken ct = default);
}
