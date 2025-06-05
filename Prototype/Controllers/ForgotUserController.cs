using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Models;
using Prototype.Services;
using Prototype.Utility;

namespace Prototype.Controllers;

[ApiController]
[Route("[controller]")]
public class ForgotUserController(
    IEntityCreationFactoryService userRecoveryRequestService,
    IEntitySaveService<AuditLogModel> auditLogService,
    IEntitySaveService<UserRecoveryRequestModel> userRecoveryLogService,
    IVerificationService verificationService,
    IEmailNotificationService emailNotificationService,
    SentinelContext context)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ForgotUser([FromBody] ForgotUserRequest request)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            return BadRequest("Invalid email, No account exist with that email address");
        }

        var userRecoveryLog = userRecoveryRequestService.CreateUserRecoveryRequestFronForgotUser(user, request, verificationService.GenerateVerificationCode());;
        await userRecoveryLogService.CreateAsync(userRecoveryLog);
        
        var auditLog = userRecoveryRequestService.CreateAuditLogFromForgotUser(user, request, userRecoveryLog);
        await auditLogService.CreateAsync(auditLog);

        if (request.UserRecoveryType == UserRecoveryTypeEnum.PASSWORD)
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