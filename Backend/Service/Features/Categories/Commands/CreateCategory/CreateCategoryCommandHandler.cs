using Core.Results;
using Domain.Entities;
using MediatR;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Categories.Commands.CreateCategory
{
    public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _UOF;

        public CreateCategoryCommandHandler(IUnitOfWork unitOfWork)
        {
            _UOF = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = new Category(request.Name,request.Description);
            await _UOF.Categories.AddAsync(category, cancellationToken);
            await _UOF.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(category.Id);
        }
    }
}
