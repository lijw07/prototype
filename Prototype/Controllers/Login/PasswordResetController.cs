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
    IEntityCreationFactoryService entityFactory,
    IUnitOfWorkFactoryService uows,
    IJwtTokenFactoryService jwtTokenFactoryService,
    IEmailNotificationFactoryService emailFactoryService, 
    IAuthenticatedUserAccessor userAccessor) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PasswordReset([FromBody] ResetPasswordRequestDto requestDto)
    {
        if (!jwtTokenFactoryService.ValidateToken(requestDto.Token, out ClaimsPrincipal principal))
            return BadRequest("Invalid or expired token.");
        
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            return BadRequest("Token does not contain a valid user ID.");

        var user = await userAccessor.FindUserById(userId);
        if (user is null)
            return BadRequest("Registered account does not exist.");

        var userRecovery = await userAccessor.FindUserRecoveryRequest(user.UserId);
        if (userRecovery is null || userRecovery.IsUsed || userRecovery.ExpiresAt < DateTime.UtcNow)
            return BadRequest("This link is no longer valid or has already been used.");

        if (!requestDto.Password.Equals(requestDto.ReTypePassword))
            return BadRequest("Passwords do not match.");
        
        userRecovery.IsUsed = true;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(requestDto.Password);
        user.UpdatedAt = DateTime.UtcNow;

        var userActivityLog = entityFactory.CreateUserActivityLog(user, ActionTypeEnum.ChangePassword, HttpContext);

        var auditLog = entityFactory.CreateAuditLog(user, ActionTypeEnum.ChangePassword, new List<string>
        {
            nameof(UserModel),
            nameof(UserRecoveryRequestModel)
        });

        uows.UserRecoveryRequests.Update(userRecovery);
        uows.Users.Update(user);
        await uows.UserActivityLogs.AddAsync(userActivityLog);
        await uows.AuditLogs.AddAsync(auditLog);
        await uows.SaveChangesAsync();

        await emailFactoryService.SendPasswordResetVerificationEmail(user.Email);

        return Ok("Your password has been successfully reset.");
    }
}