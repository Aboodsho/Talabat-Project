using Core.Pagination;
using Core.Results;
using Domain.Entities;
using MediatR;
using Service.DTOs.Products;
using Service.Filters;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Products.Queries.User.GetProducts
{
    public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery,Result<PagedResult<ProductDto>>>
    {
        private readonly IUnitOfWork _UOF;

        public GetProductsQueryHandler(IUnitOfWork unitOfWork)
        {
            _UOF = unitOfWork;
        }

       

        public async Task<Result<PagedResult<ProductDto>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
        {
            var filter = new ProdcutFilter
            {
                CategoryId = request.CategoryId,
                SearchTerm = request.SearchTerm,
                MinPrice = request.MinPrice,
                MaxPrice = request.MaxPrice,
                IsActive=true,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            var pagedproducts= await _UOF.Products.GetFilteredAsync(filter, cancellationToken);

            var dtoItems= pagedproducts.Items.Select(p=>new ProductDto
            { 
                Id = p.Id,
                Name=p.Name,
                Description=p.Description,
                Price = p.Price,
                CategoryName=p.Category?.Name,
                Images=p.Images.Select(i=>i.ImageUrl).ToList(),
                IsAvailable=p.StockQuantity>0,
            }).ToList();

            var result = new PagedResult<ProductDto>(dtoItems, pagedproducts.PageSize, pagedproducts.PageNumber, pagedproducts.TotalCount);
            return Result<PagedResult<ProductDto>>.Success(result);

        }
    }
}


// i need to return list of productdto so i made generic pagedresult(pagination) 
// there method in product repo is getfilterd that return generic pagedresult(pagination)
//            return new PagedResult<Product>(items, filter.PageNumber, filter.PageSize, totalCount);
// so i hold the return and get items from it and convert them into dto
// and finally return =>   var result = new PagedResult<ProductDto>(dtoItems, pagedproducts.PageSize, pagedproducts.PageNumber, pagedproducts.TotalCount);
