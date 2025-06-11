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
    IVerificationService verificationService,
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
            return BadRequest("Invalid email, No account exists with that email address");
        }

        var verificationCode = verificationService.GenerateVerificationCode();

        var userRecoveryLog = entityCreationFactoryService.CreateFromForgotUser(user, requestDto, verificationCode);
        await uows.UserRecoveryRequests.AddAsync(userRecoveryLog);

        var auditLog = entityCreationFactoryService.CreateFromForgotUser(user, requestDto, userRecoveryLog);
        await uows.AuditLogs.AddAsync(auditLog);
        await uows.SaveChangesAsync();

        if (requestDto.UserRecoveryType == UserRecoveryTypeEnum.PASSWORD)
        {
            await emailNotificationService.SendPasswordResetEmail(user.Email, verificationCode);
        }
        else
        {
            await emailNotificationService.SendUsernameEmail(user.Email, user.Username);
        }

        return Ok(new { message = "If your account exists, you will receive an email with a link to reset your password." });
    }
}