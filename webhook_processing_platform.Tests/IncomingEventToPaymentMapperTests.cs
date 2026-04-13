using System;
using NUnit.Framework;
using webhook_processing_platform.Application.Dtos;
using webhook_processing_platform.Application.Mappers;

namespace webhook_processing_platform.Tests
{
  public class IncomingEventToPaymentMapperTests
  {
    private IncomingEventToPaymentMapper _mapper = null!;

    [SetUp]
    public void Setup()
    {
      _mapper = new IncomingEventToPaymentMapper();
    }

    [Test]
    public void MapToPayment_MapsAllFields()
    {
      var timestamp = DateTime.UtcNow;
      var incoming = new IncomingMessage
      {
        EventType = "payment.completed",
        Source = "Stripe",
        Timestamp = timestamp,
        Data = new IncomingMessageData
        {
          PaymentId = "pay_123",
          OrderId = "order_456",
          Amount = 99.99,
          Currency = "USD"
        }
      };

      var payment = _mapper.MapToPayment(incoming);

      Assert.AreEqual("pay_123", payment.PaymentId);
      Assert.AreEqual("order_456", payment.OrderId);
      Assert.AreEqual(99.99m, payment.Amount);
      Assert.AreEqual("USD", payment.Currency);
      Assert.AreEqual("payment.completed", payment.EventType);
      Assert.AreEqual("Stripe", payment.Source);
      Assert.AreEqual(timestamp, payment.ReceivedAt);
      Assert.AreEqual("Pending", payment.Status);
      Assert.AreNotEqual(Guid.Empty, payment.Id);
    }

    [Test]
    public void MapToPayment_ConvertsAmountToDecimal()
    {
      var incoming = new IncomingMessage
      {
        EventType = "payment.completed",
        Source = "PayPal",
        Timestamp = DateTime.UtcNow,
        Data = new IncomingMessageData
        {
          PaymentId = "p1",
          OrderId = "o1",
          Amount = 50.5555,
          Currency = "EUR"
        }
      };

      var payment = _mapper.MapToPayment(incoming);

      Assert.AreEqual(typeof(decimal), payment.Amount.GetType());
      Assert.AreEqual(50.5555m, payment.Amount);
    }

    [Test]
    public void MapToPayment_GeneratesUniqueIds()
    {
      var incomingTemplate = new IncomingMessage
      {
        EventType = "payment.completed",
        Source = "Stripe",
        Timestamp = DateTime.UtcNow,
        Data = new IncomingMessageData
        {
          PaymentId = "p1",
          OrderId = "o1",
          Amount = 100,
          Currency = "USD"
        }
      };

      var payment1 = _mapper.MapToPayment(incomingTemplate);
      var payment2 = _mapper.MapToPayment(incomingTemplate);

      Assert.AreNotEqual(payment1.Id, payment2.Id);
    }

    [Test]
    public void MapToPayment_ThrowsArgumentNullException_WhenIncomingIsNull()
    {
      Assert.Throws<ArgumentNullException>(() => _mapper.MapToPayment(null!));
    }

    [Test]
    public void MapToPayment_DefaultsStatusToPending()
    {
      var incoming = new IncomingMessage
      {
        EventType = "payment.completed",
        Source = "Stripe",
        Timestamp = DateTime.UtcNow,
        Data = new IncomingMessageData
        {
          PaymentId = "p1",
          OrderId = "o1",
          Amount = 50,
          Currency = "USD"
        }
      };

      var payment = _mapper.MapToPayment(incoming);

      Assert.AreEqual("Pending", payment.Status);
    }

    [Test]
    public void MapToPayment_MapsEventTypeDirectly()
    {
      var eventTypes = new[] { "payment.completed", "payment.failed", "refund.processed" };

      foreach (var eventType in eventTypes)
      {
        var incoming = new IncomingMessage
        {
          EventType = eventType,
          Source = "TestSource",
          Timestamp = DateTime.UtcNow,
          Data = new IncomingMessageData
          {
            PaymentId = "p1",
            OrderId = "o1",
            Amount = 50,
            Currency = "USD"
          }
        };

        var payment = _mapper.MapToPayment(incoming);

        Assert.AreEqual(eventType, payment.EventType);
      }
    }

    [Test]
    public void MapToPayment_PreservesTimestamp()
    {
      var timestamp = new DateTime(2024, 4, 15, 10, 30, 45, DateTimeKind.Utc);
      var incoming = new IncomingMessage
      {
        EventType = "payment.completed",
        Source = "Stripe",
        Timestamp = timestamp,
        Data = new IncomingMessageData
        {
          PaymentId = "p1",
          OrderId = "o1",
          Amount = 50,
          Currency = "USD"
        }
      };

      var payment = _mapper.MapToPayment(incoming);

      Assert.AreEqual(timestamp, payment.ReceivedAt);
    }
  }
}
