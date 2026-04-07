using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using webhook_processing_platform.Application.Dtos;
using webhook_processing_platform.Application.Handlers;
using webhook_processing_platform.Domain.Models;
using webhook_processing_platform.Infrastructure.Validators;

namespace webhook_processing_platform.Api.Controllers
{
    [Route("webhook")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly ISignatureValidator _signatureValidator;
        private readonly IIncomingEventHandler _incomingEventHandler;
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(ISignatureValidator signatureValidator, IIncomingEventHandler incomingEventHandler, ILogger<WebhookController> logger)
        {
            _signatureValidator = signatureValidator;
            _incomingEventHandler = incomingEventHandler;
            _logger = logger;
        }

        [Route("incoming")]
        [HttpPost]
        public async Task<IActionResult> IncomingEvent([FromHeader(Name = "X-Signature")] string signature)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var payload = await reader.ReadToEndAsync();

            _logger.LogInformation("Webhook Incoming raw payload: {Payload}", payload);
            _logger.LogInformation("Webhook Incoming X-Signature header: {Signature}", signature);

            if (string.IsNullOrWhiteSpace(payload))
                return BadRequest("Invalid event data");

            if (!_signatureValidator.ValidateSignature(payload, signature))
                return Unauthorized("Invalid signature");

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var incomingMessage = JsonSerializer.Deserialize<IncomingMessage>(payload, options);
                if (incomingMessage == null)
                    return BadRequest("Invalid event data");

                await _incomingEventHandler.HandleEventAsync(incomingMessage);
                _logger.LogInformation("Successfully processed incoming webhook event. EventType={EventType}, Source={Source}", incomingMessage.EventType, incomingMessage.Source);
                return Ok("Incoming event processed successfully");
            }
            catch (JsonException jex)
            {
                _logger.LogWarning(jex, "JSON deserialization failed");
                return BadRequest($"Invalid JSON format: {jex.Message}");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid incoming webhook event");
                return BadRequest(ex.Message);
            }
            catch (DuplicatePaymentException ex)
            {
                _logger.LogWarning(ex, "Duplicate webhook event ignored");
                return Conflict("Duplicate event");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception processing webhook");
                return StatusCode(500, "Internal server error");
            }

        }
    }
}
