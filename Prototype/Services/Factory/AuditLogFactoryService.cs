using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class AuditLogFactoryService : IAuditLogFactoryService
{
    public AuditLogModel CreateAuditLog(UserModel user, ActionTypeEnum action, List<string> affectedTables)
    {
        var latestRecovery = user.UserRecoveryRequests?
            .Where(r => r.UserId == user.UserId)
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefault();

        return new AuditLogModel
        {
            AuditLogId = Guid.NewGuid(),
            UserId = user.UserId,
            User = user,
            ActionType = action,
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                RecoveryRequestId = latestRecovery?.UserRecoveryRequestId,
                RecoveryType = latestRecovery?.UserRecoveryType.ToString(),
                Token = latestRecovery?.VerificationCode,
                RequestedAt = latestRecovery?.RequestedAt,
                ResetAt = DateTime.UtcNow,
                AffectedEntities = affectedTables
            }),
            CreatedAt = DateTime.UtcNow
        };
    }
}