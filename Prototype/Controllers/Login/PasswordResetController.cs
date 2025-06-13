using Microsoft.AspNetCore.Mvc;
using Prototype.DTOs;
using Prototype.Services.Interfaces;
using System.Security.Claims;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Utility;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("[controller]")]
public class PasswordResetController(
    IEntityCreationService entity,
    IUnitOfWorkService uows,
    IJwtTokenService jwtTokenService,
    IEmailNotificationService emailService, 
    IAuthenticatedUserAccessor userAccessor) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PasswordReset([FromBody] ResetPasswordRequestDto requestDto)
    {
        if (!jwtTokenService.ValidateToken(requestDto.Token, out ClaimsPrincipal principal))
            return BadRequest("Invalid or expired token.");

        var email = principal.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("Token does not contain a valid email.");
        
        var user = await userAccessor.FindUserByEmail(email);

        if (user is null)
            return BadRequest("Registered account does not exist!");
        
        var userRecovery = await userAccessor.FindUserRecoveryRequest(user.UserId);

        if (userRecovery is null || userRecovery.ExpiresAt < DateTime.Now)
            return BadRequest("This link is no longer valid or has already been used.");

        if (!requestDto.Password.Equals(requestDto.ReTypePassword))
            return BadRequest("Passwords do not match.");
        
        userRecovery.IsUsed = true;
        
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(requestDto.Password);
        user.UpdatedAt = DateTime.Now;
        
        var userActivityLog = entity.CreateUserActivityLog(user, ActionTypeEnum.ChangePassword, HttpContext);
        
        var affectedEntities = new List<string>
        {
            nameof(UserModel),
            nameof(UserRecoveryRequestModel)
        };
        
        var auditLog = entity.CreateAuditLog(user, ActionTypeEnum.ChangePassword, affectedEntities);
        
        await uows.UserRecoveryRequests.AddAsync(userRecovery);
        await uows.Users.AddAsync(user);
        await uows.UserActivityLogs.AddAsync(userActivityLog);
        await uows.AuditLogs.AddAsync(auditLog);
        await uows.SaveChangesAsync();

        await emailService.SendPasswordResetVerificationEmail(user.Email);
        return Ok("Your password has been successfully reset.");
    }
}