using Microsoft.AspNetCore.Mvc;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;
using Prototype.Utility;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("[controller]")]
public class ForgotUserController(
    IEntityCreationService entityCreationService,
    IUnitOfWorkService uows,
    IJwtTokenService jwtTokenService,
    IEmailNotificationService emailNotificationService, 
    IAuthenticatedUserAccessor userAccessor) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ForgotUser([FromBody] ForgotUserRequestDto requestDto)
    {
        if (string.IsNullOrWhiteSpace(requestDto.Email))
            return BadRequest("Email cannot be empty");
        
        var user = await userAccessor.FindUserByEmail(requestDto.Email);

        if (user is null) 
            return BadRequest("No account exist with that email address");
        
        var token = jwtTokenService.BuildUserClaims(user, ActionTypeEnum.ForgotPassword);
        
        UserActivityLogModel userActivityLog;

        
        if (requestDto.UserRecoveryType == UserRecoveryTypeEnum.PASSWORD)
        {
            await emailNotificationService.SendPasswordResetEmail(user.Email, token);
            userActivityLog = entityCreationService.CreateUserActivityLog(user, ActionTypeEnum.ForgotPassword, HttpContext);
        }
        else
        {
            await emailNotificationService.SendUsernameEmail(user.Email, user.Username);
            userActivityLog = entityCreationService.CreateUserActivityLog(user, ActionTypeEnum.ForgotUsername, HttpContext);
        }
        
        var userRecoveryLog = entityCreationService.CreateUserRecoveryRequest(user, requestDto, token);

        var affectedEntities = new List<string>
        {
            nameof(UserRecoveryRequestModel),
            nameof(UserActivityLogModel)
        };
        
        var auditLog = entityCreationService.CreateAuditLog(user, ActionTypeEnum.ForgotPassword, affectedEntities);
        
        await uows.UserActivityLogs.AddAsync(userActivityLog);
        await uows.UserRecoveryRequests.AddAsync(userRecoveryLog);
        await uows.AuditLogs.AddAsync(auditLog);
        await uows.SaveChangesAsync();
        
        return Ok(new {message = "If your account exists, you will receive an email with a link to reset your password."});

    }
}