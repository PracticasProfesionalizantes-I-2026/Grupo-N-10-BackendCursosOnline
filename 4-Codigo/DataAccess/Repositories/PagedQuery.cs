using Lumen.Shared.DTOs;

namespace Lumen.DataAccess.Repositories;

internal static class PagedQuery
{
    internal static IQueryable<T> WithPage<T>(this IOrderedQueryable<T> query, ListPage? page) =>
        page is null ? query : query.Skip(page.Offset).Take(page.Limit);
}
