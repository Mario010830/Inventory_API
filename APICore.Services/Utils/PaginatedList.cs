using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APICore.Services.Utils
{
    public class PaginatedList<T> : List<T>
    {
        public PaginatedList(List<T> items, int count, int page, int perPage)
        {
            Page = page;
            PerPage = perPage;
            TotalPages = (int)Math.Ceiling(count / (double)perPage);
            TotalItems = count;

            this.AddRange(items);
        }

        public int Page { get; private set; }
        public int TotalPages { get; private set; }
        public int TotalItems { get; private set; }
        public int PerPage { get; private set; }

        public bool HasPreviousPage
        {
            get
            {
                return (Page > 1);
            }
        }

        public bool HasNextPage
        {
            get
            {
                return (Page < TotalPages);
            }
        }

        public object GetPaginationData
        {
            get
            {
                return new
                {
                    currentPage = Page,
                    totalPages = TotalPages,
                    totalCount = TotalItems,
                    pageSize = PerPage,
                    hasPreviousPage = HasPreviousPage,
                    hasNextPage = HasNextPage
                };
            }
        }

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int perPage)
        {
            if (pageIndex < 1)
                pageIndex = 1;
            if (perPage < 1)
                perPage = 1;

            var count = source.Count();
            var totalPages = count == 0 ? 0 : (int)Math.Ceiling(count / (double)perPage);
            // Evita result=[] cuando el cliente pide page>totalPages (p. ej. subió pageSize y dejó page=2).
            if (count == 0)
                pageIndex = 1;
            else if (pageIndex > totalPages)
                pageIndex = totalPages;

            var items = source.Skip((pageIndex - 1) * perPage).Take(perPage).ToList();
            return await Task.FromResult(new PaginatedList<T>(items, count, pageIndex, perPage));
        }
    }
}
