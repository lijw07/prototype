using Prototype.Data;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class UnitOfWorkFactoryService(SentinelContext context) : IUnitOfWorkFactoryService
{
    public RepositoryFactoryService<UserModel> Users { get; } = new RepositoryFactoryService<UserModel>(context);
    public IRepositoryFactoryService<ApplicationModel> Applications { get; } = new RepositoryFactoryService<ApplicationModel>(context);
    public IRepositoryFactoryService<ApplicationLogModel> ApplicationLogs { get; } = new RepositoryFactoryService<ApplicationLogModel>(context);
    public IRepositoryFactoryService<UserActivityLogModel> UserActivityLogs { get; } = new RepositoryFactoryService<UserActivityLogModel>(context);
    public IRepositoryFactoryService<AuditLogModel> AuditLogs { get; } = new RepositoryFactoryService<AuditLogModel>(context);
    public IRepositoryFactoryService<UserRecoveryRequestModel> UserRecoveryRequests { get; } = new RepositoryFactoryService<UserRecoveryRequestModel>(context);
    public IRepositoryFactoryService<TemporaryUserModel> TemporaryUser { get; } = new RepositoryFactoryService<TemporaryUserModel>(context);
    public IRepositoryFactoryService<UserApplicationModel> UserApplications { get; } = new RepositoryFactoryService<UserApplicationModel>(context);
    public IRepositoryFactoryService<ApplicationConnectionModel> ApplicationConnections { get; } = new RepositoryFactoryService<ApplicationConnectionModel>(context);
    public Task<int> SaveChangesAsync() => context.SaveChangesAsync();
}