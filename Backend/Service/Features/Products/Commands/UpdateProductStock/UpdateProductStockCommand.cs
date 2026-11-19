using Core.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Products.Commands.UpdateProductStock
{
    public  class UpdateProductStockCommand:IRequest<Result>
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
