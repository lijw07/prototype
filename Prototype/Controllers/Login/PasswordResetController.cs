using Microsoft.AspNetCore.Mvc;
using Prototype.Controllers.Navigation;
using Prototype.DTOs;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("login/password-reset")]
public class PasswordResetController(
    IUserAccountService userAccountService,
    ILogger<PasswordResetController> logger)
    : BaseNavigationController(logger)
{
    [HttpPost]
    public async Task<IActionResult> PasswordReset([FromBody] ResetPasswordRequestDto requestDto)
    {
        try
        {
            var result = await userAccountService.ResetPasswordAsync(requestDto);
            
            return !result.Success ? BadRequestWithMessage(result) : SuccessResponse(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during password reset");
            return StatusCode(500, new { message = "An internal error occurred" });
        }
    }
}