using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Services.Interfaces;

public interface IAuditLogFactoryService
{
    AuditLogModel CreateAuditLog(UserModel? user, ActionTypeEnum action, List<string> affectedTables);
}