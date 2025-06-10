using System.Security.Cryptography;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class VerificationService : IVerificationService
{
    public string GenerateVerificationCode() =>
        RandomNumberGenerator.GetInt32(0, 100000).ToString("D5");
}