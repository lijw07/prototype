using Prototype.DTOs;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class UserRecoveryFactoryService : IUserRecoveryRequestFactoryService
{
    public UserRecoveryRequestModel CreateUserRecoveryRequest(UserModel user, ForgotUserRequestDto dto, string verificationCode)
    {
        return new UserRecoveryRequestModel
        {
            UserRecoveryRequestId = Guid.NewGuid(),
            UserId = user.UserId,
            User = user,
            VerificationCode = verificationCode,
            RecoveryType = dto.UserRecoveryType,
            RequestedAt = DateTime.Now,
            ExpiresAt = DateTime.Now.AddMinutes(30),
            IsUsed = false
        };
    }
}