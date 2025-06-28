using Microsoft.AspNetCore.Mvc;
using Prototype.Controllers.Navigation;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("login/verify-user")]
public class VerifyUserController(
    IUserAccountService userAccountService,
    ILogger<VerifyUserController> logger)
    : BaseNavigationController(logger)
{
    [HttpGet]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        try
        {
            var result = await userAccountService.RegisterNewUser(token);
            
            return !result.Success ? BadRequestWithMessage(result) : SuccessResponse(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during email verification for token");
            return StatusCode(500, new { message = "An internal error occurred" });
        }
    }
}