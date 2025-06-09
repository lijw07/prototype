namespace Prototype.Services.Interfaces;

/// <summary>
/// IVerificationService Is responsible for Generating Verification Codes.
/// </summary>
public interface IVerificationService
{
    string GenerateVerificationCode();
}