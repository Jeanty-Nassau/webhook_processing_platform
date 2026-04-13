using webhook_processing_platform.Application.Dtos;
using webhook_processing_platform.Application.Interfaces;
using webhook_processing_platform.Application.Mappers;
using webhook_processing_platform.Domain.Models;

namespace webhook_processing_platform.Application.Handlers;

public class IncomingEventHandler : IIncomingEventHandler
{
  private readonly IPaymentRepository _paymentRepository;
  private readonly IncomingEventToPaymentMapper _mapper;
  private readonly ILogger<IncomingEventHandler> _logger;

  public IncomingEventHandler(IPaymentRepository paymentRepository, IncomingEventToPaymentMapper mapper, ILogger<IncomingEventHandler> logger)
  {
    _paymentRepository = paymentRepository;
    _mapper = mapper;
    _logger = logger;
  }

  public async Task HandleEventAsync(IncomingMessage incomingEvent)
  {
    if (incomingEvent == null)
      throw new ArgumentNullException(nameof(incomingEvent));

    // Only process payment completed events
    if (incomingEvent.EventType != "payment.completed")
      throw new ArgumentException($"Unsupported event type: {incomingEvent.EventType}");

    var payment = _mapper.MapToPayment(incomingEvent);

    try
    {
      var response = await _paymentRepository.SavePaymentAsync(payment);
      if (response == Guid.Empty)
      {
        _logger.LogError("Failed to save payment to database for paymentId={PaymentId}", payment.PaymentId);
        throw new InvalidOperationException("Failed to save payment to database.");
      }

      _logger.LogInformation("Payment event handled for paymentId={PaymentId}", payment.PaymentId);
    }
    catch (DuplicatePaymentException ex)
    {
      _logger.LogWarning(ex, "Duplicate payment event ignored for paymentId={PaymentId}", payment.PaymentId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error handling payment event for paymentId={PaymentId}", payment.PaymentId);
      throw;
    }
  }
}

