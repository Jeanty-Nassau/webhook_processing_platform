namespace webhook_processing_platform.Domain.Models;

public class DuplicatePaymentException : Exception
{
  public DuplicatePaymentException(string paymentId)
      : base($"Payment with paymentId '{paymentId}' already exists.")
  {
  }
}