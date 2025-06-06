using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Models;
using Prototype.Services;

namespace Prototype.Controllers;

[ApiController]
[Route("[controller]")]
public class PasswordResetController(
    IEntityCreationFactoryService entityCreationFactoryService,
    IEntitySaveService<AuditLogModel> auditLogService,
    IEmailNotificationService emailNotificationService,
    SentinelContext context) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto requestDto)
    {
        var userRecoveryRequest = await context.UserRecoveryRequests
            .Include(userRecoveryRequestModel => userRecoveryRequestModel.User)
            .FirstOrDefaultAsync(r => r.Token == requestDto.Token);

        if (userRecoveryRequest == null || userRecoveryRequest.ExpiresAt < DateTime.Now)
        {
            return BadRequest("Invalid or expired token.");
        }
        
        userRecoveryRequest.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(requestDto.password);
        context.Users.Update(userRecoveryRequest.User);
        await context.SaveChangesAsync();
        
        var auditLog = entityCreationFactoryService.CreateAuditLogFromResetPassword(userRecoveryRequest);
        await auditLogService.CreateAsync(auditLog);
        
        await emailNotificationService.SendPasswordResetVerificationEmail(userRecoveryRequest.User.Email);
        
        return Ok("Password has been successfully reset.");
    }
}