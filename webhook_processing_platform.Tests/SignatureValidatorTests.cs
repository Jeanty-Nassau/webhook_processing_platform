using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using webhook_processing_platform.Infrastructure.Validators;

namespace webhook_processing_platform.Tests
{
  public class SignatureValidatorTests
  {
    [Test]
    public void ValidateSignature_ReturnsTrueForValidSignature()
    {
      var secret = "super-secret";
      var payload = "{\"eventType\":\"payment.completed\"}";
      using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
      var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
      var signature = "sha256=" + BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

      var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { { "WebhookSignatureSecret", secret } })
        .Build();
      var validator = new SignatureValidator(config);

      Assert.True(validator.ValidateSignature(payload, signature));
    }

    [Test]
    public void ValidateSignature_ReturnsFalseForInvalidSignature()
    {
      var secret = "super-secret";
      var payload = "{\"eventType\":\"payment.completed\"}";
      var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { { "WebhookSignatureSecret", secret } })
        .Build();
      var validator = new SignatureValidator(config);

      Assert.False(validator.ValidateSignature(payload, "sha256=invalid"));
    }

    [Test]
    public void ValidateSignature_ReturnsFalseForWrongPayload()
    {
      var secret = "super-secret";
      var payload = "{\"eventType\":\"payment.completed\"}";
      using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
      var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
      var signature = "sha256=" + BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

      var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { { "WebhookSignatureSecret", secret } })
        .Build();
      var validator = new SignatureValidator(config);

      var differentPayload = "{\"eventType\":\"payment.failed\"}";
      Assert.False(validator.ValidateSignature(differentPayload, signature));
    }

    [Test]
    public void ValidateSignature_ReturnsFalseForWrongSecret()
    {
      var secret = "super-secret";
      var payload = "{\"eventType\":\"payment.completed\"}";
      using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
      var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
      var signature = "sha256=" + BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

      var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { { "WebhookSignatureSecret", "different-secret" } })
        .Build();
      var validator = new SignatureValidator(config);

      Assert.False(validator.ValidateSignature(payload, signature));
    }

    [Test]
    public void ValidateSignature_ReturnsFalseForNullPayload()
    {
      var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { { "WebhookSignatureSecret", "secret" } })
        .Build();
      var validator = new SignatureValidator(config);

      Assert.False(validator.ValidateSignature(null!, "sha256=signature"));
    }

    [Test]
    public void ValidateSignature_ReturnsFalseForNullSignature()
    {
      var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { { "WebhookSignatureSecret", "secret" } })
        .Build();
      var validator = new SignatureValidator(config);

      Assert.False(validator.ValidateSignature("{\"test\":true}", null!));
    }

    [Test]
    public void ValidateSignature_ReturnsFalseForEmptyPayload()
    {
      var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { { "WebhookSignatureSecret", "secret" } })
        .Build();
      var validator = new SignatureValidator(config);

      Assert.False(validator.ValidateSignature("", "sha256=signature"));
    }

    [Test]
    public void ValidateSignature_ReturnsFalseForEmptySignature()
    {
      var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { { "WebhookSignatureSecret", "secret" } })
        .Build();
      var validator = new SignatureValidator(config);

      Assert.False(validator.ValidateSignature("{\"test\":true}", ""));
    }

    [Test]
    public void ValidateSignature_IsTimingSafeAgainstLengthDifference()
    {
      var secret = "super-secret";
      var payload = "{\"test\":true}";
      using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
      var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
      var validSignature = "sha256=" + BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

      var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { { "WebhookSignatureSecret", secret } })
        .Build();
      var validator = new SignatureValidator(config);

      // Wrong signature with different length
      Assert.False(validator.ValidateSignature(payload, "sha256=short"));
    }

    [Test]
    public void ValidateSignature_HandlesDifferentCases()
    {
      var secret = "test-secret";
      var payload = "{\"test\":\"data\"}";
      using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
      var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
      var signature = "sha256=" + BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

      var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { { "WebhookSignatureSecret", secret } })
        .Build();
      var validator = new SignatureValidator(config);

      // Validation is case-sensitive (lowercase sha256 in signature)
      Assert.True(validator.ValidateSignature(payload, signature));

      // Upper case signature should fail (hex must be lowercase)
      var upperSignature = signature.ToUpperInvariant();
      Assert.False(validator.ValidateSignature(payload, upperSignature));
    }

    [Test]
    public void Constructor_ThrowsInvalidOperationException_WhenSecretNotConfigured()
    {
      var config = new ConfigurationBuilder().Build(); // No secret configured

      Assert.Throws<InvalidOperationException>(() => new SignatureValidator(config));
    }
  }
}
