using Prototype.Models;

namespace Prototype.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<UserModel> Users { get; }
        IRepository<ApplicationModel> Applications { get; }
        IRepository<UserApplicationModel> UserApplications { get; }
        IRepository<UserActivityLogModel> UserActivityLogs { get; }
        IRepository<AuditLogModel> AuditLogs { get; }
        IRepository<ApplicationLogModel> ApplicationLogs { get; }
        IRepository<BulkUploadHistoryModel> BulkUploadHistories { get; }
        IRepository<UserRecoveryRequestModel> UserRecoveryRequests { get; }
        IRepository<TemporaryUserModel> TemporaryUsers { get; }
        IRepository<ApplicationConnectionModel> ApplicationConnections { get; }
        IRepository<DataSourceModel> DataSources { get; }
        IRepository<HumanResourceModel> HumanResources { get; }
        IRepository<AuthenticationModel> Authentications { get; }
        IRepository<UserRequestModel> UserRequests { get; }
        
        Task<int> SaveChangesAsync();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
        void BeginTransaction();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}