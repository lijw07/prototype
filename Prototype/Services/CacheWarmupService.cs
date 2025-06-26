using Microsoft.EntityFrameworkCore;
using Prototype.DTOs.Cache;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class CacheWarmupService(
    IServiceScopeFactory serviceScopeFactory,
    ICacheService cacheService,
    ILogger<CacheWarmupService> logger)
    : ICacheWarmupService
{
    public async Task WarmupCriticalCacheAsync()
    {
        logger.LogInformation("Starting critical cache warmup");
        
        var tasks = new[]
        {
            WarmupDashboardCacheAsync(),
            WarmupAnalyticsCacheAsync(),
            WarmupActiveUsersCacheAsync()
        };

        await Task.WhenAll(tasks);
        
        logger.LogInformation("Critical cache warmup completed");
    }

    public async Task WarmupDashboardCacheAsync()
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SentinelContext>();

            // Pre-calculate global dashboard statistics
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

            var stats = new DashboardStatsCacheDto
            {
                TotalUsers = tasks[0].Result,
                TotalTemporaryUsers = tasks[1].Result,
                TotalRoles = tasks[2].Result,
                RecentActivityCount = tasks[3].Result,
                TotalApplications = tasks[4].Result,
                GeneratedAt = DateTime.UtcNow
            };

            await cacheService.SetAsync("dashboard:statistics:global", stats, TimeSpan.FromMinutes(15));
            logger.LogInformation("Dashboard cache warmed up");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error warming up dashboard cache");
        }
    }

    public async Task WarmupAnalyticsCacheAsync()
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SentinelContext>();

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

            var tasks = new[]
            {
                context.Users.CountAsync(),
                context.Applications.CountAsync(),
                context.UserApplications.CountAsync(),
                context.UserActivityLogs.Where(log => log.ActivityDate >= thirtyDaysAgo).CountAsync(),
                context.UserActivityLogs.Where(log => log.ActivityDate >= sevenDaysAgo).CountAsync()
            };

            await Task.WhenAll(tasks);

            var analytics = new AnalyticsMetricsCacheDto
            {
                TotalUsers = tasks[0].Result,
                TotalApplications = tasks[1].Result,
                TotalUserApplications = tasks[2].Result,
                ActivityLast30Days = tasks[3].Result,
                ActivityLast7Days = tasks[4].Result,
                GeneratedAt = DateTime.UtcNow
            };

            await cacheService.SetAsync("analytics:overview:metrics", analytics, TimeSpan.FromMinutes(30));
            logger.LogInformation("Analytics cache warmed up");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error warming up analytics cache");
        }
    }

    public async Task WarmupUserCacheAsync(Guid userId)
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SentinelContext>();

            var user = await context.Users
                .Where(u => u.UserId == userId)
                .Select(u => new UserCacheDto
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    LastLoginDate = u.LastLoginDate,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (user != null)
            {
                await cacheService.SetSecureAsync($"user:profile:{userId}", user, userId, TimeSpan.FromMinutes(30));
                logger.LogDebug("User cache warmed up for UserId: {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error warming up user cache for UserId: {UserId}", userId);
        }
    }

    private async Task WarmupActiveUsersCacheAsync()
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SentinelContext>();

            // Warm up cache for users who logged in today
            var activeUsers = await context.Users
                .Where(u => u.LastLoginDate >= DateTime.UtcNow.AddDays(-1))
                .Take(100) // Limit to prevent memory spike
                .Select(u => new UserCacheDto
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    LastLoginDate = u.LastLoginDate,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt
                })
                .ToListAsync();

            var warmupTasks = activeUsers.Select(async user =>
            {
                await cacheService.SetSecureAsync($"user:profile:{user.UserId}", user, user.UserId, TimeSpan.FromMinutes(30));
            });

            await Task.WhenAll(warmupTasks);
            
            logger.LogInformation("Warmed up cache for {Count} active users", activeUsers.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error warming up active users cache");
        }
    }
}