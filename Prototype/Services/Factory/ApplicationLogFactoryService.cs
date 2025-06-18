using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class ApplicationLogFactoryService : IApplicationLogFactoryService
{
    public ApplicationLogModel CreateApplicationLog(ApplicationModel application, ActionTypeEnum actionType, List<string> affectedEntities)
    {
        return new ApplicationLogModel
        {
            ApplicationLogId = Guid.NewGuid(),
            ApplicationId = application.ApplicationId,
            Application = application,
            ActionType = actionType,
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                AffectedEntities = affectedEntities,
            }),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
    }
}