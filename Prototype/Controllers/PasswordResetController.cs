using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.Models;
using Prototype.Services;
using ResetPasswordRequest = Prototype.DTOs.ResetPasswordRequest;

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
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var userRecoveryRequest = await context.UserRecoveryRequests
            .Include(userRecoveryRequestModel => userRecoveryRequestModel.User)
            .FirstOrDefaultAsync(r => r.Token == request.Token);

        if (userRecoveryRequest == null || userRecoveryRequest.ExpiresAt < DateTime.Now)
        {
            return BadRequest("Invalid or expired token.");
        }
        
        userRecoveryRequest.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.password);
        context.Users.Update(userRecoveryRequest.User);
        await context.SaveChangesAsync();
        
        var auditLog = entityCreationFactoryService.CreateAuditLogFromResetPassword(userRecoveryRequest);
        await auditLogService.CreateAsync(auditLog);
        
        await emailNotificationService.SendPasswordResetVerificationEmail(userRecoveryRequest.User.Email);
        
        return Ok("Password has been successfully reset.");
    }
}