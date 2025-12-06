using Koinon.Domain.Entities;
using Koinon.Infrastructure.Data;
using Koinon.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Koinon.Infrastructure;

/// <summary>
/// Unit of Work implementation for managing transactions and repository coordination.
/// Uses Entity Framework Core DbContext as the underlying transaction coordinator.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly KoinonDbContext _context;
    private readonly Dictionary<Type, object> _repositories;
    private IDbContextTransaction? _currentTransaction;
    private bool _disposed;

    public UnitOfWork(KoinonDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _repositories = new Dictionary<Type, object>();
    }

    /// <inheritdoc />
    public Repositories.IRepository<T> Repository<T>() where T : Entity
    {
        var type = typeof(T);

        // Return existing repository if already created
        if (_repositories.TryGetValue(type, out var existingRepository))
        {
            return (Repositories.IRepository<T>)existingRepository;
        }

        // Create new repository instance
        var repository = new Repository<T>(_context);
        _repositories[type] = repository;

        return repository;
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(ct);
    }

    /// <inheritdoc />
    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction is in progress.");
        }

        try
        {
            await _context.SaveChangesAsync(ct);
            await _currentTransaction.CommitAsync(ct);
        }
        catch
        {
            await RollbackTransactionAsync(ct);
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    /// <inheritdoc />
    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction is in progress.");
        }

        try
        {
            await _currentTransaction.RollbackAsync(ct);
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    /// <summary>
    /// Disposes the Unit of Work and its associated resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose transaction if still active
                if (_currentTransaction != null)
                {
                    _currentTransaction.Dispose();
                    _currentTransaction = null;
                }

                // Clear repository cache
                _repositories.Clear();

                // Note: We don't dispose the DbContext here as it's injected
                // and should be managed by the DI container
            }

            _disposed = true;
        }
    }
}
