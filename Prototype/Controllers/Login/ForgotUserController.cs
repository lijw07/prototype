using Microsoft.AspNetCore.Mvc;
using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("ForgotUser")]
public class ForgotUserController(
    IUserAccountService userAccountService,
    ILogger<ForgotUserController> logger)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ForgotUser([FromBody] ForgotUserRequestDto requestDto)
    {
        try
        {
            var result = await userAccountService.ForgotPasswordAsync(requestDto);
            
            if (!result.Success)
                return BadRequest(result);
                
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing forgot user request for email: {Email}", requestDto.Email);
            return StatusCode(500, new { message = "An internal error occurred" });
        }
    }
}
