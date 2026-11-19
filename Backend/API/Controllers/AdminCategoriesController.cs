using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Features.Categories.Commands.CreateCategory;
using Service.Features.Categories.Commands.DeleteCategory;
using Service.Features.Categories.Commands.UpdateCategory;
using Core.Constants;
using Service.Features.Categories.Queries.GetAllCategoriesIncludingDeleted;

namespace API.Controllers
{

    [ApiController]
    [Authorize(Roles = Role.Admin)]
    [Route("api/admin/categories")]
    public class AdminCategoriesController: ControllerBase
    {
        private readonly ISender _sender;

        public AdminCategoriesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllIncludingDeleted(
        [FromQuery] GetAllCategoriesIncludingDeletedQuery query, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(query, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
        }


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryCommand command, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(command, cancellationToken);
            return result.IsSuccess
                ? CreatedAtAction(nameof(GetAllIncludingDeleted), new { id = result.Value }, result.Value)
                : BadRequest(result.Error);
        }

        [HttpPut("{CategoryId:guid}")]
        public async Task<IActionResult> Update(Guid CategoryId, [FromBody] UpdateCategoryCommand command, CancellationToken cancellationToken)
        {
            command.Id = CategoryId;
            var result = await _sender.Send(command, cancellationToken);
            return result.IsSuccess ? NoContent() : BadRequest(result.Error);
        }

        [HttpDelete("{CategoryId:guid}")]
        public async Task<IActionResult> Delete(Guid CategoryId, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new DeleteCategoryCommand { Id = CategoryId }, cancellationToken);
            return result.IsSuccess ? NoContent() : BadRequest(result.Error);
        }
    }
}
