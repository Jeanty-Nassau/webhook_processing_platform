using System.Data;
using Dapper;
using Npgsql;
using webhook_processing_platform.Application.Repositories;
using webhook_processing_platform.Domain.Models;

namespace webhook_processing_platform.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
  private readonly IDbConnection _dbConnection;
  private readonly ILogger<PaymentRepository> _logger;
  private const int MaxRetries = 3;
  private const int InitialDelayMs = 100;

  public PaymentRepository(IDbConnection dbConnection, ILogger<PaymentRepository> logger)
  {
    _dbConnection = dbConnection;
    _logger = logger;
  }

  public async Task<Guid> SavePaymentAsync(Payment payment)
  {
    const string sql = @"
      INSERT INTO webhook_schema.payments 
      (id, payment_id, order_id, amount, currency, event_type, source, received_at, processed_at, status)
      VALUES 
      (@Id, @PaymentId, @OrderId, @Amount, @Currency, @EventType, @Source, @ReceivedAt, @ProcessedAt, @Status);";

    return await RetryWithBackoffAsync(async () =>
    {
      await _dbConnection.ExecuteAsync(sql, payment);
      _logger.LogInformation("Payment saved: paymentId={PaymentId}", payment.PaymentId);
      return payment.Id;
    }, payment.PaymentId);
  }

  private async Task<Guid> RetryWithBackoffAsync(Func<Task<Guid>> operation, string paymentId)
  {
    for (int attempt = 1; attempt <= MaxRetries; attempt++)
    {
      try
      {
        return await operation();
      }
      catch (PostgresException pgEx) when (pgEx.SqlState == "23505")
      {
        _logger.LogWarning(pgEx, "Duplicate payment detected for paymentId={PaymentId}", paymentId);
        throw new DuplicatePaymentException(paymentId);
      }
      catch (NpgsqlException ex) when (attempt < MaxRetries && IsTransientError(ex))
      {
        int delayMs = InitialDelayMs * (int)Math.Pow(2, attempt - 1);
        _logger.LogWarning(ex, "Transient database error for paymentId={PaymentId}. Attempt {Attempt}/{MaxRetries}. Retrying in {DelayMs}ms",
          paymentId, attempt, MaxRetries, delayMs);
        await Task.Delay(delayMs);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to save payment to database for paymentId={PaymentId}", paymentId);
        throw new InvalidOperationException("Failed to save payment to database.", ex);
      }
    }

    throw new InvalidOperationException($"Failed to save payment after {MaxRetries} attempts.");
  }

  private static bool IsTransientError(NpgsqlException ex)
  {
    // Check for transient network errors
    return ex.InnerException is System.Net.Sockets.SocketException socketEx &&
           (socketEx.SocketErrorCode == System.Net.Sockets.SocketError.HostNotFound ||
            socketEx.SocketErrorCode == System.Net.Sockets.SocketError.HostUnreachable ||
            socketEx.SocketErrorCode == System.Net.Sockets.SocketError.NetworkUnreachable ||
            socketEx.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionRefused ||
            socketEx.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut);
  }
}

