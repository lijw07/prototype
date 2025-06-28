using Prototype.Enum;

namespace Prototype.Services
{
    public interface IAuditLogService
    {
        /// <summary>
        /// Logs both audit log and user activity log for a user action
        /// </summary>
        Task LogUserActionAsync(Guid userId, ActionTypeEnum actionType, string metadata, string? description = null);

        /// <summary>
        /// Logs audit log and application log for application-related actions
        /// </summary>
        Task LogApplicationActionAsync(Guid userId, Guid applicationId, ActionTypeEnum actionType, string metadata);

        /// <summary>
        /// Logs only an audit log entry (for simple actions that don't need activity tracking)
        /// </summary>
        Task LogAuditOnlyAsync(Guid userId, ActionTypeEnum actionType, string metadata);

        /// <summary>
        /// Logs only a user activity log entry
        /// </summary>
        Task LogUserActivityOnlyAsync(Guid userId, ActionTypeEnum actionType, string description);

        /// <summary>
        /// Logs failed login attempts (no user ID required)
        /// </summary>
        Task LogFailedLoginAsync(string username, string reason);

        /// <summary>
        /// Helper method to format user update metadata consistently
        /// </summary>
        string FormatUserUpdateMetadata(string field, string oldValue, string newValue);

        /// <summary>
        /// Helper method to format application-related metadata
        /// </summary>
        string FormatApplicationMetadata(string applicationName, string action);

        /// <summary>
        /// Helper method to format password change metadata
        /// </summary>
        string FormatPasswordChangeMetadata(string username);

        /// <summary>
        /// Helper method to format connection-related metadata
        /// </summary>
        string FormatConnectionMetadata(string connectionName, string action, string? details = null);

        /// <summary>
        /// Creates an audit log entry
        /// </summary>
        Task CreateAuditLogAsync(Guid userId, ActionTypeEnum actionType, string metadata);

        /// <summary>
        /// Creates a user activity log entry with IP and device information
        /// </summary>
        Task CreateUserActivityLogAsync(Guid userId, ActionTypeEnum actionType, string description, string ipAddress, string deviceInfo);
    }
}