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
    IEntityCreationFactoryService entityCreationFactoryService,
    IUnitOfWorkFactoryService uows,
    IJwtTokenFactoryService jwtTokenFactoryService,
    IEmailNotificationFactoryService emailNotificationFactoryService, 
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
        
        var token = jwtTokenFactoryService.BuildUserClaims(user, ActionTypeEnum.ForgotPassword);
        
        UserActivityLogModel userActivityLog;
        
        if (requestDto.UserRecoveryType == UserRecoveryTypeEnum.PASSWORD)
        {
            await emailNotificationFactoryService.SendPasswordResetEmail(user.Email, token);
            userActivityLog = entityCreationFactoryService.CreateUserActivityLog(user, ActionTypeEnum.ForgotPassword, HttpContext);
        }
        else
        {
            await emailNotificationFactoryService.SendUsernameEmail(user.Email, user.Username);
            userActivityLog = entityCreationFactoryService.CreateUserActivityLog(user, ActionTypeEnum.ForgotUsername, HttpContext);
        }
        
        var userRecoveryLog = entityCreationFactoryService.CreateUserRecoveryRequest(user, requestDto, token);

        var affectedEntities = new List<string>
        {
            nameof(UserRecoveryRequestModel),
            nameof(UserActivityLogModel)
        };
        
        var auditLog = entityCreationFactoryService.CreateAuditLog(user, ActionTypeEnum.ForgotPassword, affectedEntities);
        
        await uows.UserActivityLogs.AddAsync(userActivityLog);
        await uows.UserRecoveryRequests.AddAsync(userRecoveryLog);
        await uows.AuditLogs.AddAsync(auditLog);
        await uows.SaveChangesAsync();
        
        return Ok(new {message = "If your account exists, you will receive an email with a link to reset your password."});

    }
}