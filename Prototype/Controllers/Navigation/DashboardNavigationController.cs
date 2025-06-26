using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.DTOs.Cache;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation;

[Route("[controller]")]
public class DashboardNavigationController(
    SentinelContext context,
    IAuthenticatedUserAccessor userAccessor,
    ICacheService cacheService,
    ILogger<DashboardNavigationController> logger)
    : ControllerBase
{
    [HttpGet("statistics")]
    public async Task<IActionResult> GetDashboardStatistics()
    {
        try
        {
            var currentUser = await userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            // Check cache first for user-specific dashboard data
            var userDashboardCacheKey = $"dashboard:user:{currentUser.UserId}";
            var cachedUserDashboard = await cacheService.GetSecureAsync<UserDashboardCacheDto>(userDashboardCacheKey, currentUser.UserId);
            
            // Check cache for global dashboard statistics
            var globalDashboardCacheKey = "dashboard:statistics:global";
            var cachedGlobalStats = await cacheService.GetAsync<DashboardStatsCacheDto>(globalDashboardCacheKey);

            UserDashboardCacheDto userDashboard;
            DashboardStatsCacheDto globalStats;

            // Load user-specific data if not cached
            if (cachedUserDashboard == null)
            {
                userDashboard = await LoadUserDashboardDataAsync(currentUser.UserId);
                await cacheService.SetSecureAsync(userDashboardCacheKey, userDashboard, currentUser.UserId, TimeSpan.FromMinutes(15));
            }
            else
            {
                userDashboard = cachedUserDashboard;
            }

            // Load global statistics if not cached
            if (cachedGlobalStats == null || !cachedGlobalStats.IsFresh)
            {
                globalStats = await LoadGlobalDashboardStatsAsync();
                await cacheService.SetAsync(globalDashboardCacheKey, globalStats, TimeSpan.FromMinutes(15));
            }
            else
            {
                globalStats = cachedGlobalStats;
            }

            var statistics = new
            {
                success = true,
                data = new
                {
                    totalApplications = userDashboard.TotalApplications,
                    totalRoles = globalStats.TotalRoles,
                    totalUsers = globalStats.TotalUsers + globalStats.TotalTemporaryUsers,
                    totalVerifiedUsers = globalStats.TotalUsers,
                    totalTemporaryUsers = globalStats.TotalTemporaryUsers,
                    recentActivity = userDashboard.RecentActivityCount,
                    systemHealth = "healthy", // You can implement actual health checks later
                    recentActivities = userDashboard.RecentActivities,
                    isFresh = globalStats.IsFresh && userDashboard.IsFresh,
                    lastUpdated = DateTime.UtcNow.Min(globalStats.GeneratedAt).Min(userDashboard.GeneratedAt)
                }
            };

            logger.LogInformation("Dashboard statistics retrieved for user: {Username} (Cache hit: Global={GlobalCached}, User={UserCached})", 
                currentUser.Username, cachedGlobalStats != null, cachedUserDashboard != null);
            
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving dashboard statistics");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    private async Task<UserDashboardCacheDto> LoadUserDashboardDataAsync(Guid userId)
    {
        // Get user's applications (the ones they have access to)
        var userApplications = await context.UserApplications
            .Where(ua => ua.UserId == userId)
            .Include(ua => ua.Application)
            .Include(ua => ua.ApplicationConnection)
            .ToListAsync();

        // Get recent activity count (last 24 hours for this user)
        var twentyFourHoursAgo = DateTime.UtcNow.AddHours(-24);
        var recentActivity = await context.UserActivityLogs
            .Where(log => log.UserId == userId && log.Timestamp >= twentyFourHoursAgo)
            .CountAsync();

        // Get recent activity details (last 10 activities for this user)
        var recentActivities = await context.UserActivityLogs
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.Timestamp)
            .Take(10)
            .Select(log => new UserActivityCacheDto
            {
                ActionType = log.ActionType.ToString(),
                Description = log.Description,
                Timestamp = log.Timestamp,
                IpAddress = log.IpAddress
            })
            .ToListAsync();

        return new UserDashboardCacheDto
        {
            TotalApplications = userApplications.Count,
            RecentActivityCount = recentActivity,
            RecentActivities = recentActivities.Select(activity => new
            {
                actionType = activity.ActionType,
                description = activity.Description,
                timestamp = activity.Timestamp,
                timeAgo = GetTimeAgo(activity.Timestamp),
                ipAddress = activity.IpAddress
            }).ToList(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    private async Task<DashboardStatsCacheDto> LoadGlobalDashboardStatsAsync()
    {
        // Execute all global queries in parallel for better performance
        var tasks = new[]
        {
            context.Users.CountAsync(),
            context.TemporaryUsers.CountAsync(),
            context.UserRoles.CountAsync(),
            context.UserActivityLogs
                .Where(log => log.ActivityDate >= DateTime.UtcNow.AddDays(-30))
                .CountAsync(),
            context.Applications.CountAsync()
        };

        await Task.WhenAll(tasks);

        return new DashboardStatsCacheDto
        {
            TotalUsers = tasks[0].Result,
            TotalTemporaryUsers = tasks[1].Result,
            TotalRoles = tasks[2].Result,
            RecentActivityCount = tasks[3].Result,
            TotalApplications = tasks[4].Result,
            GeneratedAt = DateTime.UtcNow
        };
    }

    private string GetTimeAgo(DateTime timestamp)
    {
        var timeSpan = DateTime.UtcNow - timestamp;
        
        if (timeSpan.TotalMinutes < 1)
            return "Just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes == 1 ? "" : "s")} ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours == 1 ? "" : "s")} ago";
        if (timeSpan.TotalDays < 30)
            return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays == 1 ? "" : "s")} ago";
        
        return timestamp.ToString("MMM dd, yyyy");
    }
}