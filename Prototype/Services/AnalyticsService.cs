using Microsoft.EntityFrameworkCore;
using Prototype.DTOs.Cache;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class AnalyticsService(
    SentinelContext context,
    ILogger<AnalyticsService> logger) : IAnalyticsService
{
    public async Task<AnalyticsMetricsCacheDto> GetAnalyticsOverviewAsync()
    {
        try
        {
            logger.LogInformation("Starting analytics overview collection");

            var now = DateTime.UtcNow;
            var last30Days = now.AddDays(-30);

            // Collect all metrics in parallel for better performance
            var userMetricsTask = GetUserGrowthMetricsAsync();
            var appMetricsTask = GetApplicationMetricsAsync();
            var securityMetricsTask = GetSecurityMetricsAsync();
            var systemHealthTask = GetSystemHealthMetricsAsync();

            await Task.WhenAll(userMetricsTask, appMetricsTask, securityMetricsTask, systemHealthTask);

            var userMetrics = await userMetricsTask;
            var appMetrics = await appMetricsTask;
            var securityMetrics = await securityMetricsTask;
            var systemHealth = await systemHealthTask;

            logger.LogInformation("Successfully collected all analytics metrics");

            return new AnalyticsMetricsCacheDto
            {
                Summary = new
                {
                    totalUsers = ((dynamic)userMetrics).total,
                    totalApplications = ((dynamic)appMetrics).total,
                    securityScore = ((dynamic)securityMetrics).securityScore,
                    systemHealth = ((dynamic)systemHealth).healthScore,
                    timeframe = "Last 30 Days"
                },
                UserMetrics = userMetrics,
                ApplicationMetrics = appMetrics,
                SecurityMetrics = securityMetrics,
                SystemHealth = systemHealth,
                CollectedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error collecting analytics overview");
            throw;
        }
    }

    public async Task<object> GetUserGrowthMetricsAsync(int days = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        var totalUsers = await context.Users.CountAsync();
        var verifiedUsers = await context.Users.CountAsync();
        var unverifiedUsers = await context.TemporaryUsers.CountAsync();
        var newUsersLastPeriod = await context.Users
            .Where(u => u.CreatedAt >= cutoffDate)
            .CountAsync();

        var growthRate = await CalculateUserGrowthRate(days);
        var adoptionRate = totalUsers > 0 ? (double)verifiedUsers / totalUsers * 100 : 0;

        return new
        {
            total = totalUsers,
            verified = verifiedUsers,
            unverified = unverifiedUsers,
            newUsersLastPeriod = newUsersLastPeriod,
            growthRate = Math.Round(growthRate, 2),
            adoptionRate = Math.Round(adoptionRate, 1),
            periodDays = days
        };
    }

    public async Task<object> GetApplicationMetricsAsync()
    {
        var totalApplications = await context.Applications.CountAsync();
        var activeApplications = await context.UserApplications
            .Select(ua => ua.ApplicationId)
            .Distinct()
            .CountAsync();
        
        var utilizationRate = totalApplications > 0 ? (double)activeApplications / totalApplications * 100 : 0;
        var totalConnections = await context.ApplicationConnections.CountAsync();
        var averageConnectionsPerApp = totalApplications > 0 ? 
            Math.Round((double)totalConnections / totalApplications, 1) : 0;

        return new
        {
            total = totalApplications,
            active = activeApplications,
            utilizationRate = Math.Round(utilizationRate, 1),
            totalConnections = totalConnections,
            averageConnectionsPerApp = averageConnectionsPerApp,
            applicationDistribution = await GetApplicationDistributionAsync()
        };
    }

    public async Task<object> GetSecurityMetricsAsync(int days = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        var securityEvents = await context.UserActivityLogs
            .Where(log => log.Timestamp >= cutoffDate && 
                         (log.ActionType == ActionTypeEnum.FailedLogin ||
                          log.ActionType == ActionTypeEnum.ApplicationRemoved ||
                          log.ActionType == ActionTypeEnum.RoleDeleted))
            .CountAsync();

        var successfulLogins = await context.UserActivityLogs
            .Where(log => log.Timestamp >= cutoffDate && log.ActionType == ActionTypeEnum.Login)
            .CountAsync();

        var failedLogins = await context.UserActivityLogs
            .Where(log => log.Timestamp >= cutoffDate && log.ActionType == ActionTypeEnum.FailedLogin)
            .CountAsync();

        var securityScore = await CalculateSecurityScore(failedLogins, successfulLogins, securityEvents);

        return new
        {
            securityScore = Math.Round(securityScore, 1),
            failedLogins = failedLogins,
            successfulLogins = successfulLogins,
            securityEvents = securityEvents,
            loginSuccessRate = successfulLogins + failedLogins > 0 ? 
                Math.Round((double)successfulLogins / (successfulLogins + failedLogins) * 100, 1) : 100,
            periodDays = days
        };
    }

    public async Task<object> GetSystemHealthMetricsAsync()
    {
        var healthScore = await CalculateSystemHealthScore();
        var averageUserSessions = await CalculateAverageUserSessions();
        var totalRoles = await context.UserRoles.CountAsync();

        var totalUsers = await context.Users.CountAsync();
        var totalApplications = await context.Applications.CountAsync();
        var costSavings = await CalculateCostSavings(totalUsers, totalApplications);
        var productivityGain = await CalculateProductivityGain(totalUsers, averageUserSessions);

        return new
        {
            healthScore = Math.Round(healthScore, 1),
            averageUserSessions = Math.Round(averageUserSessions, 1),
            totalRoles = totalRoles,
            estimatedCostSavings = costSavings,
            productivityGain = Math.Round(productivityGain, 1),
            systemUptime = "99.9%", // This would come from actual monitoring
            responseTime = "< 200ms" // This would come from actual monitoring
        };
    }

    public async Task<double> CalculateSecurityScore(int failedLogins, int successfulLogins, int securityEvents)
    {
        return await Task.FromResult(() =>
        {
            // Security score calculation (0-100)
            double baseScore = 100.0;

            // Deduct points for failed logins (max 30 points)
            var failedLoginPenalty = Math.Min(failedLogins * 0.5, 30);
            baseScore -= failedLoginPenalty;

            // Deduct points for security events (max 25 points)
            var securityEventPenalty = Math.Min(securityEvents * 2.0, 25);
            baseScore -= securityEventPenalty;

            // Bonus for successful logins (up to 10 points back)
            var successBonus = Math.Min(successfulLogins * 0.01, 10);
            baseScore += successBonus;

            return Math.Max(0, Math.Min(100, baseScore));
        });
    }

    public async Task<double> CalculateUserGrowthRate(int days = 30)
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-days);
        var previousStartDate = startDate.AddDays(-days);

        var currentPeriodUsers = await context.Users
            .Where(u => u.CreatedAt >= startDate && u.CreatedAt < endDate)
            .CountAsync();

        var previousPeriodUsers = await context.Users
            .Where(u => u.CreatedAt >= previousStartDate && u.CreatedAt < startDate)
            .CountAsync();

        if (previousPeriodUsers == 0)
            return currentPeriodUsers > 0 ? 100.0 : 0.0;

        return ((double)(currentPeriodUsers - previousPeriodUsers) / previousPeriodUsers) * 100;
    }

    public async Task<double> CalculateSystemHealthScore()
    {
        return await Task.FromResult(() =>
        {
            // Simplified system health calculation
            // In a real implementation, this would check actual system metrics
            double healthScore = 100.0;

            // Simulated metrics - replace with actual monitoring data
            var errorRate = 0.01; // 1% error rate
            var responseTime = 150; // milliseconds
            var uptime = 99.9; // percentage

            // Deduct points based on metrics
            healthScore -= errorRate * 1000; // Error rate penalty
            healthScore -= Math.Max(0, (responseTime - 100) * 0.1); // Response time penalty
            healthScore -= Math.Max(0, (100 - uptime) * 10); // Uptime penalty

            return Math.Max(0, Math.Min(100, healthScore));
        });
    }

    public async Task<double> CalculateAverageUserSessions()
    {
        var last30Days = DateTime.UtcNow.AddDays(-30);
        
        var userSessions = await context.UserActivityLogs
            .Where(log => log.Timestamp >= last30Days && log.ActionType == ActionTypeEnum.Login)
            .GroupBy(log => log.UserId)
            .Select(g => new { UserId = g.Key, SessionCount = g.Count() })
            .ToListAsync();

        if (!userSessions.Any())
            return 0.0;

        return userSessions.Average(us => us.SessionCount);
    }

    public async Task<decimal> CalculateCostSavings(int totalUsers, int totalApplications)
    {
        return await Task.FromResult(() =>
        {
            // Simplified cost savings calculation
            // Assumes cost savings from centralized management
            var savingsPerUser = 25.0m; // $25 per user per month
            var savingsPerApp = 100.0m; // $100 per application per month

            return (totalUsers * savingsPerUser) + (totalApplications * savingsPerApp);
        });
    }

    public async Task<double> CalculateProductivityGain(int totalUsers, double averageUserSessions)
    {
        return await Task.FromResult(() =>
        {
            // Simplified productivity gain calculation
            // Assumes productivity gain from better access management
            var baseProductivityGain = 5.0; // 5% base gain
            var sessionBonus = Math.Min(averageUserSessions * 0.5, 10.0); // Up to 10% bonus

            return baseProductivityGain + sessionBonus;
        });
    }

    private async Task<object> GetApplicationDistributionAsync()
    {
        var appTypes = await context.Applications
            .GroupBy(a => a.ApplicationDataSourceType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync();

        return appTypes.ToDictionary(at => at.Type, at => at.Count);
    }
}