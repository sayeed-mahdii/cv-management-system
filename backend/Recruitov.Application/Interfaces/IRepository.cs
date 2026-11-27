using System.Linq.Expressions;

namespace Recruitov.Application.Interfaces;

/// <summary>
/// Generic repository contract (Dukaan Clean Architecture style) decoupling Application from EF Core.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(object id, bool trackChanges = false, CancellationToken ct = default);

    Task<T?> FindFirstAsync(
        Expression<Func<T, bool>> predicate,
        bool trackChanges = false,
        CancellationToken ct = default);

    Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        bool trackChanges = false,
        CancellationToken ct = default);

    Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default);

    Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken ct = default);

    Task<(IReadOnlyList<T> Items, int TotalCount)> GetPagedAsync(
        Expression<Func<T, bool>>? predicate,
        int page,
        int pageSize,
        bool trackChanges = false,
        CancellationToken ct = default);

    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
