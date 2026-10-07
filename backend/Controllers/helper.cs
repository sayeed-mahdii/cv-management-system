namespace backend.Controllers;

public static class PaginationHelper
{
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, int page, int pageSize)
    {
        return query.Skip(Math.Max(0, page - 1) * pageSize).Take(pageSize);
    }
}