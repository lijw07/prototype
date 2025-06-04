using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Models;
using Prototype.Utility;

namespace Prototype.Controllers;

[ApiController]
[Route("[controller]")]
public class RegisterTemporaryUserController(SentinelContext context) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await context.Users.AnyAsync(u => u.Username == request.Username))
            return Conflict(new { message = "Account already exists!" });

        var userSession = new UserSessionModel
        {
            UserSessionId = Guid.NewGuid(),
            ActionType = ActionTypeEnum.Create,
            ResourceAffected = "Temporary User has been created",
            CreatedAt = DateTime.Now.Date
        };
        
        var verificationCode = RandomNumberGenerator.GetInt32(000000, 100000).ToString();
        var tempUser = new TemporaryUserModel
        {
            TemporaryUserId = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.PasswordHash),
            Firstname = request.Firstname,
            Lastname = request.Lastname,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Manager = request.Manager,
            Department = request.Department,
            Location = $"{request.Address}, {request.City}, {request.State}, {request.ZipCode}",
            JobTitle = request.JobTitle,
            Application = null,
            ActiveDirectory = null,
            AuditLog = null,
            UserSessionId = userSession.UserSessionId,
            UserSession = userSession,
            //HumanResourceId = default,
            //HumanResource = null,
            Permission = PermissionEnum.USER,
            Status = StatusEnum.ACTIVE,
            CreatedAt = DateTime.Now.Date,
            UpdatedAt = DateTime.Now.Date,
            VerificationCode = verificationCode,
            RequestedAt = DateTime.Now.Date,
        };
        
        context.TemporaryUser.Add(tempUser);
        await context.SaveChangesAsync();
        await EmailNotification.SendVerificationEmail(tempUser.Email, verificationCode);
        return Ok(new { id = tempUser.TemporaryUserId, message = "Registration Successful" });
    }
}