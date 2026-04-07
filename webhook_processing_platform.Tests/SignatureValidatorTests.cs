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

      var config = new ConfigurationBuilder().AddInMemoryCollection(new[] { new KeyValuePair<string, string>("WebhookSignatureSecret", secret) }).Build();
      var validator = new SignatureValidator(config);

      Assert.True(validator.ValidateSignature(payload, signature));
    }

    [Test]
    public void ValidateSignature_ReturnsFalseForInvalidSignature()
    {
      var secret = "super-secret";
      var payload = "{\"eventType\":\"payment.completed\"}";
      var config = new ConfigurationBuilder().AddInMemoryCollection(new[] { new KeyValuePair<string, string>("WebhookSignatureSecret", secret) }).Build();
      var validator = new SignatureValidator(config);

      Assert.False(validator.ValidateSignature(payload, "sha256=invalid"));
    }
  }
}
