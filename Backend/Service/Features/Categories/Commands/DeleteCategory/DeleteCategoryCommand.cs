using Core.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Categories.Commands.DeleteCategory
{
    public class DeleteCategoryCommand:IRequest<Result>
    {
        public Guid Id { get; set; }
    }
}
