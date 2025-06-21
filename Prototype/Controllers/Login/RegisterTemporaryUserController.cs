using Microsoft.AspNetCore.Mvc;
using Prototype.DTOs;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("Register")]
public class RegisterTemporaryUserController : ControllerBase
{
    private readonly IUserAccountService _userAccountService;
    private readonly ILogger<RegisterTemporaryUserController> _logger;

    public RegisterTemporaryUserController(
        IUserAccountService userAccountService,
        ILogger<RegisterTemporaryUserController> logger)
    {
        _userAccountService = userAccountService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto requestDto)
    {
        try
        {
            var result = await _userAccountService.RegisterTemporaryUserAsync(requestDto);
            
            if (!result.Success)
                return BadRequest(result);
                
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email: {Email}", requestDto.Email);
            return StatusCode(500, new { message = "An internal error occurred" });
        }
    }
}