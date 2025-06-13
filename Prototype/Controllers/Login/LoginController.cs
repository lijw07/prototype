using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Models;
using Prototype.Services.Interfaces;
using Prototype.Enum;
using Prototype.Utility;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("[controller]")]
public class LoginController(
    IEntityCreationService entityCreation,
    IUnitOfWorkService uows,
    IAuthenticatedUserAccessor userAccessor,
    IJwtTokenService jwtTokenService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto requestDto)
    {
        var usernameMissing = string.IsNullOrWhiteSpace(requestDto.Username);
        var passwordMissing = string.IsNullOrWhiteSpace(requestDto.Password);

        if (usernameMissing && passwordMissing)
            return BadRequest(new { message = "Username and password cannot be empty" });

        if (usernameMissing)
            return BadRequest(new { message = "Username cannot be empty" });

        if (passwordMissing)
            return BadRequest(new { message = "Password cannot be empty" });
        
        if (!await userAccessor.ValidateUser(requestDto.Username, requestDto.Password))
            return Unauthorized(new { message = "Invalid username or password" });
        
        var user = await userAccessor.GetUser(requestDto.Username, requestDto.Password);
        var userActivityLog = entityCreation.CreateUserActivityLog(user, ActionTypeEnum.Login, HttpContext);
        await uows.UserActivityLogs.AddAsync(userActivityLog);
        await uows.SaveChangesAsync();
        
        var token = jwtTokenService.BuildUserClaims(user, ActionTypeEnum.Login);
        return Ok(new { token });
    }
}