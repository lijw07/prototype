using Prototype.DTOs;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class UserRecoveryFactoryService : IUserRecoveryRequestFactoryService
{
    public UserRecoveryRequestModel CreateFromForgotUser(UserModel user, ForgotUserRequestDto dto, string verificationCode)
    {
        return new UserRecoveryRequestModel
        {
            UserRecoveryRequestId = Guid.NewGuid(),
            UserId = user.UserId,
            User = user,
            VerificationCode = verificationCode,
            UserRecoveryType = dto.UserRecoveryType,
            RequestedAt = DateTime.Now,
            ExpiresAt = DateTime.Now.AddMinutes(30)
        };
    }
}