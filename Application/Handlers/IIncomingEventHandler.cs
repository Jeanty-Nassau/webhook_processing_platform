using System;
using webhook_processing_platform.Application.Dtos;

namespace webhook_processing_platform.Application.Handlers;

public interface IIncomingEventHandler
{
  public Task HandleEventAsync(IncomingMessage incomingEvent);
}
