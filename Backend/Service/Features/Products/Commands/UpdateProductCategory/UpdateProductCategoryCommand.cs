using Core.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Products.Commands.UpdateProductCategory
{
    public class UpdateProductCategoryCommand:IRequest<Result>
    {
        public Guid productId { get; set; }
        public Guid? CategoryId { get; set; }
    }
}
