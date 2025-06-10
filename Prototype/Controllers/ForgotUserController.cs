using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers;

[ApiController]
[Route("[controller]")]
public class ForgotUserController(
    IEntityCreationFactoryService entityCreationFactoryService,
    IEntitySaveService<AuditLogModel> auditLogService,
    IEntitySaveService<UserRecoveryRequestModel> userRecoveryLogService,
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
            return BadRequest("Invalid email, No account exist with that email address");
        }

        var userRecoveryLog = entityCreationFactoryService.CreateUserRecoveryRequestFronForgotUser(user, requestDto, verificationService.GenerateVerificationCode());;
        await userRecoveryLogService.CreateAsync(userRecoveryLog);
        
        var auditLog = entityCreationFactoryService.CreateAuditLogFromForgotUser(user, requestDto, userRecoveryLog);
        await auditLogService.CreateAsync(auditLog);

        if (requestDto.UserRecoveryType == UserRecoveryTypeEnum.PASSWORD)
        {
            await emailNotificationService.SendPasswordResetEmail(user.Email, userRecoveryLog.Token);
        }
        else
        {
            await emailNotificationService.SendUsernameEmail(user.Email, user.Username);
        }
        return Ok(new {message = "If your account exists, you will receive an email with a link to reset your password."});

    }
}