namespace Prototype.Services.Interfaces;

public interface IDeviceInformationService
{
    string GetClientIpAddress(HttpContext? httpContext);
    string GetDeviceInformation(HttpContext? httpContext);
    string ParseUserAgent(string userAgent);
    bool IsMobileDevice(string userAgent);
    string GetBrowserName(string userAgent);
    string GetOperatingSystem(string userAgent);
}