using System.Text.Json;

namespace Prototype.Services.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task<T?> GetSecureAsync<T>(string key, Guid userId) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    Task SetSecureAsync<T>(string key, T value, Guid userId, TimeSpan? expiry = null) where T : class;
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    Task<bool> ExistsAsync(string key);
    Task InvalidateUserCacheAsync(Guid userId);
    string GenerateSecureKey(string key, Guid userId);
}

public interface ICacheInvalidationService
{
    Task InvalidateUserCacheAsync(Guid userId, string? email = null, string? username = null);
    Task InvalidateApplicationCacheAsync(Guid userId);
    Task InvalidateDashboardCacheAsync();
    Task InvalidateAnalyticsCacheAsync();
    Task InvalidateComplianceCacheAsync();
    Task InvalidateSystemHealthCacheAsync();
    Task InvalidateAllUserSpecificCacheAsync(Guid userId);
}

public interface ICacheWarmupService
{
    Task WarmupCriticalCacheAsync();
    Task WarmupDashboardCacheAsync();
    Task WarmupUserCacheAsync(Guid userId);
    Task WarmupAnalyticsCacheAsync();
}

public interface ICacheMetricsService
{
    void RecordCacheHit(string key);
    void RecordCacheMiss(string key);
    void RecordCacheLatency(string operation, double milliseconds);
    double GetHitRatio();
    int GetTotalQueries();
    Task LogCacheAccessAsync(string operation, string key, Guid? userId = null);
}