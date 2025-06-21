using Microsoft.AspNetCore.Mvc;
using Prototype.DTOs;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("PasswordReset")]
public class PasswordResetController : ControllerBase
{
    private readonly IUserAccountService _userAccountService;
    private readonly ILogger<PasswordResetController> _logger;

    public PasswordResetController(
        IUserAccountService userAccountService,
        ILogger<PasswordResetController> logger)
    {
        _userAccountService = userAccountService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> PasswordReset([FromBody] ResetPasswordRequestDto requestDto)
    {
        try
        {
            var result = await _userAccountService.ResetPasswordAsync(requestDto);
            
            if (!result.Success)
                return BadRequest(result);
                
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
            return StatusCode(500, new { message = "An internal error occurred" });
        }
    }
}