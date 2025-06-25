using Microsoft.AspNetCore.Mvc;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("[controller]")]
public class VerifyUserController(
    IUserAccountService userAccountService,
    ILogger<VerifyUserController> logger)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        try
        {
            var result = await userAccountService.RegisterNewUser(token);
            
            if (!result.Success)
                return BadRequest(result);
                
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during email verification for token");
            return StatusCode(500, new { message = "An internal error occurred" });
        }
    }
}