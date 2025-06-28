using Microsoft.AspNetCore.Mvc;
using Prototype.Controllers.Navigation;
using Prototype.DTOs;
using Prototype.Exceptions;
using Prototype.Services.Interfaces;
using StackExchange.Redis;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("login")]
public class LoginController(
    IAuthenticationService authService, 
    ILogger<LoginController> logger)
    : BaseNavigationController(logger)
{
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto requestDto)
    {
        try
        {
            var result = await authService.AuthenticateAsync(requestDto);

            if (result.Success)
            {
                Console.WriteLine(SuccessResponse(result));
            }
            
            return result.Success ? SuccessResponse(result) : BadRequestWithMessage(result);
        }
        catch (AuthenticationException ex)
        {
            logger.LogWarning(ex, "Authentication failed for username: {Username} from IP: {IpAddress}", 
                requestDto.Username, HttpContext.Connection.RemoteIpAddress?.ToString());
            return HandleUserNotAuthenticated();
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Validation failed during login for username: {Username}", requestDto.Username);
            return BadRequestWithMessage(new { success = false, message = ex.Message, errors = ex.ValidationErrors });
        }
        catch (ExternalServiceException ex)
        {
            logger.LogError(ex, "External service failure during login for username: {Username}", requestDto.Username);
            return StatusCode(503, new { success = false, message = "Authentication service temporarily unavailable" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during login for username: {Username}", requestDto.Username);
            return StatusCode(500, new { success = false, message = "An internal error occurred" });
        }
    }
}