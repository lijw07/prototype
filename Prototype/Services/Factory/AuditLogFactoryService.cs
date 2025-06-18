using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;
using System.Text.Json;

namespace Prototype.Services.Factory;

public class AuditLogFactoryService : IAuditLogFactoryService
{
    public AuditLogModel CreateAuditLog(UserModel? user, ActionTypeEnum action, List<string> affectedTables)
    {
        
        if (user == null)
            throw new ArgumentNullException(nameof(user), "User cannot be null when creating a UserActivityLog.");
        
        var metadataObject = new
        {
            Action = action.ToString(),
            AffectedEntities = affectedTables,
            PerformedBy = new
            {
                user.UserId,
                user.Username,
                user.Email
            },
            Timestamp = DateTime.UtcNow
        };

        return new AuditLogModel
        {
            AuditLogId = Guid.NewGuid(),
            UserId = user.UserId,
            User = user,
            ActionType = action,
            Metadata = JsonSerializer.Serialize(metadataObject),
            CreatedAt = DateTime.UtcNow
        };
    }
}