using Core.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{

    [ApiController]
    [Authorize(Roles = Role.Admin)]
    [Route("api/admin/products")]
    public class AdminProductsController:ControllerBase
    {
        private readonly ISender _sender;
        public AdminProductsController(ISender sender)
        {
            _sender = sender;
        }


        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] GetAdminProductsQuery query, CancellationToken ct)
        {
            var result = await _sender.Send(query, ct);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var result= await _sender.Send(new GetAdminProductByIdQuery {Id=id }, ct);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
        }


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProdcutCommand command, CancellationToken ct)
        {
            var result = await _sender.Send(command, ct);
            return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
                : BadRequest(result.Error);
        }

        [HttpPut("{productId:guid}")]
        public async Task<IActionResult> Update(Guid productId, [FromBody] UpdateProductCommand command, CancellationToken ct)
        {
            //if (id != command.Id)
            //{
            //    return BadRequest("Id in the URL does not match Id in the request body.");
            //}
            command.Id = productId; // Ensure the command has the correct Id from the URL
            var result = await _sender.Send(command, ct);
            return result.IsSuccess ? NoContent() : BadRequest(result.Error);
        }

        [HttpPatch("{productId:guid}/category")]
        public async Task<IActionResult> UpdateCategory(Guid productId, [FromBody] Guid? categoryId, CancellationToken ct)
        {
            var command = new UpdateProductCategoryCommand { productId = productId, CategoryId = categoryId };
            var result = await _sender.Send(command, ct);
            return result.IsSuccess ? NoContent() : BadRequest(result.Error);
        }

        [HttpPatch("{productId:guid}/stock")]
        public async Task<IActionResult> UpdateStock(Guid productId, [FromBody] int stock, CancellationToken ct)
        {
            var command = new UpdateProductStockCommand { ProductId = productId, Quantity = stock };
            var result = await _sender.Send(command, ct);
            return result.IsSuccess ? NoContent() : BadRequest(result.Error);
        }

        [HttpPatch("{productId:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid productId, [FromBody] bool isActive, CancellationToken ct)
        {
            var command = new SetProductStatusCommand { ProductId = productId, IsActive = isActive };
            var result = await _sender.Send(command, ct);
            return result.IsSuccess ? NoContent() : BadRequest(result.Error);
        }

        [HttpDelete("{productId:guid}")]
        public async Task<IActionResult> Delete(Guid productId, CancellationToken ct)
        {
            var result = await _sender.Send(new DeleteProductCommand { Id = productId }, ct);
            return result.IsSuccess ? NoContent() : BadRequest(result.Error);
        }


    }
}
