using Core.Results;
using MediatR;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Products.Commands.UpdateProduct
{
    public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, Result>
    {
        private readonly IUnitOfWork _UOF;
        public UpdateProductHandler(IUnitOfWork unitOfWork)
        {
            _UOF = unitOfWork;
        }

        

        public async Task<Result> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            var Product = await _UOF.Products.GetByIdAsync(request.Id, cancellationToken);
            if (Product is null) return Result.Failure(" Product Not Found ");

            Product.SetName(request.Name);
            Product.SetPrice(request.Price);
            Product.UpdateDescription(request.Description);

            _UOF.Products.Update(Product);
            await _UOF.SaveChangesAsync(cancellationToken);
            return Result.Success();

        }
    }
}
