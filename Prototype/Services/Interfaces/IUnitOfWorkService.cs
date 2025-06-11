using Prototype.Models;

namespace Prototype.Services.Interfaces;

public interface IUnitOfWorkService
{
    IRepositoryService<UserModel> Users { get; }
    IRepositoryService<ApplicationModel> Applications { get; }
    IRepositoryService<UserActivityLogModel> UserActivityLogs { get; }
    IRepositoryService<AuditLogModel> AuditLogs { get; }
    IRepositoryService<UserRecoveryRequestModel> UserRecoveryRequests { get; }
    IRepositoryService<TemporaryUserModel> TemporaryUser { get; }

    Task<int> SaveChangesAsync();
}