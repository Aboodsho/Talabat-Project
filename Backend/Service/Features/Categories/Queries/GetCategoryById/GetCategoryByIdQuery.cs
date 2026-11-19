using Core.Results;
using MediatR;
using Service.DTOs.Category;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Categories.Queries.GetCategoryById
{
    public class GetCategoryByIdQuery:IRequest<Result<CategortDto>>
    {
        public Guid Id { get; set; }
    }
}
