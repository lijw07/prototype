using Microsoft.EntityFrameworkCore;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Common;

public interface IAuditService
{
    Task LogUserActivityAsync(Guid userId, ActionTypeEnum actionType, string description, HttpContext? httpContext = null);
    Task LogAuditAsync(Guid userId, ActionTypeEnum actionType, string metadata);
    Task LogApplicationActivityAsync(Guid applicationId, Guid userId, ActionTypeEnum actionType, string metadata);
    Task<List<UserActivityLogModel>> GetUserActivityLogsAsync(Guid userId, int page = 1, int pageSize = 50);
    Task<List<AuditLogModel>> GetAuditLogsAsync(int page = 1, int pageSize = 50);
}

public class AuditService : IAuditService
{
    private readonly SentinelContext _context;
    private readonly ILogger<AuditService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(
        SentinelContext context,
        ILogger<AuditService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogUserActivityAsync(Guid userId, ActionTypeEnum actionType, string description, HttpContext? httpContext = null)
    {
        try
        {
            var context = httpContext ?? _httpContextAccessor.HttpContext;
            var ipAddress = GetClientIpAddress(context);
            var deviceInfo = GetDeviceInformation(context);

            var activityLog = new UserActivityLogModel
            {
                UserActivityLogId = Guid.NewGuid(),
                UserId = userId,
                User = null, // Avoid navigation property issues
                IpAddress = ipAddress,
                DeviceInformation = deviceInfo,
                ActionType = actionType,
                Description = description,
                Timestamp = DateTime.UtcNow
            };

            _context.UserActivityLogs.Add(activityLog);
            await _context.SaveChangesAsync();

            _logger.LogDebug("User activity logged: {UserId} - {ActionType} - {Description}", 
                userId, actionType, description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log user activity for user {UserId}: {Error}", userId, ex.Message);
            // Don't rethrow - audit logging failures shouldn't break the main operation
        }
    }

    public async Task LogAuditAsync(Guid userId, ActionTypeEnum actionType, string metadata)
    {
        try
        {
            var auditLog = new AuditLogModel
            {
                AuditLogId = Guid.NewGuid(),
                UserId = userId,
                User = null, // Avoid navigation property issues
                ActionType = actionType,
                Metadata = metadata,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Audit log created: {UserId} - {ActionType} - {Metadata}", 
                userId, actionType, metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log for user {UserId}: {Error}", userId, ex.Message);
            // Don't rethrow - audit logging failures shouldn't break the main operation
        }
    }

    public async Task LogApplicationActivityAsync(Guid applicationId, Guid userId, ActionTypeEnum actionType, string metadata)
    {
        try
        {
            var applicationLog = new ApplicationLogModel
            {
                ApplicationLogId = Guid.NewGuid(),
                ApplicationId = applicationId,
                Application = null, // Avoid navigation property issues
                ActionType = actionType,
                Metadata = metadata,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ApplicationLogs.Add(applicationLog);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Application activity logged: {ApplicationId} - {UserId} - {ActionType}", 
                applicationId, userId, actionType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log application activity for app {ApplicationId}: {Error}", 
                applicationId, ex.Message);
            // Don't rethrow - audit logging failures shouldn't break the main operation
        }
    }

    public async Task<List<UserActivityLogModel>> GetUserActivityLogsAsync(Guid userId, int page = 1, int pageSize = 50)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            
            return await _context.UserActivityLogs
                .Where(log => log.UserId == userId)
                .OrderByDescending(log => log.Timestamp)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user activity logs for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<AuditLogModel>> GetAuditLogsAsync(int page = 1, int pageSize = 50)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            
            return await _context.AuditLogs
                .Include(log => log.User)
                .OrderByDescending(log => log.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            throw;
        }
    }

    private string GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null)
            return "Unknown";

        // Check for forwarded IP first (for reverse proxy scenarios)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check for real IP header
        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to remote IP address
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private string GetDeviceInformation(HttpContext? httpContext)
    {
        if (httpContext == null)
            return "Unknown";

        var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault();
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown";

        // Extract basic device information from User-Agent
        var deviceInfo = new List<string>();

        // Check for mobile devices
        if (userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Mobile");
        else if (userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Tablet");
        else
            deviceInfo.Add("Desktop");

        // Extract browser information
        if (userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Chrome");
        else if (userAgent.Contains("Firefox", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Firefox");
        else if (userAgent.Contains("Safari", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Safari");
        else if (userAgent.Contains("Edge", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Edge");

        // Extract OS information
        if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Windows");
        else if (userAgent.Contains("Mac", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("macOS");
        else if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Linux");
        else if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Android");
        else if (userAgent.Contains("iOS", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("iOS");

        return deviceInfo.Count > 0 ? string.Join(", ", deviceInfo) : "Unknown";
    }
}