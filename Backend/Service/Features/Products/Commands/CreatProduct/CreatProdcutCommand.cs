using Core.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Results;

namespace Service.Features.Products.Commands.CreatProduct
{
    public class CreateProdcutCommand:IRequest<Result<Guid>>
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public Guid? CategoryId { get; set; }


    }
}
