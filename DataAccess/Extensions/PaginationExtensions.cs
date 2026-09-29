using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Onion.Common.Models.Pagination;

namespace Onion.DataAccess.Extensions
{
    public static class PaginationExtensions
    {
        public static async Task<PagedList<T>> ToPagedListAsync<T>(this IQueryable<T> source, int pageNumber, int pageSize, CancellationToken ct = default)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            // If the provider does not support async (IAsyncQueryProvider), it's an in-memory LINQ provider
            // (e.g. Enumerable.AsQueryable()). In that case fall back to synchronous enumeration to avoid
            // the InvalidOperationException: provider doesn't implement IAsyncQueryProvider.
            if (source.Provider is not Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider)
            {
                var list = source.AsEnumerable().ToList();
                var total = list.Count;
                var pageCount = (int)Math.Ceiling(total / (double)pageSize);
                var items = list.Skip((pageNumber - 1) * pageSize).Take(pageSize);
                return new PagedList<T>(items, pageSize, pageCount, total);
            }

            var totalAsync = await source.CountAsync(ct);
            var pageCountAsync = (int)Math.Ceiling(totalAsync / (double)pageSize);
            var itemsAsync = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            return new PagedList<T>(itemsAsync, pageSize, pageCountAsync, totalAsync);
        }

        public static PagedList<T> ToPagedList<T>(this IEnumerable<T> source, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var list = source is IList<T> l ? l : source.ToList();
            var total = list.Count;
            var pageCount = (int)Math.Ceiling(total / (double)pageSize);
            var items = list.Skip((pageNumber - 1) * pageSize).Take(pageSize);
            return new PagedList<T>(items, pageSize, pageCount, total);
        }
    }
}
