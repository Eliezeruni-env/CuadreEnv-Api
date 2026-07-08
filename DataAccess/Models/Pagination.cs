

namespace Onion.Common.Models.Pagination
{
    public class PagedList<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int PageSize { get; set; }
        public int PageCount { get; set; }
        public int TotalItemCount { get; set; }

        public PagedList(IEnumerable<T> items, int pageSize, int pageCount, int totalItemCount)
        {
            Items = items;
            PageSize = pageSize;
            PageCount = pageCount;
            TotalItemCount = totalItemCount;
        }
    }
}