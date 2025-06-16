using Prototype.Models;

namespace Prototype.Services.Interfaces;

public interface IUnitOfWorkFactoryService
{
    IRepositoryFactoryService<UserModel> Users { get; }
    IRepositoryFactoryService<ApplicationModel> Applications { get; }
    IRepositoryFactoryService<ApplicationLogModel> ApplicationLogs { get; }
    IRepositoryFactoryService<UserActivityLogModel> UserActivityLogs { get; }
    IRepositoryFactoryService<AuditLogModel> AuditLogs { get; }
    IRepositoryFactoryService<UserRecoveryRequestModel> UserRecoveryRequests { get; }
    IRepositoryFactoryService<TemporaryUserModel> TemporaryUser { get; }
    IRepositoryFactoryService<UserApplicationModel> UserApplications { get; }
    
    Task<int> SaveChangesAsync();
}