using Core.Pagination;
using Core.Results;
using MediatR;
using Service.DTOs.Category;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Categories.Queries.GetCategories
{
    public class GetCategoriesQuery:IRequest<Result<PagedResult<CategortDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
