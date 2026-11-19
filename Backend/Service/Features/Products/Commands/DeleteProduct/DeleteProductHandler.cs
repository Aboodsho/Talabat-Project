using Core.Results;
using MediatR;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Products.Commands.DeleteProduct
{
    public class DeleteProductHandler : IRequestHandler<DeleteProductCommand, Result>
    {
        private readonly IUnitOfWork _UOF;

        public DeleteProductHandler(IUnitOfWork unitOfWork)
        {
            _UOF=unitOfWork;
        }
        public async  Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            var Product = await _UOF.Products.GetByIdAsync(request.Id, cancellationToken);
            if (Product is null) return Result.Failure(" Product Not Found ");
            Product.Delete();
            _UOF.Products.Update(Product);
            await _UOF.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
