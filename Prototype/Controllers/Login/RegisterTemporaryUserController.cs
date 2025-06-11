using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("[controller]")]
public class RegisterTemporaryUserController(
    IUnitOfWorkService uows,
    IJwtTokenService jwtTokenService,
    IEntityCreationFactoryService tempUserFactory,
    IEmailNotificationService emailNotificationService,
    SentinelContext context)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto requestDto)
    {
        if (await context.Users.AnyAsync(u => u.Email == requestDto.Email))
            return Conflict(new { message = "Email already exists!" });

        if (await context.TemporaryUsers.AnyAsync(tu => tu.Email == requestDto.Email))
            return Conflict(new { message = "Temporary registration already exists for this email!" });

        var token = jwtTokenService.BuildUserClaims(requestDto, JwtPurposeTypeEnum.Verification);
        var tempUser = tempUserFactory.CreateTemporaryUser(requestDto, token);
        await uows.TemporaryUser.AddAsync(tempUser);
        await uows.SaveChangesAsync();
        await emailNotificationService.SendVerificationEmail(tempUser.Email, token);
        return Ok(new { id = tempUser.TemporaryUserId, message = "Registration successful. Please check your email to verify your account." });
    }
}