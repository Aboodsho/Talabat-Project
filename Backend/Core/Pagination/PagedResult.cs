using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Pagination
{
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; }
        public int PageSize { get; }
        public int PageNumber { get; }
        public int TotalCount { get; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

        public PagedResult(IReadOnlyList<T> items, int pageSize, int pageNumber, int totalCount)
        {
            Items = items;
            PageSize = pageSize;
            PageNumber = pageNumber;
            TotalCount = totalCount;
        }


    }
}
