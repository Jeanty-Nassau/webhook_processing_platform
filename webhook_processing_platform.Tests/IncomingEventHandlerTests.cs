using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using webhook_processing_platform.Application.Dtos;
using webhook_processing_platform.Application.Handlers;
using webhook_processing_platform.Application.Mappers;
using webhook_processing_platform.Application.Interfaces;
using webhook_processing_platform.Domain.Models;

namespace webhook_processing_platform.Tests
{
  public class IncomingEventHandlerTests
  {
    [Test]
    public async Task HandleEventAsync_SavesPayment_WhenEventIsValid()
    {
      var mapper = new IncomingEventToPaymentMapper();
      var mockRepo = new Mock<IPaymentRepository>();
      mockRepo.Setup(x => x.SavePaymentAsync(It.IsAny<Payment>())).ReturnsAsync(Guid.NewGuid());

      var handler = new IncomingEventHandler(mockRepo.Object, mapper, new NullLogger<IncomingEventHandler>());
      var incoming = new IncomingMessage
      {
        EventType = "payment.completed",
        Source = "Stripe",
        Timestamp = DateTime.UtcNow,
        Data = new IncomingMessageData { PaymentId = "p1", OrderId = "o1", Amount = 50, Currency = "USD" }
      };

      await handler.HandleEventAsync(incoming);

      mockRepo.Verify(x => x.SavePaymentAsync(It.Is<Payment>(p => p.PaymentId == "p1")), Times.Once);
    }

    [Test]
    public async Task HandleEventAsync_ThrowsArgumentException_WhenEventTypeInvalid()
    {
      var mapper = new IncomingEventToPaymentMapper();
      var mockRepo = new Mock<IPaymentRepository>();
      var handler = new IncomingEventHandler(mockRepo.Object, mapper, new NullLogger<IncomingEventHandler>());

      var incoming = new IncomingMessage
      {
        EventType = "Other",
        Source = "Stripe",
        Timestamp = DateTime.UtcNow,
        Data = new IncomingMessageData { PaymentId = "p1", OrderId = "o1", Amount = 50, Currency = "USD" }
      };

      Assert.ThrowsAsync<ArgumentException>(async () => await handler.HandleEventAsync(incoming));
      mockRepo.Verify(x => x.SavePaymentAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Test]
    public void HandleEventAsync_ThrowsArgumentNullException_WhenMessageIsNull()
    {
      var mapper = new IncomingEventToPaymentMapper();
      var mockRepo = new Mock<IPaymentRepository>();
      var handler = new IncomingEventHandler(mockRepo.Object, mapper, new NullLogger<IncomingEventHandler>());

      Assert.ThrowsAsync<ArgumentNullException>(async () => await handler.HandleEventAsync(null!));
    }

    [Test]
    public async Task HandleEventAsync_MapsProperly_AllFields()
    {
      var mapper = new IncomingEventToPaymentMapper();
      var mockRepo = new Mock<IPaymentRepository>();
      var capturedPayment = (Payment?)null;

      mockRepo.Setup(x => x.SavePaymentAsync(It.IsAny<Payment>()))
        .Returns((Payment p) =>
        {
          capturedPayment = p;
          return Task.FromResult(Guid.NewGuid());
        });

      var handler = new IncomingEventHandler(mockRepo.Object, mapper, new NullLogger<IncomingEventHandler>());
      var timestamp = DateTime.UtcNow;
      var incoming = new IncomingMessage
      {
        EventType = "payment.completed",
        Source = "PayPal",
        Timestamp = timestamp,
        Data = new IncomingMessageData
        {
          PaymentId = "pay_123",
          OrderId = "order_456",
          Amount = 99.99,
          Currency = "EUR"
        }
      };

      await handler.HandleEventAsync(incoming);

      Assert.IsNotNull(capturedPayment);
      Assert.AreEqual("pay_123", capturedPayment!.PaymentId);
      Assert.AreEqual("order_456", capturedPayment.OrderId);
      Assert.AreEqual(99.99m, capturedPayment.Amount);
      Assert.AreEqual("EUR", capturedPayment.Currency);
      Assert.AreEqual("payment.completed", capturedPayment.EventType);
      Assert.AreEqual("PayPal", capturedPayment.Source);
      Assert.AreEqual("Pending", capturedPayment.Status);
    }

    [Test]
    public async Task HandleEventAsync_LogsDuplicatePaymentException_DoesNotThrow()
    {
      var mapper = new IncomingEventToPaymentMapper();
      var mockRepo = new Mock<IPaymentRepository>();
      mockRepo.Setup(x => x.SavePaymentAsync(It.IsAny<Payment>()))
        .ThrowsAsync(new DuplicatePaymentException("p1"));

      var handler = new IncomingEventHandler(mockRepo.Object, mapper, new NullLogger<IncomingEventHandler>());
      var incoming = new IncomingMessage
      {
        EventType = "payment.completed",
        Source = "Stripe",
        Timestamp = DateTime.UtcNow,
        Data = new IncomingMessageData { PaymentId = "p1", OrderId = "o1", Amount = 50, Currency = "USD" }
      };

      // Should not throw even though repo throws DuplicatePaymentException
      await handler.HandleEventAsync(incoming);
    }

    [Test]
    public async Task HandleEventAsync_PropagatesOtherExceptions()
    {
      var mapper = new IncomingEventToPaymentMapper();
      var mockRepo = new Mock<IPaymentRepository>();
      mockRepo.Setup(x => x.SavePaymentAsync(It.IsAny<Payment>()))
        .ThrowsAsync(new InvalidOperationException("Database error"));

      var handler = new IncomingEventHandler(mockRepo.Object, mapper, new NullLogger<IncomingEventHandler>());
      var incoming = new IncomingMessage
      {
        EventType = "payment.completed",
        Source = "Stripe",
        Timestamp = DateTime.UtcNow,
        Data = new IncomingMessageData { PaymentId = "p1", OrderId = "o1", Amount = 50, Currency = "USD" }
      };

      Assert.ThrowsAsync<InvalidOperationException>(async () => await handler.HandleEventAsync(incoming));
    }
  }
}
