using System.Text;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using webhook_processing_platform.Application.Dtos;
using webhook_processing_platform.Domain.Models;
using webhook_processing_platform.Application.Interfaces;

namespace webhook_processing_platform.Api.Controllers
{
    [Route("webhook")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly ISignatureValidator _signatureValidator;
        private readonly IWebhookQueue _webhookQueue;
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(ISignatureValidator signatureValidator, IWebhookQueue webhookQueue, ILogger<WebhookController> logger)
        {
            _signatureValidator = signatureValidator;
            _webhookQueue = webhookQueue;
            _logger = logger;
        }

        [Route("incoming")]
        [HttpPost]
        public async Task<IActionResult> IncomingEvent([FromHeader(Name = "X-Signature")] string signature)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var payload = await reader.ReadToEndAsync();

            _logger.LogInformation("Webhook Incoming raw payload: {Payload}", payload);

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

                // Validate the deserialized message
                var validationContext = new ValidationContext(incomingMessage, null, null);
                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(incomingMessage, validationContext, validationResults, validateAllProperties: true))
                {
                    var errorMessages = string.Join("; ", validationResults.Select(v => v.ErrorMessage));
                    _logger.LogWarning("Validation failed for webhook event: {Errors}", errorMessages);
                    return BadRequest($"Validation failed: {errorMessages}");
                }

                // Queue the event for asynchronous processing
                await _webhookQueue.EnqueueAsync(incomingMessage);
                _logger.LogInformation("Successfully queued incoming webhook event. EventType={EventType}, Source={Source}", incomingMessage.EventType, incomingMessage.Source);
                return Accepted("Webhook event queued for processing");
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
