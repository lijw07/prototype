using Microsoft.EntityFrameworkCore;
using Prototype.DTOs.Cache;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class DashboardService(
    SentinelContext context,
    ILogger<DashboardService> logger) : IDashboardService
{
    public async Task<DashboardStatsCacheDto> GetUserDashboardStatsAsync(Guid userId)
    {
        try
        {
            logger.LogInformation("Loading dashboard stats for user {UserId}", userId);

            // Load data in parallel for better performance
            var userAppsTask = GetUserApplicationsAsync(userId);
            var recentActivityTask = GetRecentUserActivityAsync(userId);
            var statsSummaryTask = GetUserStatsSummaryAsync(userId);
            var notificationsTask = GetUserNotificationsAsync(userId);

            await Task.WhenAll(userAppsTask, recentActivityTask, statsSummaryTask, notificationsTask);

            var userApps = await userAppsTask;
            var recentActivity = await recentActivityTask;
            var statsSummary = await statsSummaryTask;
            var notifications = await notificationsTask;

            return new DashboardStatsCacheDto
            {
                UserId = userId,
                UserApplications = userApps,
                RecentActivity = recentActivity,
                StatsSummary = statsSummary,
                Notifications = notifications,
                LoadedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading dashboard stats for user {UserId}", userId);
            throw;
        }
    }

    public async Task<object> GetUserApplicationsAsync(Guid userId)
    {
        var userApplications = await context.UserApplications
            .Where(ua => ua.UserId == userId)
            .Include(ua => ua.Application)
            .Include(ua => ua.ApplicationConnection)
            .Select(ua => new
            {
                ua.Application.ApplicationId,
                ua.Application.ApplicationName,
                ua.Application.ApplicationDescription,
                ua.Application.ApplicationDataSourceType,
                ConnectionStatus = ua.ApplicationConnection != null ? "Connected" : "Disconnected",
                ua.Application.CreatedAt,
                LastAccessed = context.UserActivityLogs
                    .Where(log => log.UserId == userId && 
                                 log.Description.Contains(ua.Application.ApplicationName))
                    .OrderByDescending(log => log.Timestamp)
                    .Select(log => log.Timestamp)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return new
        {
            applications = userApplications,
            totalCount = userApplications.Count,
            connectedCount = userApplications.Count(ua => ua.ConnectionStatus == "Connected"),
            recentlyUsed = userApplications
                .Where(ua => ua.LastAccessed.HasValue && ua.LastAccessed > DateTime.UtcNow.AddDays(-7))
                .Count()
        };
    }

    public async Task<object> GetRecentUserActivityAsync(Guid userId, int limit = 10)
    {
        var recentActivities = await context.UserActivityLogs
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.Timestamp)
            .Take(limit)
            .Select(log => new
            {
                log.ActionType,
                log.Description,
                log.Timestamp,
                log.IpAddress,
                log.DeviceInformation,
                FormattedTime = FormatTimeAgo(log.Timestamp)
            })
            .ToListAsync();

        return new
        {
            activities = recentActivities,
            totalCount = recentActivities.Count,
            lastActivity = recentActivities.FirstOrDefault()?.Timestamp,
            activitySummary = recentActivities
                .GroupBy(a => a.ActionType)
                .ToDictionary(g => g.Key.ToString(), g => g.Count())
        };
    }

    public async Task<object> GetUserStatsSummaryAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        var last30Days = now.AddDays(-30);
        var last7Days = now.AddDays(-7);

        var applicationCount = await GetUserApplicationCount(userId);
        var sessionDuration = await CalculateUserSessionDuration(userId);
        var lastLogin = await GetLastLoginTime(userId);

        var activitiesLast30Days = await context.UserActivityLogs
            .Where(log => log.UserId == userId && log.Timestamp >= last30Days)
            .CountAsync();

        var loginCount30Days = await context.UserActivityLogs
            .Where(log => log.UserId == userId && 
                         log.Timestamp >= last30Days && 
                         log.ActionType == ActionTypeEnum.Login)
            .CountAsync();

        var loginCount7Days = await context.UserActivityLogs
            .Where(log => log.UserId == userId && 
                         log.Timestamp >= last7Days && 
                         log.ActionType == ActionTypeEnum.Login)
            .CountAsync();

        return new
        {
            applicationCount = applicationCount,
            averageSessionDuration = FormatDuration(sessionDuration),
            lastLogin = lastLogin?.ToString("yyyy-MM-dd HH:mm:ss"),
            activitiesLast30Days = activitiesLast30Days,
            loginFrequency = new
            {
                last30Days = loginCount30Days,
                last7Days = loginCount7Days,
                averagePerDay = Math.Round((double)loginCount30Days / 30, 1)
            },
            accountStatus = await GetAccountStatusAsync(userId)
        };
    }

    public async Task<object> GetUserNotificationsAsync(Guid userId)
    {
        // This would be connected to a real notification system
        // For now, creating sample notifications based on user activity
        var notifications = new List<object>();

        // Check for recent failed logins
        var recentFailedLogins = await context.UserActivityLogs
            .Where(log => log.UserId == userId && 
                         log.ActionType == ActionTypeEnum.FailedLogin &&
                         log.Timestamp >= DateTime.UtcNow.AddDays(-7))
            .CountAsync();

        if (recentFailedLogins > 0)
        {
            notifications.Add(new
            {
                id = Guid.NewGuid(),
                type = "security",
                title = "Security Alert",
                message = $"You have {recentFailedLogins} failed login attempts in the last 7 days",
                severity = recentFailedLogins > 5 ? "high" : "medium",
                timestamp = DateTime.UtcNow.AddDays(-1)
            });
        }

        // Check for new applications
        var newApplications = await context.Applications
            .Where(a => a.CreatedAt >= DateTime.UtcNow.AddDays(-7))
            .CountAsync();

        if (newApplications > 0)
        {
            notifications.Add(new
            {
                id = Guid.NewGuid(),
                type = "info",
                title = "New Applications Available",
                message = $"{newApplications} new applications have been added to the system",
                severity = "low",
                timestamp = DateTime.UtcNow.AddDays(-2)
            });
        }

        return new
        {
            notifications = notifications.OrderByDescending(n => ((dynamic)n).timestamp),
            unreadCount = notifications.Count,
            hasHighPriority = notifications.Any(n => ((dynamic)n).severity == "high")
        };
    }

    public async Task<TimeSpan> CalculateUserSessionDuration(Guid userId)
    {
        var loginLogs = await context.UserActivityLogs
            .Where(log => log.UserId == userId && 
                         log.ActionType == ActionTypeEnum.Login &&
                         log.Timestamp >= DateTime.UtcNow.AddDays(-30))
            .OrderBy(log => log.Timestamp)
            .Select(log => log.Timestamp)
            .ToListAsync();

        if (loginLogs.Count < 2)
            return TimeSpan.Zero;

        var totalDuration = TimeSpan.Zero;
        for (int i = 1; i < loginLogs.Count; i++)
        {
            var sessionDuration = loginLogs[i] - loginLogs[i - 1];
            // Cap session duration at 8 hours (likely logout or timeout)
            if (sessionDuration <= TimeSpan.FromHours(8))
            {
                totalDuration += sessionDuration;
            }
        }

        return new TimeSpan(totalDuration.Ticks / Math.Max(1, loginLogs.Count - 1));
    }

    public async Task<int> GetUserApplicationCount(Guid userId)
    {
        return await context.UserApplications
            .Where(ua => ua.UserId == userId)
            .CountAsync();
    }

    public async Task<DateTime?> GetLastLoginTime(Guid userId)
    {
        return await context.UserActivityLogs
            .Where(log => log.UserId == userId && log.ActionType == ActionTypeEnum.Login)
            .OrderByDescending(log => log.Timestamp)
            .Select(log => log.Timestamp)
            .FirstOrDefaultAsync();
    }

    private async Task<object> GetAccountStatusAsync(Guid userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
            return new { status = "unknown", isActive = false };

        return new
        {
            status = user.IsActive ? "active" : "inactive",
            isActive = user.IsActive,
            role = user.Role,
            memberSince = user.CreatedAt.ToString("yyyy-MM-dd"),
            lastUpdated = user.UpdatedAt?.ToString("yyyy-MM-dd")
        };
    }

    private string FormatTimeAgo(DateTime timestamp)
    {
        var timeSpan = DateTime.UtcNow - timestamp;

        if (timeSpan.TotalMinutes < 1)
            return "Just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} minutes ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} hours ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays} days ago";
        
        return timestamp.ToString("MMM dd, yyyy");
    }

    private string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalMinutes < 1)
            return "< 1 minute";
        if (duration.TotalHours < 1)
            return $"{(int)duration.TotalMinutes} minutes";
        
        return $"{(int)duration.TotalHours}h {duration.Minutes}m";
    }
}