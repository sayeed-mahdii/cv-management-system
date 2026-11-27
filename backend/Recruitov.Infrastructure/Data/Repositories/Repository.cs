using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Recruitov.Application.Interfaces;

namespace Recruitov.Infrastructure.Data.Repositories;

public class Repository<T>(AppDbContext dbContext) : IRepository<T> where T : class
{
    private readonly DbSet<T> _dbSet = dbContext.Set<T>();

    public async Task<T?> GetByIdAsync(object id, bool trackChanges = false, CancellationToken ct = default)
    {
        var entity = await _dbSet.FindAsync([id], ct);
        if (entity is not null && !trackChanges)
            dbContext.Entry(entity).State = EntityState.Detached;

        return entity;
    }

    public async Task<T?> FindFirstAsync(
        Expression<Func<T, bool>> predicate,
        bool trackChanges = false,
        CancellationToken ct = default)
    {
        IQueryable<T> query = trackChanges ? _dbSet : _dbSet.AsNoTracking();
        return await query.FirstOrDefaultAsync(predicate, ct);
    }

    public async Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        bool trackChanges = false,
        CancellationToken ct = default)
    {
        IQueryable<T> query = trackChanges ? _dbSet : _dbSet.AsNoTracking();
        return await query.Where(predicate).ToListAsync(ct);
    }

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        _dbSet.AsNoTracking().AnyAsync(predicate, ct);

    public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default) =>
        predicate is null
            ? _dbSet.AsNoTracking().CountAsync(ct)
            : _dbSet.AsNoTracking().CountAsync(predicate, ct);

    public async Task<(IReadOnlyList<T> Items, int TotalCount)> GetPagedAsync(
        Expression<Func<T, bool>>? predicate,
        int page,
        int pageSize,
        bool trackChanges = false,
        CancellationToken ct = default)
    {
        IQueryable<T> query = trackChanges ? _dbSet : _dbSet.AsNoTracking();
        if (predicate is not null)
            query = query.Where(predicate);

        var count = await query.CountAsync(ct);
        var safePage = Math.Max(1, page);
        var safeSize = Math.Clamp(pageSize, 1, 100);

        var items = await query
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .ToListAsync(ct);

        return (items, count);
    }

    public async Task AddAsync(T entity, CancellationToken ct = default) =>
        await _dbSet.AddAsync(entity, ct);

    public void Update(T entity) => _dbSet.Update(entity);
    public void Remove(T entity) => _dbSet.Remove(entity);
    public void RemoveRange(IEnumerable<T> entities) => _dbSet.RemoveRange(entities);
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => dbContext.SaveChangesAsync(ct);
}
