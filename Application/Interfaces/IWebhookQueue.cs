using webhook_processing_platform.Application.Dtos;

namespace webhook_processing_platform.Application.Interfaces;

/// <summary>
/// Abstraction for webhook event queuing.
/// Allows switching between in-memory (development) and Redis (production).
/// </summary>
public interface IWebhookQueue
{
  /// <summary>
  /// Enqueue an incoming webhook event for asynchronous processing.
  /// </summary>
  /// <param name="message">The webhook message to process</param>
  Task EnqueueAsync(IncomingMessage message);

  /// <summary>
  /// Dequeue the next webhook event for processing.
  /// Returns null if queue is empty.
  /// </summary>
  Task<IncomingMessage?> DequeueAsync();

  /// <summary>
  /// Get the current queue size (for monitoring).
  /// </summary>
  Task<int> GetQueueSizeAsync();
}
