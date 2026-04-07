namespace webhook_processing_platform.Application.Dtos.Enums;

public enum IncomingEventType
{
  PaymentCompleted,
  PaymentFailed,
  PaymentPending,
  PaymentRefunded
}
