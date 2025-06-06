using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.Models;
using System.Security.Claims;
using Prototype.DTOs;

namespace Prototype.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly SentinelContext _context;

    public SettingsController(SentinelContext context)
    {
        _context = context;
    }
    
    private Guid? GetCurrentUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdString, out var userId) ? userId : null;
    }
    
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var user = await _context.Users
            .Where(u => u.UserId == userId)
            .Select(u => new
            {
                u.Username,
                u.FirstName,
                u.LastName,
                u.Email,
                u.PhoneNumber
            })
            .FirstOrDefaultAsync();

        if (user == null) return NotFound();

        return Ok(user);
    }

    // PUT: /settings/profile
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UserProfileUpdateRequest dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        // Update allowed fields
        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Email = dto.Email;
        user.PhoneNumber = dto.PhoneNumber;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] UserProfileUpdatePasswordRequest dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        // TODO: Hash & verify password in real-world scenario!
        if (user.PasswordHash != dto.OldPassword)
            return BadRequest("Old password does not match.");

        user.PasswordHash = dto.NewPassword;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    
    [HttpGet("applications")]
    public async Task<IActionResult> GetUserApplications()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var apps = await _context.UserApplications
            .Where(ua => ua.UserId == userId)
            .Include(ua => ua.Application)
            .Select(ua => new
            {
                ua.Application.ApplicationId,
                ua.Application.ApplicationName,
                ua.Application.CreatedAt,
                ua.Application.UpdatedAt,
                // Add other fields as needed
            })
            .ToListAsync();

        return Ok(apps);
    }

    [HttpPost("applications")]
    public async Task<IActionResult> AddApplication([FromBody] AddApplicationRequest dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var application = new ApplicationModel
        {
            ApplicationId = Guid.NewGuid(),
            ApplicationName = dto.ApplicationName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _context.Applications.Add(application);

        var userApp = new UserApplicationModel
        {
            UserId = userId.Value,
            ApplicationId = application.ApplicationId
        };
        _context.UserApplications.Add(userApp);

        await _context.SaveChangesAsync();
        return Ok(new { application.ApplicationId });
    }

    [HttpDelete("applications/{id:guid}")]
    public async Task<IActionResult> RemoveApplication(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var userApp = await _context.UserApplications
            .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.ApplicationId == id);

        if (userApp == null) return NotFound();

        _context.UserApplications.Remove(userApp);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}