using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Utility;

namespace Prototype.Controllers.Logout;

[Authorize]
[ApiController]
[Route("[controller]")]
public class LogoutController(
    SentinelContext context,
    IAuthenticatedUserAccessor userAccessor,
    ILogger<LogoutController> logger)
    : ControllerBase
{
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var user = await userAccessor.GetCurrentUserAsync();
            if (user != null)
            {
                var userActivityLog = new UserActivityLogModel
                {
                    UserActivityLogId = Guid.NewGuid(),
                    UserId = user.UserId,
                    User = user,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString(),
                    ActionType = ActionTypeEnum.Logout,
                    Description = "User logged out",
                    Timestamp = DateTime.UtcNow
                };
                
                context.UserActivityLogs.Add(userActivityLog);
                await context.SaveChangesAsync();
                
                logger.LogInformation("User {Username} logged out successfully", user.Username);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { message = "An internal error occurred" });
        }
    }
}
