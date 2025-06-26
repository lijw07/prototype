using Prototype.Enum;

namespace Prototype.Services.Interfaces;

public interface IUserActivityService
{
    Task CreateUserActivityLogAsync(Guid userId, ActionTypeEnum action, string description);
    Task CreateAuditLogAsync(Guid userId, ActionTypeEnum action, string description);
    Task<List<string>> GetUserActivityHistoryAsync(Guid userId, int limit = 50);
    Task<List<string>> GetUserAuditHistoryAsync(Guid userId, int limit = 50);
}