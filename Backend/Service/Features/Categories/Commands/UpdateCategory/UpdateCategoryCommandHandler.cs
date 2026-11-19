using Core.Results;
using MediatR;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Categories.Commands.UpdateCategory
{
    public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Result>
    {
        private readonly IUnitOfWork _UOF;

        public UpdateCategoryCommandHandler(IUnitOfWork unitOfWork)
        {
            _UOF = unitOfWork;
        }

        public async Task<Result> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            var category =  await _UOF.Categories.GetByIdAsync(request.Id, cancellationToken);
            if(category is null) return Result.Failure("Category not found.");

            category.SetName(request.Name);
            category.updateDescription(request.Description);

            _UOF.Categories.Update(category);
            await _UOF.SaveChangesAsync(cancellationToken);
             return Result.Success();


        }
    }
}
