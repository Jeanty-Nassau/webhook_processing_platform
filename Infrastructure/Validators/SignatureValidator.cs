using System.Security.Cryptography;
using System.Text;

namespace webhook_processing_platform.Infrastructure.Validators;

public class SignatureValidator : ISignatureValidator
{
  private readonly string _secret;

  public SignatureValidator(IConfiguration configuration)
  {
    _secret = configuration["WebhookSignatureSecret"] ?? throw new InvalidOperationException("WebhookSignatureSecret is not configured.");
  }

  public bool ValidateSignature(string payload, string signature)
  {
    if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(payload))
      return false;

    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret));
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    var computed = "sha256=" + BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

    // Temp debugging log: show what the validator computed compared to incoming.
    Console.WriteLine($"[DEBUG SignatureValidator] incomingSignature={signature}");
    Console.WriteLine($"[DEBUG SignatureValidator] computedSignature={computed}");
    Console.WriteLine($"[DEBUG SignatureValidator] payloadLength={payload.Length}, payloadRaw={payload}");

    return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(computed), Encoding.UTF8.GetBytes(signature));
  }
}
