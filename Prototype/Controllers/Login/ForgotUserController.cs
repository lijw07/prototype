using Microsoft.AspNetCore.Mvc;
using Prototype.DTOs;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("[controller]")]
public class ForgotUserController : ControllerBase
{
    private readonly IUserAccountService _userAccountService;
    private readonly ILogger<ForgotUserController> _logger;

    public ForgotUserController(
        IUserAccountService userAccountService,
        ILogger<ForgotUserController> logger)
    {
        _userAccountService = userAccountService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> ForgotUser([FromBody] ForgotUserRequestDto requestDto)
    {
        try
        {
            var result = await _userAccountService.ForgotPasswordAsync(requestDto);
            
            if (!result.Success)
                return BadRequest(result);
                
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing forgot user request for email: {Email}", requestDto.Email);
            return StatusCode(500, new { message = "An internal error occurred" });
        }
    }
}
