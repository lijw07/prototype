using Microsoft.AspNetCore.Mvc;
using Prototype.Controllers.Navigation;
using Prototype.DTOs;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("login/register")]
public class RegisterTemporaryUserController(
    IUserAccountService userAccountService,
    ILogger<RegisterTemporaryUserController> logger)
    : BaseNavigationController(logger)
{
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto requestDto)
    {
        try
        {
            var result = await userAccountService.RegisterTemporaryUserAsync(requestDto);
            
            return !result.Success ? BadRequestWithMessage(result) : SuccessResponse(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during registration for email: {Email}", requestDto.Email);
            return StatusCode(500, new { message = "An internal error occurred" });
        }
    }
}