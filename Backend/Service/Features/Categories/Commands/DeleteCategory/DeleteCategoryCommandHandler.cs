using Core.Results;
using MediatR;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Categories.Commands.DeleteCategory
{
    public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, Result>
    {
        private readonly IUnitOfWork _UOF;

        public DeleteCategoryCommandHandler(IUnitOfWork unitOfWork)
        {
            _UOF = unitOfWork;
        }

        public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _UOF.Categories.GetByIdAsync(request.Id);
            if (category == null)
            {
                return Result.Failure("Category not found.");
            }
            category.Delete();
            await _UOF.SaveChangesAsync();
            return Result.Success();

        }
    }
}
