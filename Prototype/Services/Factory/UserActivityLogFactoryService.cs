using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;
using UAParser;

namespace Prototype.Services.Factory;

public class UserActivityLogFactoryService : IUserActivityLogFactoryService
{
    public UserActivityLogModel CreateFromLogin(UserModel user, HttpContext httpContext)
    {
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var uaParser = Parser.GetDefault();
        var clientInfo = uaParser.Parse(userAgent);
        var os = clientInfo.OS.ToString();
        var browser = clientInfo.UA.ToString();

        return new UserActivityLogModel
        {
            UserActivityLogId = Guid.NewGuid(),
            UserId = user.UserId,
            User = user,
            IPAddress = ipAddress,
            DeviceInformation = $"{browser} on {os}",
            ActionType = ActionTypeEnum.Login,
            Description = $"User {user.Username} logged in from IP {ipAddress} using {browser} on {os}.",
            Timestamp = DateTime.Now
        };
    }

    public UserActivityLogModel CreateFromPasswordChange(UserModel user, HttpContext httpContext)
    {
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var uaParser = Parser.GetDefault();
        var clientInfo = uaParser.Parse(userAgent);
        var os = clientInfo.OS.ToString();
        var browser = clientInfo.UA.ToString();

        return new UserActivityLogModel
        {
            UserActivityLogId = Guid.NewGuid(),
            UserId = user.UserId,
            User = user,
            IPAddress = ipAddress,
            DeviceInformation = $"{browser} on {os}",
            ActionType = ActionTypeEnum.Login,
            Description = $"User {user.Username} changed password from IP {ipAddress} using {browser} on {os}.",
            Timestamp = DateTime.Now
        };
    }

    public UserActivityLogModel CreateFromDataDump(UserModel user, HttpContext httpContext)
    {
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var uaParser = Parser.GetDefault();
        var clientInfo = uaParser.Parse(userAgent);
        var os = clientInfo.OS.ToString();
        var browser = clientInfo.UA.ToString();

        return new UserActivityLogModel
        {
            UserActivityLogId = Guid.NewGuid(),
            UserId = user.UserId,
            User = user,
            IPAddress = ipAddress,
            DeviceInformation = $"{browser} on {os}",
            ActionType = ActionTypeEnum.Login,
            Description = $"User {user.Username} created a data-dump from IP {ipAddress} using {browser} on {os}.",
            Timestamp = DateTime.Now
        };
    }
}