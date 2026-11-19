using Core.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Service.Features.Categories.Commands.UpdateCategory
{
    public class UpdateCategoryCommand:IRequest<Result>
    {
        [JsonIgnore]
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; } 
    }
}
