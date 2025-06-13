using Prototype.Data;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class UnitOfWorkFactoryService(SentinelContext context) : IUnitOfWorkService
{
    public IRepositoryService<UserModel> Users { get; } = new RepositoryService<UserModel>(context);
    public IRepositoryService<ApplicationModel> Applications { get; } = new RepositoryService<ApplicationModel>(context);
    public IRepositoryService<UserActivityLogModel> UserActivityLogs { get; } = new RepositoryService<UserActivityLogModel>(context);
    public IRepositoryService<AuditLogModel> AuditLogs { get; } = new RepositoryService<AuditLogModel>(context);
    public IRepositoryService<UserRecoveryRequestModel> UserRecoveryRequests { get; } = new RepositoryService<UserRecoveryRequestModel>(context);
    public IRepositoryService<TemporaryUserModel> TemporaryUser { get; } = new RepositoryService<TemporaryUserModel>(context);
    public Task<int> SaveChangesAsync() => context.SaveChangesAsync();
}