using Microsoft.AspNetCore.Mvc;
using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("PasswordReset")]
public class PasswordResetController(
    IUserAccountService userAccountService,
    ILogger<PasswordResetController> logger)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PasswordReset([FromBody] ResetPasswordRequestDto requestDto)
    {
        try
        {
            var result = await userAccountService.ResetPasswordAsync(requestDto);
            
            if (!result.Success)
                return BadRequest(result);
                
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during password reset");
            return StatusCode(500, new { message = "An internal error occurred" });
        }
    }
}