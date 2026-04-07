namespace webhook_processing_platform.Application.Dtos
{
  public sealed record IncomingMessage
  {
    public string EventType { get; init; } = String.Empty;
    public string Source { get; init; } = String.Empty;
    public DateTime Timestamp { get; init; }
    public required IncomingMessageData Data { get; init; }
  }
}