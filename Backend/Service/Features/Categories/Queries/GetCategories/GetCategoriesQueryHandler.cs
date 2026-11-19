using Core.Pagination;
using Core.Results;
using MediatR;
using Service.DTOs.Category;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Categories.Queries.GetCategories
{
    public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, Result<PagedResult<CategortDto>>>
    {
        private readonly IUnitOfWork _UOF;
        public GetCategoriesQueryHandler(IUnitOfWork unitOfWork)
        {
            _UOF=unitOfWork;
        }
        public async Task<Result<PagedResult<CategortDto>>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
        {
            var categories = await _UOF.Categories.GetAllAsync(cancellationToken);
            var dto=categories.Select(c=> new CategortDto { Id=c.Id,Name=c.Name,Description=c.Description})
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var result = new PagedResult<CategortDto>(dto, request.PageNumber, request.PageSize, categories.Count);
            return Result<PagedResult<CategortDto>>.Success(result);

        }

    }
}
