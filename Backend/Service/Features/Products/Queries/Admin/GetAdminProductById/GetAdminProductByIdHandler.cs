using Core.Results;
using MediatR;
using Service.DTOs.Products;
using Service.Interfaces;

namespace Service.Features.Products.Queries.Admin.GetAdminProductById
{
    public class GetAdminProductByIdHandler : IRequestHandler<GetAdminProductByIdQuery, Result<AdminProdcutDto>>
    {
        public IUnitOfWork _UOF { get; }
        public GetAdminProductByIdHandler(IUnitOfWork unitOfWork)
        {
            _UOF = unitOfWork;
        }

        public async Task<Result<AdminProdcutDto>> Handle(GetAdminProductByIdQuery request, CancellationToken cancellationToken)
        {
            var product = await _UOF.Products.GetWithImagesAsync(request.Id,cancellationToken);
            if(product is null) return Result<AdminProdcutDto>.Failure("Product not found");

            var dto=new AdminProdcutDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name,
                Images = product.Images.Select(i => i.ImageUrl).ToList(),
                StockQuantity = product.StockQuantity,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
            };

            return Result<AdminProdcutDto>.Success(dto);

        }
    }
}
