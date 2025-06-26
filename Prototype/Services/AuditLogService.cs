using Prototype.Data;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Constants;

namespace Prototype.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly SentinelContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(
            SentinelContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditLogService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task LogUserActionAsync(Guid userId, ActionTypeEnum actionType, string metadata, string? description = null)
        {
            try
            {
                var auditLog = CreateAuditLog(userId, actionType, metadata);
                _context.AuditLogs.Add(auditLog);

                if (!string.IsNullOrEmpty(description))
                {
                    var activityLog = CreateUserActivityLog(userId, actionType, description);
                    _context.UserActivityLogs.Add(activityLog);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log user action. UserId: {UserId}, ActionType: {ActionType}", userId, actionType);
                throw;
            }
        }

        public async Task LogApplicationActionAsync(Guid userId, Guid applicationId, ActionTypeEnum actionType, string metadata)
        {
            try
            {
                var auditLog = CreateAuditLog(userId, actionType, metadata);
                _context.AuditLogs.Add(auditLog);

                var applicationLog = CreateApplicationLog(applicationId, actionType, metadata);
                _context.ApplicationLogs.Add(applicationLog);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log application action. UserId: {UserId}, ApplicationId: {ApplicationId}, ActionType: {ActionType}", 
                    userId, applicationId, actionType);
                throw;
            }
        }

        public async Task LogAuditOnlyAsync(Guid userId, ActionTypeEnum actionType, string metadata)
        {
            try
            {
                var auditLog = CreateAuditLog(userId, actionType, metadata);
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log audit action. UserId: {UserId}, ActionType: {ActionType}", userId, actionType);
                throw;
            }
        }

        public async Task LogUserActivityOnlyAsync(Guid userId, ActionTypeEnum actionType, string description)
        {
            try
            {
                var activityLog = CreateUserActivityLog(userId, actionType, description);
                _context.UserActivityLogs.Add(activityLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log user activity. UserId: {UserId}, ActionType: {ActionType}", userId, actionType);
                throw;
            }
        }

        public async Task LogFailedLoginAsync(string username, string reason)
        {
            try
            {
                var metadata = $"Failed login attempt for username: {username}. Reason: {reason}";
                var auditLog = new AuditLogModel
                {
                    AuditLogId = Guid.NewGuid(),
                    UserId = Guid.Empty, // No user ID for failed logins
                    User = null,
                    ActionType = ActionTypeEnum.FailedLogin,
                    Metadata = metadata,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log failed login attempt for username: {Username}", username);
                throw;
            }
        }

        private AuditLogModel CreateAuditLog(Guid userId, ActionTypeEnum actionType, string metadata)
        {
            return new AuditLogModel
            {
                AuditLogId = Guid.NewGuid(),
                UserId = userId,
                User = null, // Avoiding navigation property to prevent tracking issues
                ActionType = actionType,
                Metadata = metadata,
                CreatedAt = DateTime.UtcNow
            };
        }

        private UserActivityLogModel CreateUserActivityLog(Guid userId, ActionTypeEnum actionType, string description)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? ApplicationConstants.DefaultIpAddress;
            var deviceInfo = httpContext?.Request.Headers.UserAgent.ToString() ?? ApplicationConstants.DefaultDeviceInfo;

            return new UserActivityLogModel
            {
                UserActivityLogId = Guid.NewGuid(),
                UserId = userId,
                User = null, // Avoiding navigation property
                IpAddress = ipAddress,
                DeviceInformation = deviceInfo,
                ActionType = actionType,
                Description = description,
                Timestamp = DateTime.UtcNow
            };
        }

        private ApplicationLogModel CreateApplicationLog(Guid applicationId, ActionTypeEnum actionType, string metadata)
        {
            return new ApplicationLogModel
            {
                ApplicationLogId = Guid.NewGuid(),
                ApplicationId = applicationId,
                Application = null, // Avoiding navigation property
                ActionType = actionType,
                Metadata = metadata,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public string FormatUserUpdateMetadata(string field, string oldValue, string newValue)
        {
            return $"User {field} updated from '{oldValue}' to '{newValue}'";
        }

        public string FormatApplicationMetadata(string applicationName, string action)
        {
            return $"Application '{applicationName}' {action}";
        }

        public string FormatPasswordChangeMetadata(string username)
        {
            return $"Password changed for user: {username}";
        }

        public string FormatConnectionMetadata(string connectionName, string action, string? details = null)
        {
            var metadata = $"Connection '{connectionName}' {action}";
            return string.IsNullOrEmpty(details) ? metadata : $"{metadata}. {details}";
        }
    }
}