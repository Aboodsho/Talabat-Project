using Core.Results;
using MediatR;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Products.Commands.UpdateProductCategory
{
    public class UpdateProductCategoryHandler : IRequestHandler<UpdateProductCategoryCommand, Result>
    {

        private readonly IUnitOfWork _UOF;
        public UpdateProductCategoryHandler(IUnitOfWork unitOfWork)
        {
            _UOF = unitOfWork;
        }


        public async Task<Result> Handle(UpdateProductCategoryCommand request, CancellationToken cancellationToken)
        {
            var Product = await _UOF.Products.GetByIdAsync(request.productId, cancellationToken);
            if (Product is null) return Result.Failure(" Product Not Found ");

            if (request.CategoryId.HasValue) Product.AssignCategory(request.CategoryId.Value);
            else Product.RemoveCategory();

            _UOF.Products.Update(Product);
            await _UOF.SaveChangesAsync(cancellationToken);
            return Result.Success();

        }
    }
}
