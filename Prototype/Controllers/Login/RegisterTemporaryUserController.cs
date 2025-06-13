using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Services.Interfaces;
using Prototype.Utility;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("[controller]")]
public class RegisterTemporaryUserController(
    IUnitOfWorkService uows,
    IJwtTokenService jwtTokenService,
    IEntityCreationService entityCreationService,
    IEmailNotificationService emailNotificationService,
    IAuthenticatedUserAccessor userAccessor) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto requestDto)
    {
        if (await userAccessor.EmailExistsAsync(requestDto.Email))
            return Conflict(new { message = "Email already exists!" });
        
        if (!await userAccessor.UsernameExistsAsync(requestDto.Username))
            return Conflict(new { message = "Username already in use!" });
        
        var token = jwtTokenService.BuildUserClaims(requestDto, ActionTypeEnum.CreateUser);
        var tempUser = entityCreationService.CreateTemporaryUser(requestDto, token);
        
        await uows.TemporaryUser.AddAsync(tempUser);
        await uows.SaveChangesAsync();
        
        await emailNotificationService.SendVerificationEmail(tempUser.Email, token);
        
        return Ok(new { id = tempUser.TemporaryUserId, message = "Registration successful. Please check your email to verify your account." });
    }
}