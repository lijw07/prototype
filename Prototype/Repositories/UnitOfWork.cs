using Microsoft.EntityFrameworkCore.Storage;
using Prototype.Data;
using Prototype.Models;

namespace Prototype.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly SentinelContext _context;
        private IDbContextTransaction? _transaction;
        private bool _disposed = false;

        private IRepository<UserModel>? _users;
        private IRepository<ApplicationModel>? _applications;
        private IRepository<UserApplicationModel>? _userApplications;
        private IRepository<UserActivityLogModel>? _userActivityLogs;
        private IRepository<AuditLogModel>? _auditLogs;
        private IRepository<ApplicationLogModel>? _applicationLogs;
        private IRepository<BulkUploadHistoryModel>? _bulkUploadHistories;
        private IRepository<UserRecoveryRequestModel>? _userRecoveryRequests;
        private IRepository<TemporaryUserModel>? _temporaryUsers;
        private IRepository<ApplicationConnectionModel>? _applicationConnections;
        private IRepository<DataSourceModel>? _dataSources;
        private IRepository<HumanResourceModel>? _humanResources;
        private IRepository<AuthenticationModel>? _authentications;
        private IRepository<UserRequestModel>? _userRequests;

        public UnitOfWork(SentinelContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IRepository<UserModel> Users => 
            _users ??= new Repository<UserModel>(_context);

        public IRepository<ApplicationModel> Applications => 
            _applications ??= new Repository<ApplicationModel>(_context);

        public IRepository<UserApplicationModel> UserApplications => 
            _userApplications ??= new Repository<UserApplicationModel>(_context);

        public IRepository<UserActivityLogModel> UserActivityLogs => 
            _userActivityLogs ??= new Repository<UserActivityLogModel>(_context);

        public IRepository<AuditLogModel> AuditLogs => 
            _auditLogs ??= new Repository<AuditLogModel>(_context);

        public IRepository<ApplicationLogModel> ApplicationLogs => 
            _applicationLogs ??= new Repository<ApplicationLogModel>(_context);

        public IRepository<BulkUploadHistoryModel> BulkUploadHistories => 
            _bulkUploadHistories ??= new Repository<BulkUploadHistoryModel>(_context);

        public IRepository<UserRecoveryRequestModel> UserRecoveryRequests => 
            _userRecoveryRequests ??= new Repository<UserRecoveryRequestModel>(_context);

        public IRepository<TemporaryUserModel> TemporaryUsers => 
            _temporaryUsers ??= new Repository<TemporaryUserModel>(_context);

        public IRepository<ApplicationConnectionModel> ApplicationConnections => 
            _applicationConnections ??= new Repository<ApplicationConnectionModel>(_context);

        public IRepository<DataSourceModel> DataSources => 
            _dataSources ??= new Repository<DataSourceModel>(_context);

        public IRepository<HumanResourceModel> HumanResources => 
            _humanResources ??= new Repository<HumanResourceModel>(_context);

        public IRepository<AuthenticationModel> Authentications => 
            _authentications ??= new Repository<AuthenticationModel>(_context);

        public IRepository<UserRequestModel> UserRequests => 
            _userRequests ??= new Repository<UserRequestModel>(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public void BeginTransaction()
        {
            _transaction = _context.Database.BeginTransaction();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transaction?.Dispose();
                    _context?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}