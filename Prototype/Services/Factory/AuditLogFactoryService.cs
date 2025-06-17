using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class AuditLogFactoryService : IAuditLogFactoryService
{
    public AuditLogModel CreateAuditLog(UserModel user, ActionTypeEnum action, List<string> affectedTables)
    {
        
        object? metadata = action switch
        {
            ActionTypeEnum.ForgotPassword or ActionTypeEnum.ChangePassword => user.UserRecoveryRequests?
                .Where(r => r.UserId == user.UserId)
                .OrderByDescending(r => r.RequestedAt)
                .Select(r => new
                {
                    r.UserRecoveryRequestId,
                    RecoveryType = r.UserRecoveryType.ToString(),
                    r.VerificationCode,
                    r.RequestedAt
                })
                .FirstOrDefault(),

            ActionTypeEnum.Login or ActionTypeEnum.FailedLogin => new
            {
                UserId = user.UserId,
                UserEmail = user.Email,
                UserUpdateAt = user.UpdatedAt
            },
            _ => new
            {
                user.UserId,
                Info = "Generic metadata"
            }
        };

        return new AuditLogModel
        {
            AuditLogId = Guid.NewGuid(),
            UserId = user.UserId,
            User = user,
            ActionType = action,
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                Metadata = metadata,
                AffectedEntities = affectedTables,
                Timestamp = DateTime.Now
            }),
            CreatedAt = DateTime.Now
        };
    }
}