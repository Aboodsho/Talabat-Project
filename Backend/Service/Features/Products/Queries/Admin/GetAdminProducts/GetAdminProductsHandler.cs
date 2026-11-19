using Core.Pagination;
using Core.Results;
using MediatR;
using Service.DTOs.Products;
using Service.Filters;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Products.Queries.Admin.GetAdminProducts
{
    public class GetAdminProductsHandler : IRequestHandler<GetAdminProductsQuery, Result<PagedResult<AdminProdcutDto>>>
    {
        public IUnitOfWork _UOF { get; }
        public GetAdminProductsHandler(IUnitOfWork uniteOfWOrk)
        {
            _UOF = uniteOfWOrk;
        }

        
        public async Task<Result<PagedResult<AdminProdcutDto>>> Handle(GetAdminProductsQuery request, CancellationToken cancellationToken)
        {
            var filter=new ProdcutFilter
            {
                CategoryId = request.CategoryId,
                SearchTerm = request.SearchTerm,
                MinPrice = request.MinPrice,
                MaxPrice = request.MaxPrice,
                PageNumber=request.PageNumber,
                PageSize=request.PageSize,
                IsActive=request.IsActive
            };

            var pagedProduts = await _UOF.Products.GetFilteredAsync(filter, cancellationToken);

            var dtoItems= pagedProduts.Items.Select(p => new AdminProdcutDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name,
                Images = p.Images.Select(i => i.ImageUrl).ToList(),
                IsActive = p.IsActive,
                StockQuantity= p.StockQuantity,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList();

            var result = new PagedResult<AdminProdcutDto>(dtoItems, pagedProduts.PageNumber, pagedProduts.PageSize, pagedProduts.TotalCount);

            return Result<PagedResult<AdminProdcutDto>>.Success(result);
        }
    }
}
