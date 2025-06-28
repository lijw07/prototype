using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Prototype.Data;
using Prototype.Enum;
using Prototype.Exceptions;
using Prototype.Utility;
using Prototype.Controllers.Navigation;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.Logout;

[Route("logout")]
public class LogoutController(
    ILogger<LogoutController> logger,
    IUserAccountService userAccountService,
    SentinelContext context,
    IAuthenticatedUserAccessor userAccessor)
    : BaseNavigationController(logger, context, userAccessor)
{
    private readonly IAuthenticatedUserAccessor _userAccessor = userAccessor;

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var user = await _userAccessor.GetCurrentUserAsync(User);
            if (user != null)
            {

                await userAccountService.CreateUserActivityLogAsync(user.UserId, ActionTypeEnum.Logout, "User logged out");
                Logger.LogInformation("User {Username} logged out successfully", user.Username);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return SuccessResponse(new { message = "Logged out successfully" });
        }
        catch (DataNotFoundException ex)
        {
            Logger.LogWarning(ex, "User data not found during logout");
            // Still sign out from the session even if user data is missing
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return SuccessResponse(new { message = "Logged out successfully" });
        }
        catch (ExternalServiceException ex)
        {
            Logger.LogError(ex, "External service failure during logout activity logging");
            // Continue with logout even if activity logging fails
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return SuccessResponse(new { message = "Logged out successfully (activity logging failed)" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error during logout");
            // Ensure logout happens even on unexpected errors
            try
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            catch (Exception signOutEx)
            {
                Logger.LogError(signOutEx, "Failed to sign out user during error recovery");
            }
            return StatusCode(500, new { message = "An internal error occurred during logout" });
        }
    }
}
