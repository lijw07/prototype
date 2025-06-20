using System.Collections.Concurrent;
using Prototype.Common.Responses;

namespace Prototype.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, List<DateTime>> _attempts = new();
    private readonly int _maxAttempts;
    private readonly int _windowMinutes;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, IConfiguration config)
    {
        _next = next;
        _logger = logger;
        _maxAttempts = config.GetValue<int>("RateLimit:MaxAttempts", 5);
        _windowMinutes = config.GetValue<int>("RateLimit:WindowMinutes", 15);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Apply rate limiting only to sensitive endpoints
        if (ShouldApplyRateLimit(context.Request.Path))
        {
            var clientId = GetClientIdentifier(context);
            
            if (IsRateLimited(clientId))
            {
                _logger.LogWarning("Rate limit exceeded for client: {ClientId}, Path: {Path}", 
                    clientId, context.Request.Path);
                
                context.Response.StatusCode = 429;
                context.Response.ContentType = "application/json";
                
                var response = ApiResponse.FailureResponse("Too many requests. Please try again later.");
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
                return;
            }
            
            // Record this attempt
            RecordAttempt(clientId);
        }
        
        await _next(context);
    }

    private static bool ShouldApplyRateLimit(PathString path)
    {
        var sensitiveEndpoints = new[]
        {
            "/login",
            //"/register",
            "/forgotuser",
            "/passwordreset"
        };
        
        return sensitiveEndpoints.Any(endpoint => 
            path.StartsWithSegments(endpoint, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetClientIdentifier(HttpContext context)
    {
        // Try to get real IP through proxy headers
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }
        
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }
        
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private bool IsRateLimited(string clientId)
    {
        if (!_attempts.TryGetValue(clientId, out var attempts))
            return false;

        var cutoff = DateTime.UtcNow.AddMinutes(-_windowMinutes);
        var recentAttempts = attempts.Where(a => a > cutoff).ToList();
        
        // Update the attempts list to remove old entries
        _attempts[clientId] = recentAttempts;
        
        return recentAttempts.Count >= _maxAttempts;
    }

    private void RecordAttempt(string clientId)
    {
        _attempts.AddOrUpdate(
            clientId,
            new List<DateTime> { DateTime.UtcNow },
            (key, existing) =>
            {
                var cutoff = DateTime.UtcNow.AddMinutes(-_windowMinutes);
                var recent = existing.Where(a => a > cutoff).ToList();
                recent.Add(DateTime.UtcNow);
                return recent;
            });
    }
}