using Microsoft.AspNetCore.Mvc;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Services.Interfaces;
using Prototype.Utility;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("[controller]")]
public class RegisterTemporaryUserController(
    IUnitOfWorkFactoryService uows,
    IJwtTokenFactoryService jwtTokenFactoryService,
    IEntityCreationFactoryService tempUserFactory,
    IEmailNotificationFactoryService emailNotificationFactoryService,
    IAuthenticatedUserAccessor userAccessor) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto requestDto)
    {
        if (await userAccessor.EmailExistsAsync(requestDto.Email) || await userAccessor.TemporaryEmailExistsAsync(requestDto.Email))
            return Conflict(new { message = "Email already exists!" });
        
        if (await userAccessor.UsernameExistsAsync(requestDto.Username) || await userAccessor.TemporaryUsernameExistsAsync(requestDto.Username))
            return Conflict(new { message = "Username already in use!" });
        
        var token = jwtTokenFactoryService.BuildUserClaims(requestDto, ActionTypeEnum.CreateUser);
        var tempUser = tempUserFactory.CreateTemporaryUser(requestDto, token);
        
        await uows.TemporaryUser.AddAsync(tempUser);
        await uows.SaveChangesAsync();
        
        await emailNotificationFactoryService.SendVerificationEmail(tempUser.Email, token);
        
        return Ok(new { id = tempUser.TemporaryUserId, message = "Registration successful. Please check your email to verify your account." });
    }
}