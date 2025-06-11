using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Services.Interfaces;
using System.Security.Claims;
using Prototype.Enum;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("[controller]")]
public class PasswordResetController(
    IEntityCreationFactoryService entityFactory,
    IUnitOfWorkService uows,
    IJwtTokenService jwtTokenService,
    IEmailNotificationService emailService,
    SentinelContext dbContext) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto requestDto)
    {
        if (!jwtTokenService.ValidateToken(requestDto.Token, out ClaimsPrincipal principal))
            return BadRequest("Invalid or expired token.");

        var email = principal.FindFirst("email")?.Value;
        var code = principal.FindFirst("code")?.Value;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            return BadRequest("Registered account does not exist!");

        var recoveryRequest = await dbContext.UserRecoveryRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r =>
                r.User.Email == email &&
                r.VerificationCode == code);

        if (recoveryRequest is null || recoveryRequest.IsUsed)
            return BadRequest("This link is no longer valid or has already been used.");
        
        if (recoveryRequest.ExpiresAt < DateTime.Now)
            return BadRequest("This link has expired.");
        
        recoveryRequest.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(requestDto.Password);
        recoveryRequest.IsUsed = true;

        dbContext.Users.Update(recoveryRequest.User);
        dbContext.UserRecoveryRequests.Update(recoveryRequest);

        var auditLog = entityFactory.CreateFromResetPassword(recoveryRequest);
        var userActivityLog = entityFactory.CreateUserActivityLog(recoveryRequest.User, ActionTypeEnum.ChangePassword, HttpContext);
        await uows.UserActivityLogs.AddAsync(userActivityLog);
        await uows.AuditLogs.AddAsync(auditLog);
        await dbContext.SaveChangesAsync();

        await emailService.SendPasswordResetVerificationEmail(recoveryRequest.User.Email);
        return Ok("Your password has been successfully reset.");
    }
}