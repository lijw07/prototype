using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Models;
using Prototype.Utility;

namespace Prototype.Controllers;

[ApiController]
public class RegisterController(SentinelContext context) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await context.Users.AnyAsync(u => u.Username == request.Username))
            return Conflict(new { message = "Account already exists!" });

        var user = new UserModel
        {
            UserId = Guid.NewGuid(),
            Username = request.Username,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.Firstname,
            LastName = request.Lastname,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Manager = request.Manager,
            Department = request.Department,
            Location = $"{request.Address}, {request.City}, {request.State}, {request.ZipCode}",
            JobTitle = request.JobTitle,
            Application = null,
            ActiveDirectory = null,
            AuditLog = null,
            UserSessionId = default,
            UserSession = null,
            HumanResourceId = default,
            HumanResource = null,
            Permission = PermissionEnum.USER,
            Status = StatusEnum.ACTIVE,
            CreatedAt = DateTime.Now.Date,
            UpdatedAt = DateTime.Now.Date,
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return Ok(new { id = user.UserId, message = "Registration Successful" });
    }
}