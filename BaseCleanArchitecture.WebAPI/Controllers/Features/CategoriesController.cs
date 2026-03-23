using BaseCleanArchitecture.Application.Features.Categories.Commands;
using BaseCleanArchitecture.Application.Features.Categories.Models;
using BaseCleanArchitecture.WebAPI.Controllers.Base;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BaseCleanArchitecture.WebAPI.Controllers.Features
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : BaseController
    {
        public CategoriesController(IMediator mediator) : base(mediator)
        {
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategoryAsync([FromBody] CreateCategoryRequest createCategoryRequest)
        {
            var command = new CreateCategoryCommandRequest(createCategoryRequest);
            var response = await this._mediator.Send(command);
            return Ok(response);
        }
    }
}
