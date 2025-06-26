using Prototype.Services.Interfaces;

namespace Prototype.Services;

/// <summary>
/// Service for parsing HTTP context and extracting client information
/// </summary>
public class HttpContextParsingService : IHttpContextParsingService
{
    public string GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null)
            return "Unknown";

        // Check for forwarded IP first (for reverse proxy scenarios)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check for real IP header
        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to remote IP address
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    public string GetDeviceInformation(HttpContext? httpContext)
    {
        if (httpContext == null)
            return "Unknown";

        var userAgent = GetUserAgent(httpContext);
        if (userAgent == "Unknown")
            return "Unknown";

        // Extract basic device information from User-Agent
        var deviceInfo = new List<string>();

        // Check for mobile devices
        if (userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Mobile");
        else if (userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Tablet");
        else
            deviceInfo.Add("Desktop");

        // Extract browser information
        if (userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Chrome");
        else if (userAgent.Contains("Firefox", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Firefox");
        else if (userAgent.Contains("Safari", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Safari");
        else if (userAgent.Contains("Edge", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Edge");

        // Extract OS information
        if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Windows");
        else if (userAgent.Contains("Mac", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("macOS");
        else if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Linux");
        else if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Android");
        else if (userAgent.Contains("iOS", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("iOS");

        return deviceInfo.Count > 0 ? string.Join(", ", deviceInfo) : "Unknown";
    }

    public string GetUserAgent(HttpContext? httpContext)
    {
        if (httpContext == null)
            return "Unknown";

        var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault();
        return string.IsNullOrEmpty(userAgent) ? "Unknown" : userAgent;
    }
}