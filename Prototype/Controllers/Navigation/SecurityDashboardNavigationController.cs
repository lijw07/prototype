using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.DTOs.Cache;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation;

[Authorize]
[Route("api/security-dashboard")]
[ApiController]
public class SecurityDashboardNavigationController(
    SentinelContext context,
    IAuthenticatedUserAccessor userAccessor,
    ICacheService cacheService,
    ILogger<SecurityDashboardNavigationController> logger)
    : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> GetSecurityOverview()
    {
        try
        {
            var currentUser = await userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            // Check cache first
            var cacheKey = "security:dashboard:overview";
            var cachedData = await cacheService.GetAsync<object>(cacheKey);
            
            if (cachedData != null)
            {
                logger.LogDebug("Security dashboard cache hit for user: {Username}", currentUser.Username);
                return Ok(new { success = true, data = cachedData, fromCache = true });
            }

            var now = DateTime.UtcNow;
            var last24Hours = now.AddHours(-24);
            var last7Days = now.AddDays(-7);
            var last30Days = now.AddDays(-30);

            // Security metrics from the last 24 hours
            var failedLoginsToday = await context.UserActivityLogs
                .Where(log => log.ActionType == ActionTypeEnum.FailedLogin && log.Timestamp >= last24Hours)
                .CountAsync();

            var successfulLoginsToday = await context.UserActivityLogs
                .Where(log => log.ActionType == ActionTypeEnum.Login && log.Timestamp >= last24Hours)
                .CountAsync();

            // Failed logins by location (IP-based)
            var failedLoginsByIp = await context.UserActivityLogs
                .Where(log => log.ActionType == ActionTypeEnum.FailedLogin && log.Timestamp >= last7Days)
                .GroupBy(log => log.IpAddress)
                .Select(g => new { IpAddress = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            // Security events in last 7 days
            var securityEvents = await context.UserActivityLogs
                .Where(log => log.Timestamp >= last7Days && 
                             (log.ActionType == ActionTypeEnum.FailedLogin ||
                              log.ActionType == ActionTypeEnum.Login ||
                              log.ActionType == ActionTypeEnum.ChangePassword ||
                              log.ActionType == ActionTypeEnum.ApplicationRemoved ||
                              log.ActionType == ActionTypeEnum.RoleDeleted))
                .OrderByDescending(log => log.Timestamp)
                .Take(20)
                .Select(log => new {
                    log.UserActivityLogId,
                    log.ActionType,
                    log.Description,
                    log.Timestamp,
                    log.IpAddress,
                    log.UserId
                })
                .ToListAsync();

            // Active sessions (approximation based on recent activity)
            var activeSessions = await context.UserActivityLogs
                .Where(log => log.Timestamp >= last24Hours)
                .Select(log => log.UserId)
                .Distinct()
                .CountAsync();

            // Unverified users
            var unverifiedUsers = await context.TemporaryUsers.CountAsync();

            // Application access patterns
            var applicationChanges = await context.ApplicationLogs
                .Where(log => log.CreatedAt >= last7Days)
                .CountAsync();

            // Risk indicators
            var suspiciousIps = failedLoginsByIp.Where(x => x.Count >= 5).ToList();
            
            var riskScore = CalculateRiskScore(failedLoginsToday, suspiciousIps.Count, unverifiedUsers);

            var securityData = new
            {
                overview = new
                {
                    riskScore = riskScore,
                    riskLevel = GetRiskLevel(riskScore),
                    activeSessions = activeSessions,
                    unverifiedUsers = unverifiedUsers,
                    failedLoginsToday = failedLoginsToday,
                    successfulLoginsToday = successfulLoginsToday,
                    applicationChanges = applicationChanges
                },
                threats = new
                {
                    suspiciousIps = suspiciousIps.Take(5),
                    failedLoginsByIp = failedLoginsByIp.Take(5)
                },
                recentEvents = securityEvents,
                trends = new
                {
                    failedLoginsLast7Days = await GetFailedLoginsTrend(last7Days),
                    successfulLoginsLast7Days = await GetSuccessfulLoginsTrend(last7Days)
                }
            };

            // Cache for 10 minutes (security data should be relatively fresh)
            await cacheService.SetAsync(cacheKey, securityData, TimeSpan.FromMinutes(10));
            logger.LogDebug("Security dashboard cached for user: {Username}", currentUser.Username);

            return Ok(new { success = true, data = securityData, fromCache = false });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving security overview");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("failed-logins")]
    public async Task<IActionResult> GetFailedLogins([FromQuery] int days = 7)
    {
        try
        {
            var currentUser = await userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            // Check cache first - include days parameter in cache key
            var cacheKey = $"security:failed-logins:{days}";
            var cachedData = await cacheService.GetAsync<object>(cacheKey);
            
            if (cachedData != null)
            {
                logger.LogDebug("Failed logins cache hit for user: {Username}, days: {Days}", currentUser.Username, days);
                return Ok(new { success = true, data = cachedData, fromCache = true });
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            var failedLogins = await context.UserActivityLogs
                .Where(log => log.ActionType == ActionTypeEnum.FailedLogin && log.Timestamp >= cutoffDate)
                .OrderByDescending(log => log.Timestamp)
                .Select(log => new {
                    log.UserActivityLogId,
                    log.Description,
                    log.Timestamp,
                    log.IpAddress,
                    log.DeviceInformation,
                    log.UserId
                })
                .ToListAsync();

            // Cache for 5 minutes (failed logins data changes more frequently)
            await cacheService.SetAsync(cacheKey, failedLogins, TimeSpan.FromMinutes(5));
            logger.LogDebug("Failed logins cached for user: {Username}, days: {Days}", currentUser.Username, days);

            return Ok(new { success = true, data = failedLogins, fromCache = false });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving failed logins");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    private int CalculateRiskScore(int failedLoginsToday, int suspiciousIpsCount, int unverifiedUsers)
    {
        int score = 0;
        
        // Failed logins contribution (0-40 points)
        score += Math.Min(failedLoginsToday * 2, 40);
        
        // Suspicious IPs contribution (0-30 points)
        score += Math.Min(suspiciousIpsCount * 10, 30);
        
        // Unverified users contribution (0-30 points)
        score += Math.Min(unverifiedUsers * 2, 30);
        
        return Math.Min(score, 100);
    }

    private string GetRiskLevel(int riskScore)
    {
        return riskScore switch
        {
            >= 70 => "HIGH",
            >= 40 => "MEDIUM",
            >= 20 => "LOW",
            _ => "MINIMAL"
        };
    }

    private async Task<List<object>> GetFailedLoginsTrend(DateTime startDate)
    {
        var dailyData = await context.UserActivityLogs
            .Where(log => log.ActionType == ActionTypeEnum.FailedLogin && log.Timestamp >= startDate)
            .GroupBy(log => log.Timestamp.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .Take(365) // Limit to max 365 days of trend data
            .ToListAsync();

        return dailyData.Cast<object>().ToList();
    }

    private async Task<List<object>> GetSuccessfulLoginsTrend(DateTime startDate)
    {
        var dailyData = await context.UserActivityLogs
            .Where(log => log.ActionType == ActionTypeEnum.Login && log.Timestamp >= startDate)
            .GroupBy(log => log.Timestamp.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .Take(365) // Limit to max 365 days of trend data
            .ToListAsync();

        return dailyData.Cast<object>().ToList();
    }
}