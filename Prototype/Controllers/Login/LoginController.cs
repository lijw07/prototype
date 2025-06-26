using Microsoft.AspNetCore.Mvc;
using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("login")]
public class LoginController(IAuthenticationService authService, ILogger<LoginController> logger)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto requestDto)
    {
        try
        {
            var result = await authService.AuthenticateAsync(requestDto);
            
            if (!result.Success)
                return BadRequest(result);
                
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during login for username: {Username}", requestDto.Username);
            return StatusCode(500, new { message = "An internal error occurred" });
        }
    }
}