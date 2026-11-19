using Core.Results;
using MediatR;
using Service.DTOs.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Products.Queries.User.GetProductById
{
    public class GetProductByIdQuery:IRequest<Result<ProductDto>>
    {
        public Guid Id { get; set; }
    }
}
