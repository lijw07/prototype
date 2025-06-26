using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class DeviceInformationService : IDeviceInformationService
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

        var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault();
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown";

        return ParseUserAgent(userAgent);
    }

    public string ParseUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown";

        var deviceInfo = new List<string>();

        // Check for mobile devices
        if (IsMobileDevice(userAgent))
            deviceInfo.Add("Mobile");
        else if (userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Tablet");
        else
            deviceInfo.Add("Desktop");

        // Add browser information
        var browser = GetBrowserName(userAgent);
        if (!string.IsNullOrEmpty(browser))
            deviceInfo.Add(browser);

        // Add operating system
        var os = GetOperatingSystem(userAgent);
        if (!string.IsNullOrEmpty(os))
            deviceInfo.Add(os);

        return string.Join(" - ", deviceInfo);
    }

    public bool IsMobileDevice(string userAgent)
    {
        return userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase) ||
               userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
               userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase);
    }

    public string GetBrowserName(string userAgent)
    {
        if (userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
            return "Chrome";
        if (userAgent.Contains("Firefox", StringComparison.OrdinalIgnoreCase))
            return "Firefox";
        if (userAgent.Contains("Safari", StringComparison.OrdinalIgnoreCase))
            return "Safari";
        if (userAgent.Contains("Edge", StringComparison.OrdinalIgnoreCase))
            return "Edge";
        if (userAgent.Contains("Opera", StringComparison.OrdinalIgnoreCase))
            return "Opera";

        return "Unknown Browser";
    }

    public string GetOperatingSystem(string userAgent)
    {
        if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
            return "Windows";
        if (userAgent.Contains("Mac OS", StringComparison.OrdinalIgnoreCase))
            return "macOS";
        if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
            return "Linux";
        if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
            return "Android";
        if (userAgent.Contains("iOS", StringComparison.OrdinalIgnoreCase))
            return "iOS";

        return "Unknown OS";
    }
}