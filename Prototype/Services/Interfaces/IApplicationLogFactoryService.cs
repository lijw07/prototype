using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Services.Interfaces;

public interface IApplicationLogFactoryService
{
    ApplicationLogModel CreateApplicationLog(ApplicationModel application, ApplicationActionTypeEnum actionType, List<String> affectedEntities);
}