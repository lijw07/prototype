using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Models;
using Prototype.Utility;

namespace Prototype.Controllers;

[ApiController]
[Route("[controller]")]
public class RegisterVerifiedUserController(SentinelContext context) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Verify([FromBody] EmailVerificationRequest request)
    {
        var tempUser = await context.TemporaryUser.Include(temporaryUserModel => temporaryUserModel.UserSession)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.VerificationCode == request.VerificationCode);

        if (tempUser == null || tempUser.RequestedAt.AddHours(24) < DateTime.UtcNow)
            return BadRequest(new { message = "Invalid or expired verification code" });
        
        var user = new UserModel
        {
            UserId = Guid.NewGuid(),
            Username = tempUser.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempUser.PasswordHash),
            FirstName = tempUser.Firstname,
            LastName = tempUser.Lastname,
            Email = tempUser.Email,
            PhoneNumber = tempUser.PhoneNumber,
            Manager = tempUser.Manager,
            Department = tempUser.Department,
            Location = tempUser.Location,
            JobTitle = tempUser.JobTitle,
            UserApplication = null,
            ActiveDirectory = null,
            AuditLog = null,
            UserSessionId = tempUser.UserSessionId,
            UserSession = tempUser.UserSession,
            //HumanResourceId = Guid.NewGuid(),
            //HumanResource = null,
            Permission = PermissionEnum.USER,
            Status = StatusEnum.ACTIVE,
            CreatedAt = tempUser.CreatedAt,
            UpdatedAt = tempUser.UpdatedAt
        };

        context.Users.Add(user);
        context.TemporaryUser.Remove(tempUser);
        await context.SaveChangesAsync();
        return Ok(new { message = "Email Address has been verified! You can now close this window." });
    }
}