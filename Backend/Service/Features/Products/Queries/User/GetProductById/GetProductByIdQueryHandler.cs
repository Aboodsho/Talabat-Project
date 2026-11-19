using Core.Results;
using Domain.Entities;
using MediatR;
using Service.DTOs.Products;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Products.Queries.User.GetProductById
{
    public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDto>>
    {
        private readonly IUnitOfWork _UOF;

        public GetProductByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _UOF = unitOfWork;
        }


        public async Task<Result<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            var product = await _UOF.Products.GetWithImagesAsync(request.Id, cancellationToken);
            if (product is null || !product.IsActive)
                return Result<ProductDto>.Failure("Product not found.");
            var dto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                CategoryName = product.Category?.Name,
                Images = product.Images.Select(i => i.ImageUrl).ToList(),
                IsAvailable = product.StockQuantity > 0

            };
            return Result<ProductDto>.Success(dto);

        }
    }
}
