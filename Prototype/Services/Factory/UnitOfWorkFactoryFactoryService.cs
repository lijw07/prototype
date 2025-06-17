using Prototype.Data;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class UnitOfWorkFactoryFactoryService(SentinelContext context) : IUnitOfWorkFactoryService
{
    public IRepositoryFactoryService<UserModel> Users { get; } = new RepositoryFactoryFactoryService<UserModel>(context);
    public IRepositoryFactoryService<ApplicationModel> Applications { get; } = new RepositoryFactoryFactoryService<ApplicationModel>(context);
    public IRepositoryFactoryService<ApplicationLogModel> ApplicationLogs { get; } = new RepositoryFactoryFactoryService<ApplicationLogModel>(context);
    public IRepositoryFactoryService<UserActivityLogModel> UserActivityLogs { get; } = new RepositoryFactoryFactoryService<UserActivityLogModel>(context);
    public IRepositoryFactoryService<AuditLogModel> AuditLogs { get; } = new RepositoryFactoryFactoryService<AuditLogModel>(context);
    public IRepositoryFactoryService<UserRecoveryRequestModel> UserRecoveryRequests { get; } = new RepositoryFactoryFactoryService<UserRecoveryRequestModel>(context);
    public IRepositoryFactoryService<TemporaryUserModel> TemporaryUser { get; } = new RepositoryFactoryFactoryService<TemporaryUserModel>(context);
    public IRepositoryFactoryService<UserApplicationModel> UserApplications { get; } = new RepositoryFactoryFactoryService<UserApplicationModel>(context);
    public Task<int> SaveChangesAsync() => context.SaveChangesAsync();
}