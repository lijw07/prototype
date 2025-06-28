namespace Prototype.Services.Interfaces;

/// <summary>
/// Provides HTTP context parsing and client information extraction services
/// </summary>
public interface IHttpContextParsingService
{
    /// <summary>
    /// Extracts the client IP address from the HTTP context
    /// </summary>
    /// <param name="httpContext">The HTTP context to parse</param>
    /// <returns>The client IP address or "Unknown" if not available</returns>
    string GetClientIpAddress(HttpContext? httpContext);

    /// <summary>
    /// Extracts device information from the HTTP context user agent
    /// </summary>
    /// <param name="httpContext">The HTTP context to parse</param>
    /// <returns>Device information string or "Unknown" if not available</returns>
    string GetDeviceInformation(HttpContext? httpContext);

    /// <summary>
    /// Gets the user agent string from the HTTP context
    /// </summary>
    /// <param name="httpContext">The HTTP context to parse</param>
    /// <returns>User agent string or "Unknown" if not available</returns>
    string GetUserAgent(HttpContext? httpContext);
}