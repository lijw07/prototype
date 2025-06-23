using Microsoft.AspNetCore.Mvc;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("[controller]")]
public class VerifyUserController : ControllerBase
{
    private readonly IUserAccountService _userAccountService;
    private readonly ILogger<VerifyUserController> _logger;

    public VerifyUserController(
        IUserAccountService userAccountService,
        ILogger<VerifyUserController> logger)
    {
        _userAccountService = userAccountService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        try
        {
            var result = await _userAccountService.RegisterNewUser(token);
            
            if (!result.Success)
                return BadRequest(result);
                
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email verification for token");
            return StatusCode(500, new { message = "An internal error occurred" });
        }
    }
}