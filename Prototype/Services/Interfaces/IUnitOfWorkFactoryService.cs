using Prototype.Models;
using Prototype.Services.Factory;

namespace Prototype.Services.Interfaces;

public interface IUnitOfWorkFactoryService
{
    RepositoryFactoryService<UserModel> Users { get; }
    IRepositoryFactoryService<ApplicationModel> Applications { get; }
    IRepositoryFactoryService<ApplicationLogModel> ApplicationLogs { get; }
    IRepositoryFactoryService<UserActivityLogModel> UserActivityLogs { get; }
    IRepositoryFactoryService<AuditLogModel> AuditLogs { get; }
    IRepositoryFactoryService<UserRecoveryRequestModel> UserRecoveryRequests { get; }
    IRepositoryFactoryService<TemporaryUserModel> TemporaryUser { get; }
    IRepositoryFactoryService<UserApplicationModel> UserApplications { get; }
    IRepositoryFactoryService<ApplicationConnectionModel> ApplicationConnections { get; }
    
    Task<int> SaveChangesAsync();
}