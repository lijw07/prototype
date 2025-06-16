using Prototype.Data;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class UnitOfWorkFactoryService(SentinelContext context) : IUnitOfWorkService
{
    public IRepositoryService<UserModel> Users { get; } = new RepositoryFactoryService<UserModel>(context);
    public IRepositoryService<ApplicationModel> Applications { get; } = new RepositoryFactoryService<ApplicationModel>(context);
    public IRepositoryService<ApplicationLogModel> ApplicationLogs { get; } = new RepositoryFactoryService<ApplicationLogModel>(context);
    public IRepositoryService<UserActivityLogModel> UserActivityLogs { get; } = new RepositoryFactoryService<UserActivityLogModel>(context);
    public IRepositoryService<AuditLogModel> AuditLogs { get; } = new RepositoryFactoryService<AuditLogModel>(context);
    public IRepositoryService<UserRecoveryRequestModel> UserRecoveryRequests { get; } = new RepositoryFactoryService<UserRecoveryRequestModel>(context);
    public IRepositoryService<TemporaryUserModel> TemporaryUser { get; } = new RepositoryFactoryService<TemporaryUserModel>(context);
    public IRepositoryService<UserApplicationModel> UserApplications { get; } = new RepositoryFactoryService<UserApplicationModel>(context);
    public Task<int> SaveChangesAsync() => context.SaveChangesAsync();
}