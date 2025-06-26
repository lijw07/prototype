using Prototype.Models;

namespace Prototype.Repositories;

/// <summary>
/// Unit of Work pattern implementation for managing transactions across multiple repositories
/// Follows Clean Architecture principles and eliminates direct context access in services
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IGenericRepository<UserModel> Users { get; }
    IGenericRepository<ApplicationModel> Applications { get; }
    IGenericRepository<UserApplicationModel> UserApplications { get; }
    IGenericRepository<AuditLogModel> AuditLogs { get; }
    IGenericRepository<UserActivityLogModel> UserActivityLogs { get; }
    IGenericRepository<ApplicationLogModel> ApplicationLogs { get; }
    IGenericRepository<TemporaryUserModel> TemporaryUsers { get; }
    IGenericRepository<UserRecoveryRequestModel> UserRecoveryRequests { get; }
    IGenericRepository<ApplicationConnectionModel> ApplicationConnections { get; }
    IGenericRepository<HumanResourceModel> HumanResources { get; }
    IGenericRepository<DataSourceModel> DataSources { get; }
    IGenericRepository<UserRoleModel> UserRoles { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}