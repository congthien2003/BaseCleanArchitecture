using BaseCleanArchitecture.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BaseCleanArchitecture.WebAPI.Controllers.Base
{
    [Route("api/[controller]")]
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        public readonly IMediator _mediator;
        public BaseController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Responds with the status code carried by the handler result (200, 201, 400, 401, 409, ...).</summary>
        protected IActionResult ToActionResult(Application.Common.Models.IResult result) =>
            StatusCode(result.StatusCode, result);
    }
}
