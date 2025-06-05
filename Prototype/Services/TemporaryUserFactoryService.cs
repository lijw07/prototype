using Prototype.DTOs;
using Prototype.Models;

namespace Prototype.Services;


public class TemporaryUserFactoryService : ITemporaryUserFactoryService
{
    public TemporaryUserModel CreateFromRequest(RegisterRequest request, string verificationCode)
    {
        return new TemporaryUserModel
        {
            TemporaryUserId = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = request.PasswordHash,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            CreatedAt = request.CreatedAt,
            VerificationCode = verificationCode
        };
    }
}