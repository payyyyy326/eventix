using Eventix.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Eventix.Extensions;

public static class IQueryableExtensions
{
    public static async Task<PaginationResponse<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int page = 1,
        int pageSize = 10)
        where T : class
    {
        if (page <= 0)
            page = 1;

        if (pageSize <= 0)
            pageSize = 10;

        var totalRows = await query.CountAsync();

        var totalPages = (int)Math.Ceiling(
            (double)totalRows / pageSize);

        var skip = (page - 1) * pageSize;

        var data = await query
            .AsNoTracking()
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        return new PaginationResponse<T>
        {
            DataList = data,
            TotalRows = totalRows,
            TotalPages = totalPages,
            CurrentPage = page,
            PageSize = pageSize
        };
    }
}