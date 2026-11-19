using Core.Pagination;
using Core.Results;
using MediatR;
using Service.DTOs.Category;
using Service.Interfaces;

namespace Service.Features.Categories.Queries.GetAllCategoriesIncludingDeleted
{
    public class GetAllCategoriesIncludingDeletedQueryHandler : IRequestHandler<GetAllCategoriesIncludingDeletedQuery, Result<PagedResult<AdminCategoryDto>>>
    {

        private readonly IUnitOfWork _UOF;

        public GetAllCategoriesIncludingDeletedQueryHandler(IUnitOfWork unitOfWork)
        {
            _UOF = unitOfWork;
        }

        public async Task<Result<PagedResult<AdminCategoryDto>>> Handle
            (GetAllCategoriesIncludingDeletedQuery request, CancellationToken cancellationToken)
        {
            var categories = await _UOF.Categories.GetAllIncludingDeletedAsync(cancellationToken);

            var dtoItems = categories
            .Select(c => new AdminCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsDeleted = c.IsDeleted,
                DeletedAt = c.DeletedAt
            })
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();


            var result = new PagedResult<AdminCategoryDto>(dtoItems,  request.PageSize, request.PageNumber, categories.Count);

            return Result<PagedResult<AdminCategoryDto>>.Success(result);


        }
    }
}
