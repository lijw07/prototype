using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;
using UAParser;

namespace Prototype.Services.Factory;

public class UserActivityLogFactoryService : IUserActivityLogFactoryService
{
    public UserActivityLogModel CreateUserActivityLog(UserModel? user, ActionTypeEnum action, HttpContext httpContext)
    {
        if (user is null)
            throw new ArgumentNullException(nameof(user), "User cannot be null when creating a UserActivityLog.");
        
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
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
            IpAddress = ipAddress,
            DeviceInformation = $"{browser} on {os}",
            ActionType = action,
            Description = $"User {user.Username} performed {action} from IP {ipAddress} using {browser} on {os}.",
            Timestamp = DateTime.Now
        };
    }
}