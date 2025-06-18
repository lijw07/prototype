using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Services.Interfaces;

public interface IApplicationLogFactoryService
{
    ApplicationLogModel CreateApplicationLog(ApplicationModel application, ActionTypeEnum actionType, List<string> affectedEntities);
}