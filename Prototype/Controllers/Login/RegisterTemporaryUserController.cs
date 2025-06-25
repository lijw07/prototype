using Microsoft.AspNetCore.Mvc;
using Prototype.DTOs;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("Register")]
public class RegisterTemporaryUserController(
    IUserAccountService userAccountService,
    ILogger<RegisterTemporaryUserController> logger)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto requestDto)
    {
        try
        {
            var result = await userAccountService.RegisterTemporaryUserAsync(requestDto);
            
            if (!result.Success)
                return BadRequest(result);
                
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during registration for email: {Email}", requestDto.Email);
            return StatusCode(500, new { message = "An internal error occurred" });
        }
    }
}