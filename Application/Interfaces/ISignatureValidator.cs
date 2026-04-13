namespace webhook_processing_platform.Application.Interfaces;

public interface ISignatureValidator
{
  bool ValidateSignature(string payload, string signature);
}
