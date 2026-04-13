using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using webhook_processing_platform.Application.Dtos;
using webhook_processing_platform.Infrastructure.Queues;

namespace webhook_processing_platform.Tests
{
  public class InMemoryWebhookQueueTests
  {
    private InMemoryWebhookQueue _queue = null!;

    [SetUp]
    public void Setup()
    {
      _queue = new InMemoryWebhookQueue(new NullLogger<InMemoryWebhookQueue>());
    }

    [Test]
    public async Task EnqueueAsync_AddsMessageToQueue()
    {
      var message = CreateTestMessage();
      await _queue.EnqueueAsync(message);

      var size = await _queue.GetQueueSizeAsync();
      Assert.AreEqual(1, size);
    }

    [Test]
    public async Task DequeueAsync_ReturnsMessageInFifoOrder()
    {
      var message1 = CreateTestMessage("payment1");
      var message2 = CreateTestMessage("payment2");

      await _queue.EnqueueAsync(message1);
      await _queue.EnqueueAsync(message2);

      var dequeued1 = await _queue.DequeueAsync();
      var dequeued2 = await _queue.DequeueAsync();

      Assert.AreEqual("payment1", dequeued1?.Data.PaymentId);
      Assert.AreEqual("payment2", dequeued2?.Data.PaymentId);
    }

    [Test]
    public async Task DequeueAsync_ReturnsNullWhenQueueIsEmpty()
    {
      var result = await _queue.DequeueAsync();
      Assert.IsNull(result);
    }

    [Test]
    public async Task GetQueueSizeAsync_ReturnsCorrectSize()
    {
      var size0 = await _queue.GetQueueSizeAsync();
      Assert.AreEqual(0, size0);

      await _queue.EnqueueAsync(CreateTestMessage());
      var size1 = await _queue.GetQueueSizeAsync();
      Assert.AreEqual(1, size1);

      await _queue.DequeueAsync();
      var size2 = await _queue.GetQueueSizeAsync();
      Assert.AreEqual(0, size2);
    }

    [Test]
    public void EnqueueAsync_ThrowsArgumentNullException_WhenMessageIsNull()
    {
      Assert.ThrowsAsync<ArgumentNullException>(async () => await _queue.EnqueueAsync(null!));
    }

    [Test]
    public async Task Queue_CanHandleMultipleOperations()
    {
      for (int i = 0; i < 100; i++)
      {
        await _queue.EnqueueAsync(CreateTestMessage($"payment{i}"));
      }

      var size = await _queue.GetQueueSizeAsync();
      Assert.AreEqual(100, size);

      for (int i = 0; i < 100; i++)
      {
        var message = await _queue.DequeueAsync();
        Assert.IsNotNull(message);
        Assert.AreEqual($"payment{i}", message!.Data.PaymentId);
      }

      var finalSize = await _queue.GetQueueSizeAsync();
      Assert.AreEqual(0, finalSize);
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
