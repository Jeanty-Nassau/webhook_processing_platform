namespace webhook_processing_platform.Application.Dtos
{
  public sealed record IncomingMessageData
  {
    public string PaymentId { get; init; } = String.Empty;
    public string OrderId { get; init; } = String.Empty;
    public double Amount { get; init; }
    public string Currency { get; init; } = String.Empty;
  }
}
