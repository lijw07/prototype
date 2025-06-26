using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class CacheInvalidationService(
    ICacheService cacheService,
    ILogger<CacheInvalidationService> logger)
    : ICacheInvalidationService
{
    public async Task InvalidateUserCacheAsync(Guid userId, string? email = null, string? username = null)
    {
        var keys = new List<string>
        {
            $"user:{userId}",
            $"user:auth:{userId}",
            $"user:profile:{userId}",
            $"user:permissions:{userId}"
        };

        if (!string.IsNullOrEmpty(email))
            keys.Add($"user:email:{email.ToLowerInvariant()}");
            
        if (!string.IsNullOrEmpty(username))
            keys.Add($"user:username:{username.ToLowerInvariant()}");

        foreach (var key in keys)
        {
            await cacheService.RemoveAsync(key);
        }

        logger.LogInformation("Invalidated user cache for UserId: {UserId}", userId);
    }

    public async Task InvalidateApplicationCacheAsync(Guid userId)
    {
        var keys = new[]
        {
            $"user:{userId}:applications",
            $"user:{userId}:app:list",
            $"user:{userId}:app:settings"
        };

        foreach (var key in keys)
        {
            await cacheService.RemoveAsync(key);
        }
        
        logger.LogInformation("Invalidated application cache for UserId: {UserId}", userId);
    }

    public async Task InvalidateDashboardCacheAsync()
    {
        var keys = new[]
        {
            "dashboard:statistics:global",
            "dashboard:metrics:global"
        };

        foreach (var key in keys)
        {
            await cacheService.RemoveAsync(key);
        }
        
        // Remove user-specific dashboard caches
        await cacheService.RemoveByPatternAsync("dashboard:user:*");
        
        logger.LogInformation("Invalidated dashboard cache");
    }

    public async Task InvalidateAnalyticsCacheAsync()
    {
        var keys = new[]
        {
            "analytics:overview:metrics",
            "analytics:security:overview",
            "analytics:user:metrics",
            "analytics:application:metrics"
        };

        foreach (var key in keys)
        {
            await cacheService.RemoveAsync(key);
        }
        
        logger.LogInformation("Invalidated analytics cache");
    }

    public async Task InvalidateComplianceCacheAsync()
    {
        var keys = new[]
        {
            "compliance:metrics:overview",
            "compliance:audit:summary",
            "compliance:score:global"
        };

        foreach (var key in keys)
        {
            await cacheService.RemoveAsync(key);
        }
        
        logger.LogInformation("Invalidated compliance cache");
    }

    public async Task InvalidateSystemHealthCacheAsync()
    {
        var keys = new[]
        {
            "system:health:status",
            "system:performance:metrics"
        };

        foreach (var key in keys)
        {
            await cacheService.RemoveAsync(key);
        }
        
        logger.LogInformation("Invalidated system health cache");
    }

    public async Task InvalidateAllUserSpecificCacheAsync(Guid userId)
    {
        await InvalidateUserCacheAsync(userId);
        await InvalidateApplicationCacheAsync(userId);
        
        // Remove user-specific analytics and dashboard data
        await cacheService.RemoveByPatternAsync($"*:{userId}:*");
        await cacheService.RemoveByPatternAsync($"*:user:{userId}*");
        
        logger.LogInformation("Invalidated all user-specific cache for UserId: {UserId}", userId);
    }
}