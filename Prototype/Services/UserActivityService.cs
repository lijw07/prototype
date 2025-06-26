using Microsoft.EntityFrameworkCore;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class UserActivityService(
    SentinelContext context,
    IDeviceInformationService deviceInfoService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<UserActivityService> logger) : IUserActivityService
{
    public async Task CreateUserActivityLogAsync(Guid userId, ActionTypeEnum action, string description)
    {
        var user = await context.Users.FindAsync(userId);
        if (user != null)
        {
            var httpContext = httpContextAccessor.HttpContext;
            var ipAddress = deviceInfoService.GetClientIpAddress(httpContext);
            var deviceInfo = deviceInfoService.GetDeviceInformation(httpContext);

            var activityLog = new UserActivityLogModel
            {
                UserActivityLogId = Guid.NewGuid(),
                UserId = userId,
                User = user,
                IpAddress = ipAddress,
                DeviceInformation = deviceInfo,
                ActionType = action,
                Description = description,
                Timestamp = DateTime.UtcNow
            };

            context.UserActivityLogs.Add(activityLog);
            await context.SaveChangesAsync();
            logger.LogInformation("User activity log created for user {UserId}: {Action}", userId, action);
        }
    }

    public async Task CreateAuditLogAsync(Guid userId, ActionTypeEnum action, string description)
    {
        var user = await context.Users.FindAsync(userId);
        if (user != null)
        {
            var httpContext = httpContextAccessor.HttpContext;
            var ipAddress = deviceInfoService.GetClientIpAddress(httpContext);

            var auditLog = new AuditLogModel
            {
                AuditLogId = Guid.NewGuid(),
                UserId = userId,
                User = user,
                ActionType = action,
                Description = description,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            context.AuditLogs.Add(auditLog);
            await context.SaveChangesAsync();
            logger.LogInformation("Audit log created for user {UserId}: {Action}", userId, action);
        }
    }

    public async Task<List<string>> GetUserActivityHistoryAsync(Guid userId, int limit = 50)
    {
        var activities = await context.UserActivityLogs
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.Timestamp)
            .Take(limit)
            .Select(log => $"{log.Timestamp:yyyy-MM-dd HH:mm:ss} - {log.ActionType} - {log.Description}")
            .ToListAsync();

        return activities;
    }

    public async Task<List<string>> GetUserAuditHistoryAsync(Guid userId, int limit = 50)
    {
        var audits = await context.AuditLogs
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.Timestamp)
            .Take(limit)
            .Select(log => $"{log.Timestamp:yyyy-MM-dd HH:mm:ss} - {log.ActionType} - {log.Description}")
            .ToListAsync();

        return audits;
    }
}