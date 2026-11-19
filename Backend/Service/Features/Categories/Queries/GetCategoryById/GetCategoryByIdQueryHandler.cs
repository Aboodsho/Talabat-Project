using Core.Results;
using MediatR;
using Service.DTOs.Category;
using Service.Interfaces;

namespace Service.Features.Categories.Queries.GetCategoryById
{
    public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, Result<CategortDto>>
    {
        private readonly IUnitOfWork _UOF;

        public GetCategoryByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _UOF = unitOfWork;
        }

        public async Task<Result<CategortDto>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            var category =  await _UOF.Categories.GetByIdAsync(request.Id);
            if(category is null) return Result<CategortDto>.Failure("Category not found");

            var dto=new CategortDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            };

            return Result<CategortDto>.Success(dto);
        }
    }
}
