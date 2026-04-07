namespace webhook_processing_platform.Domain.Models
{
  public class Payment
  {
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PaymentId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime ProcessedAt { get; set; }
    public string Status { get; set; } = "Pending";
  }
}