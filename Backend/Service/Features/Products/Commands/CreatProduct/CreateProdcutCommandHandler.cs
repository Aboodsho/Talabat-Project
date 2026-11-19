using Core.Results;
using Domain.Entities;
using MediatR;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Products.Commands.CreatProduct
{
    public class CreateProdcutCommandHandler : IRequestHandler<CreateProdcutCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateProdcutCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<Result<Guid>> Handle(CreateProdcutCommand request, CancellationToken cancellationToken)
        {
            var product = new Product(request.Name,request.Description,request.Price,request.StockQuantity,request.CategoryId);

            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(product.Id);
        }
    }
}
