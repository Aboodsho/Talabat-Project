using Core.Results;
using MediatR;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Products.Commands.UpdateProductStock
{
    public class UpdateProductStockHandler : IRequestHandler<UpdateProductStockCommand, Result>
    {
        private readonly IUnitOfWork _UOF;

        public UpdateProductStockHandler(IUnitOfWork unitOfWork)
        {
            _UOF = unitOfWork;
        }

        public async Task<Result> Handle(UpdateProductStockCommand request, CancellationToken cancellationToken)
        {
            var product= await _UOF.Products.GetByIdAsync(request.ProductId,cancellationToken);
            if (product is null) return Result.Failure("Product Not Found");

            product.SetStock(request.Quantity);

            _UOF.Products.Update(product);
            await _UOF.SaveChangesAsync(cancellationToken);
            return Result.Success();


        }
    }
}
