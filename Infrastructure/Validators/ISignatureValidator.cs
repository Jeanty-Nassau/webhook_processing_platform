namespace webhook_processing_platform.Infrastructure.Validators;

public interface ISignatureValidator
{
  bool ValidateSignature(string payload, string signature);
}
