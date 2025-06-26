using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Prototype.DTOs.Cache;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class CacheService(
    IMemoryCache memoryCache,
    IDistributedCache distributedCache,
    IConnectionMultiplexer redis,
    IDataProtectionProvider dpProvider,
    ICacheMetricsService metrics,
    ILogger<CacheService> logger)
    : ICacheService
{
    private readonly IDataProtector _protector = dpProvider.CreateProtector("SecureCache.v1");

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            // L1: Check memory cache first (fastest)
            if (memoryCache.TryGetValue(key, out T? memoryValue))
            {
                stopwatch.Stop();
                metrics.RecordCacheHit($"L1:{key}");
                metrics.RecordCacheLatency("L1_Get", stopwatch.ElapsedMilliseconds);
                return memoryValue;
            }

            // L2: Check Redis distributed cache
            var redisValue = await distributedCache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(redisValue))
            {
                var deserializedValue = JsonSerializer.Deserialize<T>(redisValue, _jsonOptions);
                
                // Store in L1 cache for faster subsequent access
                memoryCache.Set(key, deserializedValue, TimeSpan.FromMinutes(5));
                
                stopwatch.Stop();
                metrics.RecordCacheHit($"L2:{key}");
                metrics.RecordCacheLatency("L2_Get", stopwatch.ElapsedMilliseconds);
                return deserializedValue;
            }

            stopwatch.Stop();
            metrics.RecordCacheMiss(key);
            metrics.RecordCacheLatency("Miss", stopwatch.ElapsedMilliseconds);
            return null;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Error retrieving cache key: {Key}", key);
            metrics.RecordCacheMiss(key);
            return null;
        }
    }

    public async Task<T?> GetSecureAsync<T>(string key, Guid userId) where T : class
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var secureKey = GenerateSecureKey(key, userId);
        
        try
        {
            // Log access for security monitoring
            await metrics.LogCacheAccessAsync("GetSecure", key, userId);
            
            // Check memory cache first
            if (memoryCache.TryGetValue(secureKey, out CacheWrapperDto<T>? memoryWrapper))
            {
                if (IsValidWrapper(memoryWrapper, userId))
                {
                    stopwatch.Stop();
                    metrics.RecordCacheHit($"L1_Secure:{key}");
                    metrics.RecordCacheLatency("L1_SecureGet", stopwatch.ElapsedMilliseconds);
                    return memoryWrapper.Data;
                }
            }

            // Check Redis with encryption
            var encryptedData = await distributedCache.GetStringAsync(secureKey);
            if (!string.IsNullOrEmpty(encryptedData))
            {
                try
                {
                    var decryptedJson = _protector.Unprotect(encryptedData);
                    var wrapper = JsonSerializer.Deserialize<CacheWrapperDto<T>>(decryptedJson, _jsonOptions);
                    
                    if (IsValidWrapper(wrapper, userId))
                    {
                        // Store in memory cache
                        memoryCache.Set(secureKey, wrapper, TimeSpan.FromMinutes(5));
                        
                        stopwatch.Stop();
                        metrics.RecordCacheHit($"L2_Secure:{key}");
                        metrics.RecordCacheLatency("L2_SecureGet", stopwatch.ElapsedMilliseconds);
                        return wrapper.Data;
                    }
                    else
                    {
                        logger.LogWarning("Cache access violation: User {UserId} tried to access secured data", userId);
                        await RemoveAsync(secureKey);
                    }
                }
                catch (CryptographicException ex)
                {
                    logger.LogError(ex, "Failed to decrypt cache data for key {Key}", key);
                    await RemoveAsync(secureKey);
                }
            }

            stopwatch.Stop();
            metrics.RecordCacheMiss(key);
            metrics.RecordCacheLatency("SecureMiss", stopwatch.ElapsedMilliseconds);
            return null;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Error retrieving secure cache key: {Key}", key);
            metrics.RecordCacheMiss(key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            var options = new DistributedCacheEntryOptions();
            
            var expiryTime = expiry ?? TimeSpan.FromMinutes(30);
            options.SetAbsoluteExpiration(expiryTime);

            // Store in both caches
            await distributedCache.SetStringAsync(key, serializedValue, options);
            memoryCache.Set(key, value, TimeSpan.FromMinutes(Math.Min(5, expiryTime.TotalMinutes)));
            
            stopwatch.Stop();
            metrics.RecordCacheLatency("Set", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Error setting cache key: {Key}", key);
        }
    }

    public async Task SetSecureAsync<T>(string key, T value, Guid userId, TimeSpan? expiry = null) where T : class
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var secureKey = GenerateSecureKey(key, userId);
        
        try
        {
            // Log access for security monitoring
            await metrics.LogCacheAccessAsync("SetSecure", key, userId);
            
            var wrapper = new CacheWrapperDto<T>
            {
                UserId = userId,
                Data = value,
                CachedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromMinutes(30))
            };

            var serializedWrapper = JsonSerializer.Serialize(wrapper, _jsonOptions);
            var encryptedData = _protector.Protect(serializedWrapper);

            var options = new DistributedCacheEntryOptions();
            var expiryTime = expiry ?? TimeSpan.FromMinutes(30);
            options.SetAbsoluteExpiration(expiryTime);

            await distributedCache.SetStringAsync(secureKey, encryptedData, options);
            memoryCache.Set(secureKey, wrapper, TimeSpan.FromMinutes(Math.Min(5, expiryTime.TotalMinutes)));
            
            stopwatch.Stop();
            metrics.RecordCacheLatency("SecureSet", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Error setting secure cache key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            memoryCache.Remove(key);
            await distributedCache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing cache key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            var server = redis.GetServer(redis.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern).ToArray();
            
            if (keys.Length > 0)
            {
                var database = redis.GetDatabase();
                await database.KeyDeleteAsync(keys);
                
                // Remove from memory cache (limited pattern support)
                foreach (var key in keys)
                {
                    memoryCache.Remove(key.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing cache keys by pattern: {Pattern}", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            if (memoryCache.TryGetValue(key, out _))
                return true;
                
            var redisValue = await distributedCache.GetStringAsync(key);
            return !string.IsNullOrEmpty(redisValue);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking cache key existence: {Key}", key);
            return false;
        }
    }

    public async Task InvalidateUserCacheAsync(Guid userId)
    {
        try
        {
            var patterns = new[]
            {
                $"*:user:{userId}*",
                $"*:dashboard:{userId}*",
                $"*:apps:{userId}*",
                $"*:settings:{userId}*"
            };

            foreach (var pattern in patterns)
            {
                await RemoveByPatternAsync(pattern);
            }
            
            logger.LogInformation("Invalidated cache for user: {UserId}", userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error invalidating user cache: {UserId}", userId);
        }
    }

    public string GenerateSecureKey(string key, Guid userId)
    {
        var combinedKey = $"{key}:{userId}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedKey));
        return $"secure:{Convert.ToBase64String(hash)[..16]}"; // Truncate for readability
    }

    private bool IsValidWrapper<T>(CacheWrapperDto<T>? wrapper, Guid userId)
    {
        return wrapper != null && 
               wrapper.UserId == userId && 
               wrapper.ExpiresAt > DateTime.UtcNow;
    }
}