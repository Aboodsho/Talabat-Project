using Core.Results;
using MediatR;
using Service.DTOs.Category;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Categories.Queries.GetCategoryWithProducts
{
    public class GetCategoryWithProductsQuery:IRequest<Result<CategoryWithProductsDto>>
    {
        public Guid Id { get; set; }
    }
}
