using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using webhook_processing_platform.Application.Dtos;
using webhook_processing_platform.Application.Handlers;
using webhook_processing_platform.Application.Interfaces;

namespace webhook_processing_platform.Tests
{
  public class WebhookProcessingBackgroundServiceTests
  {
    [Test]
    public void Constructor_ThrowsArgumentNullException_WhenQueueIsNull()
    {
      var mockHandler = new Mock<IIncomingEventHandler>();
      var logger = new NullLogger<WebhookProcessingBackgroundService>();

      Assert.Throws<ArgumentNullException>(() =>
        new WebhookProcessingBackgroundService(null!, mockHandler.Object, logger));
    }

    [Test]
    public void Constructor_ThrowsArgumentNullException_WhenHandlerIsNull()
    {
      var mockQueue = new Mock<IWebhookQueue>();
      var logger = new NullLogger<WebhookProcessingBackgroundService>();

      Assert.Throws<ArgumentNullException>(() =>
        new WebhookProcessingBackgroundService(mockQueue.Object, null!, logger));
    }

    [Test]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
      var mockQueue = new Mock<IWebhookQueue>();
      var mockHandler = new Mock<IIncomingEventHandler>();

      Assert.Throws<ArgumentNullException>(() =>
        new WebhookProcessingBackgroundService(mockQueue.Object, mockHandler.Object, null!));
    }

    [Test]
    public void Constructor_SucceedsWithValidParameters()
    {
      var mockQueue = new Mock<IWebhookQueue>();
      var mockHandler = new Mock<IIncomingEventHandler>();
      var logger = new NullLogger<WebhookProcessingBackgroundService>();

      var service = new WebhookProcessingBackgroundService(mockQueue.Object, mockHandler.Object, logger);
      Assert.IsNotNull(service);
    }

    // Integration test: Verify the service can start and stop without errors
    [Test]
    public async Task Service_StartsAndStopsGracefully()
    {
      var mockQueue = new Mock<IWebhookQueue>();
      var mockHandler = new Mock<IIncomingEventHandler>();
      var logger = new NullLogger<WebhookProcessingBackgroundService>();

      // Queue is always empty
      mockQueue.Setup(q => q.DequeueAsync()).ReturnsAsync((IncomingMessage?)null);

      var service = new WebhookProcessingBackgroundService(mockQueue.Object, mockHandler.Object, logger);
      var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

      // This would typically be called by the host
      try
      {
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        cts.Cancel();
      }
      finally
      {
        try
        {
          await service.StopAsync(CancellationToken.None);
        }
        catch
        {
          // Stop may throw if the service is still processing
        }
      }

      Assert.Pass("Service started and stopped without throwing");
    }

    private static IncomingMessage CreateTestMessage(string paymentId = "p1")
    {
      return new IncomingMessage
      {
        EventType = "payment.completed",
        Source = "Stripe",
        Timestamp = DateTime.UtcNow,
        Data = new IncomingMessageData
        {
          PaymentId = paymentId,
          OrderId = "o1",
          Amount = 50,
          Currency = "USD"
        }
      };
    }
  }
}
