using System.ComponentModel.DataAnnotations;

namespace webhook_processing_platform.Application.Dtos
{
  public sealed record IncomingMessageData
  {
    [Required(ErrorMessage = "PaymentId is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "PaymentId must be between 1 and 255 characters")]
    public string PaymentId { get; init; } = String.Empty;

    [Required(ErrorMessage = "OrderId is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "OrderId must be between 1 and 255 characters")]
    public string OrderId { get; init; } = String.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public double Amount { get; init; }

    [Required(ErrorMessage = "Currency is required")]
    [StringLength(10, MinimumLength = 1, ErrorMessage = "Currency must be between 1 and 10 characters")]
    public string Currency { get; init; } = String.Empty;
  }
}
