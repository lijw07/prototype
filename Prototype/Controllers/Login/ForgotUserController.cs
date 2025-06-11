using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("[controller]")]
public class ForgotUserController(
    IEntityCreationFactoryService entityCreationFactoryService,
    IUnitOfWorkService uows,
    IJwtTokenService jwtTokenService,
    IEmailNotificationService emailNotificationService,
    SentinelContext context)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ForgotUser([FromBody] ForgotUserRequestDto requestDto)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email == requestDto.Email);

        if (user == null)
        {
            return BadRequest("Invalid email, No account exist with that email address");
        }
        
        var token = jwtTokenService.BuildUserClaims(user, JwtPurposeTypeEnum.ForgotUser);

        var userRecoveryLog = entityCreationFactoryService.CreateFromForgotUser(user, requestDto, token);
        var auditLog = entityCreationFactoryService.CreateFromForgotUser(user, requestDto, userRecoveryLog);
        var userActivityLog = entityCreationFactoryService.CreateUserActivityLog(user, ActionTypeEnum.ForgotPassword, HttpContext);
        await uows.UserActivityLogs.AddAsync(userActivityLog);
        await uows.UserRecoveryRequests.AddAsync(userRecoveryLog);
        await uows.AuditLogs.AddAsync(auditLog);
        await uows.SaveChangesAsync();
        if (requestDto.UserRecoveryType == UserRecoveryTypeEnum.PASSWORD)
        {
            await emailNotificationService.SendPasswordResetEmail(user.Email, token);
        }
        else
        {
            await emailNotificationService.SendUsernameEmail(user.Email, user.Username);
        }
        return Ok(new {message = "If your account exists, you will receive an email with a link to reset your password."});

    }
}