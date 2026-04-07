using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using webhook_processing_platform.Application.Dtos;
using webhook_processing_platform.Application.Handlers;
using webhook_processing_platform.Application.Mappers;
using webhook_processing_platform.Application.Repositories;
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
  }
}
