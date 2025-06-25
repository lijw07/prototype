using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.Enum;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation;

[Route("[controller]")]
public class DashboardNavigationController(
    SentinelContext context,
    IAuthenticatedUserAccessor userAccessor,
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

            // Get user's applications (the ones they have access to)
            var userApplications = await context.UserApplications
                .Where(ua => ua.UserId == currentUser.UserId)
                .Include(ua => ua.Application)
                .Include(ua => ua.ApplicationConnection)
                .ToListAsync();

            // Get total applications this user has access to
            var totalApplications = userApplications.Count;

            // Count active connections (assuming active means recently used - within last 30 days)
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var activeConnections = await context.UserActivityLogs
                .Where(log => log.UserId == currentUser.UserId && 
                             log.Timestamp >= thirtyDaysAgo &&
                             (log.ActionType == ActionTypeEnum.ApplicationAdded || 
                              log.ActionType == ActionTypeEnum.ApplicationUpdated))
                .Select(log => log.Description)
                .Distinct()
                .CountAsync();

            // Get user statistics breakdown
            var totalVerifiedUsers = await context.Users.CountAsync();
            var totalTemporaryUsers = await context.TemporaryUsers.CountAsync();
            var totalUsers = totalVerifiedUsers + totalTemporaryUsers;
            
            // Get total roles in the system
            var totalRoles = await context.UserRoles.CountAsync();

            // Get recent activity count (last 24 hours for this user)
            var twentyFourHoursAgo = DateTime.UtcNow.AddHours(-24);
            var recentActivity = await context.UserActivityLogs
                .Where(log => log.UserId == currentUser.UserId && log.Timestamp >= twentyFourHoursAgo)
                .CountAsync();

            // Get recent activity details (last 10 activities for this user)
            var recentActivities = await context.UserActivityLogs
                .Where(log => log.UserId == currentUser.UserId)
                .OrderByDescending(log => log.Timestamp)
                .Take(10)
                .Select(log => new
                {
                    log.ActionType,
                    log.Description,
                    log.Timestamp,
                    log.IpAddress
                })
                .ToListAsync();

            var statistics = new
            {
                success = true,
                data = new
                {
                    totalApplications = totalApplications,
                    totalRoles = totalRoles,
                    totalUsers = totalUsers,
                    totalVerifiedUsers = totalVerifiedUsers,
                    totalTemporaryUsers = totalTemporaryUsers,
                    recentActivity = recentActivity,
                    systemHealth = "healthy", // You can implement actual health checks later
                    recentActivities = recentActivities.Select(activity => new
                    {
                        actionType = activity.ActionType.ToString(),
                        description = activity.Description,
                        timestamp = activity.Timestamp,
                        timeAgo = GetTimeAgo(activity.Timestamp),
                        ipAddress = activity.IpAddress
                    })
                }
            };

            logger.LogInformation("Dashboard statistics retrieved for user: {Username}", currentUser.Username);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving dashboard statistics");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
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