using BaseCleanArchitecture.Application.Features.Webhooks.Commands;
using BaseCleanArchitecture.WebAPI.Controllers.Base;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BaseCleanArchitecture.WebAPI.Controllers.Features
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhooksController : BaseController
    {
        public WebhooksController(IMediator mediator) : base(mediator)
        {
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok("Webhook is healthy.");
        }

        [HttpPost("payos/received")]
        public async Task<IActionResult> PayosReceived([FromBody] object payload)
        {
            // Handle the webhook payload here
            // You can deserialize the payload into a specific model if needed
            // For demonstration purposes, we'll just return a success response
            await _mediator.Send(new ReceivedPayosWebhookCommandRequest()
            {
                Code = "00",
                Message = "success",
                Data = payload
            });
            return Ok(new { message = "Webhook received successfully.", payload });
        }
    }
}
