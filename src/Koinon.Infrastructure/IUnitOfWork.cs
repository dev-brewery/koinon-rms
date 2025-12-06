using Koinon.Domain.Entities;
using Koinon.Infrastructure.Repositories;

namespace Koinon.Infrastructure;

/// <summary>
/// Unit of Work pattern interface for managing transactions and coordinating repository access.
/// Provides a single point of coordination for multiple repository operations within a transaction.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Gets or creates a repository for the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type, must inherit from Entity.</typeparam>
    /// <returns>A repository instance for the entity type.</returns>
    Repositories.IRepository<T> Repository<T>() where T : Entity;

    /// <summary>
    /// Saves all pending changes to the database.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task BeginTransactionAsync(CancellationToken ct = default);

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task CommitTransactionAsync(CancellationToken ct = default);

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
