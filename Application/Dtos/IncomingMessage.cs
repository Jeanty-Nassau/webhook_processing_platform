using System.ComponentModel.DataAnnotations;

namespace webhook_processing_platform.Application.Dtos
{
  public sealed record IncomingMessage
  {
    [Required(ErrorMessage = "EventType is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "EventType must be between 1 and 100 characters")]
    public string EventType { get; init; } = String.Empty;

    [Required(ErrorMessage = "Source is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Source must be between 1 and 100 characters")]
    public string Source { get; init; } = String.Empty;

    [Required(ErrorMessage = "Timestamp is required")]
    public DateTime Timestamp { get; init; }

    [Required(ErrorMessage = "Data is required")]
    public required IncomingMessageData Data { get; init; }
  }
}