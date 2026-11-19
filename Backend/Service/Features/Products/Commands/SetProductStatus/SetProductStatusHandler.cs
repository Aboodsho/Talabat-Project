using Core.Results;
using MediatR;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Products.Commands.SetProductStatus
{
    public class SetProductStatusHandler : IRequestHandler<SetProductStatusCommand, Result>
    {
        private readonly IUnitOfWork _UOF;
        public SetProductStatusHandler(IUnitOfWork unitOfWork)
        {
            _UOF = unitOfWork;
        }
       
        public async Task<Result> Handle(SetProductStatusCommand request, CancellationToken cancellationToken)
        {
            var product = await _UOF.Products.GetByIdAsync(request.ProductId, cancellationToken);
            if (product is null)  return Result.Failure("Product not found.");

            if(request.IsActive)product.Activate();
            else product.Deactivate();

            _UOF.Products.Update(product);
            await _UOF.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
