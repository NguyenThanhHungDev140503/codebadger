using Microsoft.EntityFrameworkCore;

namespace Application.Common.Models
{
    public class PaginatedResponse<T>(IList<T> items, int count, int pageNumber = 1, int pageSize = 10)
    {
        public IList<T> Items { get; } = items;
        public int PageNumber { get; set; } = pageNumber;
        public int TotalPages { get; set; } = (int)Math.Ceiling(count / (double)pageSize);
        public int TotalCount { get; set; } = count;

        public bool HasPreviousPage => PageNumber > 1;

        public bool HasNextPage => PageNumber < TotalPages;

        public static async Task<PaginatedResponse<T>> CreateAsync(IQueryable<T> source, int pageNumber = 1, int pageSize = 10)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 1;

            int count = await source.CountAsync();
            var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedResponse<T>(items, count, pageNumber, pageSize);
        }

        public static PaginatedResponse<T> Create(IEnumerable<T> sources, int pageNumber, int pageSize)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 1;

            var count = sources.Count();
            var items = sources.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            return new PaginatedResponse<T>(items, count, pageNumber, pageSize);
        }
    }
}
