using Core.Pagination;
using Core.Results;
using MediatR;
using Service.DTOs.Category;

namespace Service.Features.Categories.Queries.GetAllCategoriesIncludingDeleted
{
    public class GetAllCategoriesIncludingDeletedQuery: IRequest<Result<PagedResult<AdminCategoryDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
