using Microsoft.EntityFrameworkCore.Storage;
using Prototype.Models;

namespace Prototype.Repositories;

/// <summary>
/// Unit of Work implementation providing centralized transaction management
/// Eliminates direct context access from services and follows repository pattern
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly SentinelContext _context;
    private readonly ILogger<UnitOfWork> _logger;
    private IDbContextTransaction? _transaction;
    private bool _disposed = false;

    // Lazy-loaded repositories
    private IGenericRepository<UserModel>? _users;
    private IGenericRepository<ApplicationModel>? _applications;
    private IGenericRepository<UserApplicationModel>? _userApplications;
    private IGenericRepository<AuditLogModel>? _auditLogs;
    private IGenericRepository<UserActivityLogModel>? _userActivityLogs;
    private IGenericRepository<ApplicationLogModel>? _applicationLogs;
    private IGenericRepository<TemporaryUserModel>? _temporaryUsers;
    private IGenericRepository<UserRecoveryRequestModel>? _userRecoveryRequests;
    private IGenericRepository<ApplicationConnectionModel>? _applicationConnections;
    private IGenericRepository<HumanResourceModel>? _humanResources;
    private IGenericRepository<DataSourceModel>? _dataSources;
    private IGenericRepository<UserRoleModel>? _userRoles;

    public UnitOfWork(
        SentinelContext context,
        ILogger<UnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IGenericRepository<UserModel> Users => 
        _users ??= new GenericRepository<UserModel>(_context, 
            _context.GetService<ILogger<GenericRepository<UserModel>>>()!);

    public IGenericRepository<ApplicationModel> Applications => 
        _applications ??= new GenericRepository<ApplicationModel>(_context, 
            _context.GetService<ILogger<GenericRepository<ApplicationModel>>>()!);

    public IGenericRepository<UserApplicationModel> UserApplications => 
        _userApplications ??= new GenericRepository<UserApplicationModel>(_context, 
            _context.GetService<ILogger<GenericRepository<UserApplicationModel>>>()!);

    public IGenericRepository<AuditLogModel> AuditLogs => 
        _auditLogs ??= new GenericRepository<AuditLogModel>(_context, 
            _context.GetService<ILogger<GenericRepository<AuditLogModel>>>()!);

    public IGenericRepository<UserActivityLogModel> UserActivityLogs => 
        _userActivityLogs ??= new GenericRepository<UserActivityLogModel>(_context, 
            _context.GetService<ILogger<GenericRepository<UserActivityLogModel>>>()!);

    public IGenericRepository<ApplicationLogModel> ApplicationLogs => 
        _applicationLogs ??= new GenericRepository<ApplicationLogModel>(_context, 
            _context.GetService<ILogger<GenericRepository<ApplicationLogModel>>>()!);

    public IGenericRepository<TemporaryUserModel> TemporaryUsers => 
        _temporaryUsers ??= new GenericRepository<TemporaryUserModel>(_context, 
            _context.GetService<ILogger<GenericRepository<TemporaryUserModel>>>()!);

    public IGenericRepository<UserRecoveryRequestModel> UserRecoveryRequests => 
        _userRecoveryRequests ??= new GenericRepository<UserRecoveryRequestModel>(_context, 
            _context.GetService<ILogger<GenericRepository<UserRecoveryRequestModel>>>()!);

    public IGenericRepository<ApplicationConnectionModel> ApplicationConnections => 
        _applicationConnections ??= new GenericRepository<ApplicationConnectionModel>(_context, 
            _context.GetService<ILogger<GenericRepository<ApplicationConnectionModel>>>()!);

    public IGenericRepository<HumanResourceModel> HumanResources => 
        _humanResources ??= new GenericRepository<HumanResourceModel>(_context, 
            _context.GetService<ILogger<GenericRepository<HumanResourceModel>>>()!);

    public IGenericRepository<DataSourceModel> DataSources => 
        _dataSources ??= new GenericRepository<DataSourceModel>(_context, 
            _context.GetService<ILogger<GenericRepository<DataSourceModel>>>()!);

    public IGenericRepository<UserRoleModel> UserRoles => 
        _userRoles ??= new GenericRepository<UserRoleModel>(_context, 
            _context.GetService<ILogger<GenericRepository<UserRoleModel>>>()!);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Saved {Changes} changes to database", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes through Unit of Work");
            throw;
        }
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_transaction != null)
            {
                _logger.LogWarning("Transaction already exists. Rolling back existing transaction.");
                await _transaction.RollbackAsync(cancellationToken);
                await _transaction.DisposeAsync();
            }

            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            _logger.LogDebug("Database transaction started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error beginning transaction");
            throw;
        }
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_transaction == null)
            {
                _logger.LogWarning("No active transaction to commit");
                return;
            }

            await _transaction.CommitAsync(cancellationToken);
            _logger.LogDebug("Database transaction committed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing transaction");
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_transaction == null)
            {
                _logger.LogWarning("No active transaction to rollback");
                return;
            }

            await _transaction.RollbackAsync(cancellationToken);
            _logger.LogDebug("Database transaction rolled back");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back transaction");
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _transaction?.Dispose();
            _disposed = true;
        }
    }
}

/*
REPOSITORY PATTERN BENEFITS ACHIEVED:

✅ SEPARATION OF CONCERNS:
   - Business logic separated from data access
   - Services no longer directly access DbContext
   - Clean architecture with proper abstraction layers

✅ TESTABILITY:
   - Easy to mock repositories for unit testing
   - Business logic can be tested without database
   - Repository can be tested independently

✅ MAINTAINABILITY:
   - Centralized data access patterns
   - Consistent error handling across all entities
   - Single point of change for data access logic

✅ TRANSACTION MANAGEMENT:
   - Unit of Work pattern manages complex transactions
   - Automatic rollback on failures
   - Proper resource disposal

✅ PERFORMANCE:
   - Lazy loading of repositories
   - Bulk operations support
   - Optimized query patterns

✅ SCALABILITY:
   - Easy to add caching layer
   - Can switch to different data providers
   - Repository implementations can be optimized independently
*/