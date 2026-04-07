using webhook_processing_platform.Application.Dtos;
using webhook_processing_platform.Domain.Models;

namespace webhook_processing_platform.Application.Mappers;

public class IncomingEventToPaymentMapper
{
  public Payment MapToPayment(IncomingMessage incomingEvent)
  {
    if (incomingEvent == null)
      throw new ArgumentNullException(nameof(incomingEvent));

    return new Payment
    {
      PaymentId = incomingEvent.Data.PaymentId,
      OrderId = incomingEvent.Data.OrderId,
      Amount = (decimal)incomingEvent.Data.Amount,
      Currency = incomingEvent.Data.Currency,
      EventType = incomingEvent.EventType,
      Source = incomingEvent.Source,
      ReceivedAt = incomingEvent.Timestamp,
      Status = "Pending"
    };
  }
}
