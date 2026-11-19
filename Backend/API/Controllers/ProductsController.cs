using MediatR;
using Microsoft.AspNetCore.Mvc;
using Service.Features.Products;


namespace API.Controllers
{

    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly ISender _sender;

        public ProductsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery]GetProductsQuery query, CancellationToken ct)
        {
            var result= await _sender.Send(query, ct);
            return result.IsSuccess? Ok(result.Value) : BadRequest(result.Error);

        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id,CancellationToken ct)
        {
            var result= await _sender.Send(new GetProductByIdQuery { Id=id}, ct);
            return result.IsSuccess? Ok(result.Value) : BadRequest(result.Error);

        }

    }
}
