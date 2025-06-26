using System.Collections.Concurrent;
using Prototype.DTOs.Cache;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class CacheMetricsService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<CacheMetricsService> logger)
    : ICacheMetricsService
{
    private readonly ConcurrentDictionary<string, int> _cacheHits = new();
    private readonly ConcurrentDictionary<string, int> _cacheMisses = new();
    private readonly ConcurrentQueue<double> _latencyMetrics = new();
    private readonly object _lockObject = new();
    private int _totalQueries = 0;

    public void RecordCacheHit(string key)
    {
        _cacheHits.AddOrUpdate(key, 1, (k, v) => v + 1);
        Interlocked.Increment(ref _totalQueries);
        
        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Cache HIT: {Key}", key);
    }

    public void RecordCacheMiss(string key)
    {
        _cacheMisses.AddOrUpdate(key, 1, (k, v) => v + 1);
        Interlocked.Increment(ref _totalQueries);
        
        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Cache MISS: {Key}", key);
    }

    public void RecordCacheLatency(string operation, double milliseconds)
    {
        _latencyMetrics.Enqueue(milliseconds);
        
        // Keep only last 1000 measurements
        if (_latencyMetrics.Count > 1000)
        {
            _latencyMetrics.TryDequeue(out _);
        }
        
        if (milliseconds > 100) // Log slow cache operations
        {
            logger.LogWarning("Slow cache operation: {Operation} took {Milliseconds}ms", operation, milliseconds);
        }
    }

    public double GetHitRatio()
    {
        var totalHits = _cacheHits.Values.Sum();
        var totalMisses = _cacheMisses.Values.Sum();
        var total = totalHits + totalMisses;
        
        return total == 0 ? 0.0 : (double)totalHits / total;
    }

    public int GetTotalQueries()
    {
        return _totalQueries;
    }

    public async Task LogCacheAccessAsync(string operation, string key, Guid? userId = null)
    {
        try
        {
            // Only log security-sensitive operations to database
            if (operation.Contains("Secure") && userId.HasValue)
            {
                using var scope = serviceScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<SentinelContext>();
                
                var activityLog = new UserActivityLogModel
                {
                    UserActivityLogId = Guid.NewGuid(),
                    UserId = userId.Value,
                    ActionType = Prototype.Enum.ActionTypeEnum.Read,
                    Description = $"Cache access: {operation}",
                    Timestamp = DateTime.UtcNow,
                    DeviceInformation = "Cache Service",
                    IpAddress = "Internal"
                };

                context.UserActivityLogs.Add(activityLog);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error logging cache access for operation: {Operation}", operation);
        }
    }
}