using Microsoft.AspNetCore.Mvc;
using Prototype.DTOs;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("login")]
public class LoginController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<LoginController> _logger;

    public LoginController(IAuthenticationService authService, ILogger<LoginController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto requestDto)
    {
        try
        {
            var result = await _authService.AuthenticateAsync(requestDto);
            
            if (!result.Success)
                return BadRequest(result);
                
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for username: {Username}", requestDto.Username);
            return StatusCode(500, new { message = "An internal error occurred" });
        }
    }
}