using Core.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Categories.Commands.CreateCategory
{
    public class CreateCategoryCommand:IRequest<Result<Guid>>
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; } 
    }
}
