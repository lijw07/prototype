using Prototype.Models;

namespace Prototype.Services.Interfaces;

public interface IUnitOfWorkService
{
    IRepositoryService<UserModel> Users { get; }
    IRepositoryService<ApplicationModel> Applications { get; }
    IRepositoryService<ApplicationLogModel> ApplicationLogs { get; }
    IRepositoryService<UserActivityLogModel> UserActivityLogs { get; }
    IRepositoryService<AuditLogModel> AuditLogs { get; }
    IRepositoryService<UserRecoveryRequestModel> UserRecoveryRequests { get; }
    IRepositoryService<TemporaryUserModel> TemporaryUser { get; }
    IRepositoryService<UserApplicationModel> UserApplications { get; }
    
    Task<int> SaveChangesAsync();
}