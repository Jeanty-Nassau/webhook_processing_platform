using webhook_processing_platform.Application.Interfaces;

namespace webhook_processing_platform.Application.Handlers;

/// <summary>
/// Background service that continuously processes webhook events from the queue.
/// Runs as a long-lived service in the application during the entire lifetime of the app.
/// </summary>
public class WebhookProcessingBackgroundService : BackgroundService
{
  private readonly IWebhookQueue _queue;
  private readonly IIncomingEventHandler _eventHandler;
  private readonly ILogger<WebhookProcessingBackgroundService> _logger;
  private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(1);

  public WebhookProcessingBackgroundService(
      IWebhookQueue queue,
      IIncomingEventHandler eventHandler,
      ILogger<WebhookProcessingBackgroundService> logger)
  {
    _queue = queue ?? throw new ArgumentNullException(nameof(queue));
    _eventHandler = eventHandler ?? throw new ArgumentNullException(nameof(eventHandler));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  /// <summary>
  /// Main processing loop - continuously dequeues and processes webhook events.
  /// Polls the queue at regular intervals.
  /// </summary>
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("WebhookProcessingBackgroundService started");

    try
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        try
        {
          var message = await _queue.DequeueAsync();

          if (message != null)
          {
            _logger.LogInformation(
                "Processing dequeued webhook. EventType={EventType}, Source={Source}",
                message.EventType, message.Source);

            try
            {
              await _eventHandler.HandleEventAsync(message);
              _logger.LogInformation(
                  "Successfully processed webhook. EventType={EventType}, Source={Source}",
                  message.EventType, message.Source);
            }
            catch (ArgumentException argEx)
            {
              _logger.LogWarning(argEx,
                  "Invalid webhook event. EventType={EventType}, Source={Source}",
                  message.EventType, message.Source);
            }
            catch (Exception ex)
            {
              _logger.LogError(ex,
                  "Error processing webhook. EventType={EventType}, Source={Source}. Will NOT retry.",
                  message.EventType, message.Source);
              // Note: In production, you may want to implement dead-letter queues
              // for failed messages and implement retry logic with exponential backoff
            }
          }
          else
          {
            // Queue is empty, wait before polling again
            await Task.Delay(_pollInterval, stoppingToken);
          }
        }
        catch (OperationCanceledException)
        {
          _logger.LogInformation("WebhookProcessingBackgroundService cancellation requested");
          break;
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Unexpected error in webhook processing loop");
          // Wait before retrying to avoid tight loop on errors
          await Task.Delay(_pollInterval, stoppingToken);
        }
      }
    }
    finally
    {
      _logger.LogInformation("WebhookProcessingBackgroundService stopped");
    }
  }
}
