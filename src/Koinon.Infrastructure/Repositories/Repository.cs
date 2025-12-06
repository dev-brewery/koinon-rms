using System.Linq.Expressions;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Koinon.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation for data access operations.
/// Uses Entity Framework Core with AsNoTracking by default for read operations.
/// </summary>
/// <typeparam name="T">The entity type, must inherit from Entity.</typeparam>
public class Repository<T>(KoinonDbContext context) : IRepository<T> where T : Entity
{
    private readonly KoinonDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly DbSet<T> _dbSet = context.Set<T>();

    /// <inheritdoc />
    public async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    /// <inheritdoc />
    public async Task<T?> GetByIdKeyAsync(string idKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idKey))
        {
            return null;
        }

        // Decode the IdKey to get the integer ID
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return null;
        }

        return await GetByIdAsync(id, ct);
    }

    /// <inheritdoc />
    public async Task<T?> GetByGuidAsync(Guid guid, CancellationToken ct = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Guid == guid, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbSet
            .AsNoTracking()
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(predicate)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public IQueryable<T> Query()
    {
        return _dbSet.AsNoTracking();
    }

    /// <inheritdoc />
    public async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        // Set creation timestamp
        entity.CreatedDateTime = DateTime.UtcNow;

        await _dbSet.AddAsync(entity, ct);
        return entity;
    }

    /// <inheritdoc />
    public Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        // Set modification timestamp
        entity.ModifiedDateTime = DateTime.UtcNow;

        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(int id, CancellationToken ct = default)
    {
        return await _dbSet
            .AsNoTracking()
            .AnyAsync(e => e.Id == id, ct);
    }

    /// <inheritdoc />
    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        return await _dbSet.CountAsync(ct);
    }
}
