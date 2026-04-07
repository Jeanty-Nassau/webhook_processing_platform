using webhook_processing_platform.Domain.Models;

namespace webhook_processing_platform.Application.Repositories;

public interface IPaymentRepository
{
  Task<Guid> SavePaymentAsync(Payment payment);
}
