using Microsoft.AspNetCore.Mvc;
using Prototype.Controllers.Navigation;
using Prototype.DTOs;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("login/forgot-user")]
public class ForgotUserController(
    IUserAccountService userAccountService,
    ILogger<ForgotUserController> logger)
    : BaseNavigationController(logger)
{
    [HttpPost]
    public async Task<IActionResult> ForgotUser([FromBody] ForgotUserRequestDto requestDto)
    {
        try
        {
            var result = await userAccountService.ForgotPasswordAsync(requestDto);
            
            return !result.Success ? BadRequest(result) : SuccessResponse(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing forgot user request for email: {Email}", requestDto.Email);
            return StatusCode(500, new { message = "An internal error occurred" });
        }
    }
}
