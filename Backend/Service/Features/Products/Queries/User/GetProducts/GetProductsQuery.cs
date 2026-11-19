using Core.Pagination;
using Core.Results;
using MediatR;
using Service.DTOs.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Products.Queries.User.GetProducts
{
    public class GetProductsQuery:IRequest<Result<PagedResult<ProductDto>>>
    {
        public Guid? CategoryId { get; set; }
        public string? SearchTerm { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
