using System.Collections.Concurrent;
using webhook_processing_platform.Application.Dtos;
using webhook_processing_platform.Application.Interfaces;

namespace webhook_processing_platform.Infrastructure.Queues;

/// <summary>
/// In-memory implementation of webhook queue using ConcurrentQueue.
/// Suitable for development. For production, use Redis-based implementation.
/// </summary>
public class InMemoryWebhookQueue : IWebhookQueue
{
  private readonly ConcurrentQueue<IncomingMessage> _queue = new();
  private readonly ILogger<InMemoryWebhookQueue> _logger;

  public InMemoryWebhookQueue(ILogger<InMemoryWebhookQueue> logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public Task EnqueueAsync(IncomingMessage message)
  {
    ArgumentNullException.ThrowIfNull(message);

    _queue.Enqueue(message);
    _logger.LogInformation(
        "Webhook queued. EventType={EventType}, Source={Source}, QueueSize={QueueSize}",
        message.EventType, message.Source, _queue.Count);

    return Task.CompletedTask;
  }

  public Task<IncomingMessage?> DequeueAsync()
  {
    if (_queue.TryDequeue(out var message))
    {
      _logger.LogInformation(
          "Webhook dequeued. EventType={EventType}, Source={Source}, RemainingSize={QueueSize}",
          message.EventType, message.Source, _queue.Count);
      return Task.FromResult<IncomingMessage?>(message);
    }

    return Task.FromResult<IncomingMessage?>(null);
  }

  public Task<int> GetQueueSizeAsync()
  {
    return Task.FromResult(_queue.Count);
  }
}
