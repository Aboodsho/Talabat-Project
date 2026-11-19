using Core.Results;
using MediatR;
using Service.DTOs.Category;
using Service.DTOs.Products;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Categories.Queries.GetCategoryWithProducts
{
    public class GetCategoryWithProductsQueryHandler : IRequestHandler<GetCategoryWithProductsQuery, Result<CategoryWithProductsDto>>
    {
        private readonly IUnitOfWork _UOf;

        public GetCategoryWithProductsQueryHandler(IUnitOfWork unitOfWork)
        {
            _UOf = unitOfWork;
        }

        public async Task<Result<CategoryWithProductsDto>> Handle(GetCategoryWithProductsQuery request, CancellationToken cancellationToken)
        {
           var (category,products)= await _UOf.Categories.GetWithProductsAsync(request.Id,cancellationToken);
           if(category is null) return Result<CategoryWithProductsDto>.Failure("Category not found");

           var dto = new CategoryWithProductsDto
           {
               Id=category.Id,
               Name=category.Name,
               Description=category.Description,
               Products=products
                    .Where(p=>p.IsActive)
                    .Select(p=>new ProductDto
                    {
                        Id=p.Id,
                        Name=p.Name,
                        Description=p.Description,
                        Price=p.Price,
                        CategoryName=category.Name,
                        Images=p.Images.Select(i=>i.ImageUrl).ToList(),
                        IsAvailable=p.StockQuantity>0
                    }).ToList()

           };

            return Result<CategoryWithProductsDto>.Success(dto);



        }
    }
}
