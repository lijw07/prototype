using Prototype.DTOs;
using Prototype.DTOs.Request;
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
            Token = verificationCode,
            RecoveryType = dto.UserRecoveryType,
            RequestedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            IsUsed = false
        };
    }
}