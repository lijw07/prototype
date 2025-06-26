using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation;

[Authorize]
[Route("api/system-health")]
[ApiController]
public class SystemHealthNavigationController(
    SentinelContext context,
    IAuthenticatedUserAccessor userAccessor,
    ICacheService cacheService,
    ILogger<SystemHealthNavigationController> logger)
    : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> GetHealthOverview()
    {
        try
        {
            var currentUser = await userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            // Check cache first
            var cacheKey = "system-health:overview";
            var cachedData = await cacheService.GetAsync<object>(cacheKey);
            
            if (cachedData != null)
            {
                logger.LogDebug("System health overview cache hit");
                return Ok(new { success = true, data = cachedData });
            }

            var healthData = await CollectHealthMetrics();
            
            // Cache for 2 minutes (system health changes frequently)
            await cacheService.SetAsync(cacheKey, healthData, TimeSpan.FromMinutes(2));
            logger.LogDebug("System health overview cached");
            
            return Ok(new { success = true, data = healthData });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving system health overview");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("database-connections")]
    public async Task<IActionResult> GetDatabaseConnectionsHealth()
    {
        try
        {
            var currentUser = await userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            // Check cache first
            var cacheKey = "system-health:database-connections";
            var cachedConnections = await cacheService.GetAsync<object>(cacheKey);
            
            if (cachedConnections != null)
            {
                logger.LogDebug("Database connections health cache hit");
                return Ok(new { success = true, data = cachedConnections });
            }

            var connections = await TestDatabaseConnections();
            
            // Cache for 5 minutes (connection tests are expensive)
            await cacheService.SetAsync(cacheKey, connections, TimeSpan.FromMinutes(5));
            logger.LogDebug("Database connections health cached");
            
            return Ok(new { success = true, data = connections });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error testing database connections");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("performance-metrics")]
    public async Task<IActionResult> GetPerformanceMetrics()
    {
        try
        {
            var currentUser = await userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            // Check cache first
            var cacheKey = "system-health:performance-metrics";
            var cachedMetrics = await cacheService.GetAsync<object>(cacheKey);
            
            if (cachedMetrics != null)
            {
                logger.LogDebug("Performance metrics cache hit");
                return Ok(new { success = true, data = cachedMetrics });
            }

            var metrics = await CollectPerformanceMetrics();
            
            // Cache for 3 minutes (performance metrics change moderately)
            await cacheService.SetAsync(cacheKey, metrics, TimeSpan.FromMinutes(3));
            logger.LogDebug("Performance metrics cached");
            
            return Ok(new { success = true, data = metrics });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving performance metrics");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    private async Task<object> CollectHealthMetrics()
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Test main database connection
        var dbHealthy = await TestMainDatabaseHealth();
        
        // Get application connection counts (simplified to avoid dynamic object issues)
        var totalConnections = await context.ApplicationConnections.CountAsync();
        var healthyConnections = Math.Max(0, totalConnections - Random.Shared.Next(0, 2)); // Simulate mostly healthy connections
        
        // Calculate error rates from recent logs
        var errorRate = await CalculateErrorRate();
        
        // Get system metrics (simulated for demo)
        var systemMetrics = GetSystemMetrics();
        
        // Calculate overall health score
        var healthScore = CalculateHealthScore(dbHealthy, healthyConnections, totalConnections, errorRate, systemMetrics);
        
        stopwatch.Stop();
        
        return new
        {
            overall = new
            {
                status = GetHealthStatus(healthScore),
                healthScore = healthScore,
                lastChecked = DateTime.UtcNow,
                responseTime = stopwatch.ElapsedMilliseconds
            },
            database = new
            {
                mainDatabase = dbHealthy ? "Healthy" : "Unhealthy",
                applicationConnections = new
                {
                    healthy = healthyConnections,
                    total = totalConnections,
                    percentage = totalConnections > 0 ? (healthyConnections * 100 / totalConnections) : 100
                }
            },
            performance = systemMetrics,
            alerts = await GenerateHealthAlerts(dbHealthy, healthyConnections, totalConnections, errorRate)
        };
    }

    private async Task<bool> TestMainDatabaseHealth()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            await context.Database.ExecuteSqlRawAsync("SELECT 1");
            stopwatch.Stop();
            
            // Consider healthy if response time is under 1 second
            return stopwatch.ElapsedMilliseconds < 1000;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Main database health check failed");
            return false;
        }
    }

    private async Task<List<object>> TestDatabaseConnections()
    {
        var connections = await context.ApplicationConnections
            .Include(ac => ac.Application)
            .Select(ac => new
            {
                ac.ApplicationConnectionId,
                ac.Application!.ApplicationName,
                ac.Host,
                ac.Port,
                ac.AuthenticationType,
                ac.CreatedAt
            })
            .ToListAsync();

        var results = new List<object>();

        foreach (var connection in connections)
        {
            var stopwatch = Stopwatch.StartNew();
            var status = "Healthy";
            var responseTime = 0L;
            var errorMessage = "";

            try
            {
                // Basic connection validation
                if (string.IsNullOrEmpty(connection.Host) || string.IsNullOrEmpty(connection.Port))
                {
                    status = "Unhealthy";
                    errorMessage = "Invalid host or port configuration";
                }
                else
                {
                    // Simulate connection test (replace with actual connection test)
                    await Task.Delay(Random.Shared.Next(50, 200)); // Simulate network latency
                    
                    // Randomly simulate some failed connections for demo
                    if (Random.Shared.Next(1, 10) == 1) // 10% failure rate
                    {
                        status = "Unhealthy";
                        errorMessage = "Connection timeout";
                    }
                }
            }
            catch (Exception ex)
            {
                status = "Unhealthy";
                errorMessage = ex.Message;
                logger.LogError(ex, "Connection test failed for application {ApplicationName}", connection.ApplicationName);
            }
            
            stopwatch.Stop();
            responseTime = stopwatch.ElapsedMilliseconds;

            results.Add(new
            {
                connectionId = connection.ApplicationConnectionId,
                applicationName = connection.ApplicationName,
                connectionType = connection.AuthenticationType.ToString(),
                host = connection.Host,
                port = connection.Port,
                status = status,
                responseTime = responseTime,
                errorMessage = errorMessage,
                lastTested = DateTime.UtcNow
            });
        }

        return results;
    }

    private async Task<object> CollectPerformanceMetrics()
    {
        var now = DateTime.UtcNow;
        var last24Hours = now.AddHours(-24);
        var last7Days = now.AddDays(-7);

        // Get recent log activity as performance indicator
        var recentLogActivity = await context.UserActivityLogs
            .Where(log => log.Timestamp >= last24Hours)
            .CountAsync();

        var recentApplicationLogs = await context.ApplicationLogs
            .Where(log => log.CreatedAt >= last24Hours)
            .CountAsync();

        // Calculate average response times (simulated)
        var avgResponseTime = Random.Shared.Next(150, 500);
        var apiUptime = 99.5 + (Random.Shared.NextDouble() * 0.5); // 99.5-100%

        return new
        {
            responseTime = new
            {
                average = avgResponseTime,
                p95 = avgResponseTime + Random.Shared.Next(50, 200),
                trend = Random.Shared.Next(-10, 20) // % change
            },
            throughput = new
            {
                requestsPerMinute = recentLogActivity + recentApplicationLogs,
                peak24h = Math.Max(recentLogActivity, recentApplicationLogs) * 2,
                trend = Random.Shared.Next(-5, 15)
            },
            uptime = new
            {
                percentage = Math.Round(apiUptime, 2),
                since = DateTime.UtcNow.AddDays(-Random.Shared.Next(30, 90)).ToString("yyyy-MM-dd"),
                incidents24h = Random.Shared.Next(0, 2)
            },
            resources = GetSystemMetrics()
        };
    }

    private object GetSystemMetrics()
    {
        // Simulate system metrics (in production, use actual system monitoring)
        var cpuUsage = Random.Shared.Next(10, 30);
        var memoryUsage = Random.Shared.Next(40, 75);
        var diskUsage = Random.Shared.Next(25, 60);

        return new
        {
            cpu = new
            {
                usage = cpuUsage,
                status = cpuUsage > 80 ? "High" : cpuUsage > 60 ? "Medium" : "Normal"
            },
            memory = new
            {
                usage = memoryUsage,
                status = memoryUsage > 85 ? "High" : memoryUsage > 70 ? "Medium" : "Normal",
                available = $"{Random.Shared.Next(2, 8)}GB"
            },
            disk = new
            {
                usage = diskUsage,
                status = diskUsage > 90 ? "High" : diskUsage > 75 ? "Medium" : "Normal",
                available = $"{Random.Shared.Next(100, 500)}GB"
            },
            network = new
            {
                status = "Stable",
                latency = Random.Shared.Next(15, 45)
            }
        };
    }

    private async Task<double> CalculateErrorRate()
    {
        var now = DateTime.UtcNow;
        var last24Hours = now.AddHours(-24);

        try
        {
            var totalLogs = await context.UserActivityLogs
                .Where(log => log.Timestamp >= last24Hours)
                .CountAsync();

            var errorLogs = await context.UserActivityLogs
                .Where(log => log.Timestamp >= last24Hours && 
                             log.ActionType == ActionTypeEnum.FailedLogin)
                .CountAsync();

            return totalLogs > 0 ? (double)errorLogs / totalLogs * 100 : 0;
        }
        catch
        {
            return 0;
        }
    }

    private int CalculateHealthScore(bool dbHealthy, int healthyConnections, int totalConnections, double errorRate, object systemMetrics)
    {
        int score = 100;

        // Database health impact
        if (!dbHealthy) score -= 30;

        // Connection health impact
        if (totalConnections > 0)
        {
            var connectionHealth = (double)healthyConnections / totalConnections;
            score -= (int)((1 - connectionHealth) * 25);
        }

        // Error rate impact
        score -= (int)(errorRate * 2); // Each 1% error rate reduces score by 2

        return Math.Max(0, Math.Min(100, score));
    }

    private string GetHealthStatus(int healthScore)
    {
        return healthScore switch
        {
            >= 90 => "Excellent",
            >= 75 => "Good",
            >= 60 => "Fair",
            >= 40 => "Poor",
            _ => "Critical"
        };
    }

    private async Task<List<object>> GenerateHealthAlerts(bool dbHealthy, int healthyConnections, int totalConnections, double errorRate)
    {
        var alerts = new List<object>();

        if (!dbHealthy)
        {
            alerts.Add(new
            {
                level = "Critical",
                message = "Main database connection is unhealthy",
                timestamp = DateTime.UtcNow,
                category = "Database"
            });
        }

        if (totalConnections > 0 && healthyConnections < totalConnections)
        {
            var unhealthyCount = totalConnections - healthyConnections;
            alerts.Add(new
            {
                level = unhealthyCount > totalConnections / 2 ? "Critical" : "Warning",
                message = $"{unhealthyCount} application connection(s) are unhealthy",
                timestamp = DateTime.UtcNow,
                category = "Connections"
            });
        }

        if (errorRate > 10)
        {
            alerts.Add(new
            {
                level = errorRate > 25 ? "Critical" : "Warning",
                message = $"High error rate detected: {errorRate:F1}%",
                timestamp = DateTime.UtcNow,
                category = "Performance"
            });
        }

        return alerts;
    }
}