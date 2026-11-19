using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Service.Features.Categories.Queries.GetCategories;
using Service.Features.Categories.Queries.GetCategoryById;
using Service.Features.Categories.Queries.GetCategoryWithProducts;

namespace API.Controllers
{

    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly ISender _sender;
        public CategoriesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories([FromQuery] GetCategoriesQuery query, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(query, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetCategoryById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetCategoryByIdQuery { Id = id }, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
        }

        [HttpGet("{CategoryId:guid}/products")]
        public async Task<IActionResult> GetWithProducts(Guid CategoryId, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetCategoryWithProductsQuery { Id = CategoryId }, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
        }


        


    }
}
