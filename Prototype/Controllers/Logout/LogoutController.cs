using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prototype.Data;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Utility;

namespace Prototype.Controllers.Logout;

[Authorize]
[ApiController]
[Route("[controller]")]
public class LogoutController : ControllerBase
{
    private readonly SentinelContext _context;
    private readonly IAuthenticatedUserAccessor _userAccessor;
    private readonly ILogger<LogoutController> _logger;

    public LogoutController(
        SentinelContext context,
        IAuthenticatedUserAccessor userAccessor,
        ILogger<LogoutController> logger)
    {
        _context = context;
        _userAccessor = userAccessor;
        _logger = logger;
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var user = await _userAccessor.GetCurrentUserAsync();
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
                
                _context.UserActivityLogs.Add(userActivityLog);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("User {Username} logged out successfully", user.Username);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { message = "An internal error occurred" });
        }
    }
}
