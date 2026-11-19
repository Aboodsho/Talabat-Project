using Core.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Products.Commands.DeleteProduct
{
    public class DeleteProductCommand:IRequest<Result>
    {
        public Guid Id { get; set; }
    }
}
