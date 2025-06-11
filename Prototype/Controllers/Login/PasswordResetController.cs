using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("[controller]")]
public class PasswordResetController(
    IEntityCreationFactoryService entityCreationFactoryService,
    IUnitOfWorkService uows,
    IEmailNotificationService emailNotificationService,
    IConfiguration config,
    SentinelContext context) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto requestDto)
    {
        try
        {
            var principal = ValidateJwtToken(requestDto.Token, config["JwtSettings:Key"]);
            if (principal == null)
                return BadRequest("Invalid or tampered token.");

            var email = principal.FindFirstValue("email");
            var code = principal.FindFirstValue("code");

            var userRecoveryRequest = await context.UserRecoveryRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r =>
                    r.User.Email == email &&
                    r.VerificationCode == code);

            if (userRecoveryRequest == null ||
                userRecoveryRequest.ExpiresAt < DateTime.UtcNow ||
                userRecoveryRequest.IsUsed)
            {
                return BadRequest("Invalid or expired token.");
            }

            userRecoveryRequest.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(requestDto.password);
            userRecoveryRequest.IsUsed = true;

            context.Users.Update(userRecoveryRequest.User);
            context.UserRecoveryRequests.Update(userRecoveryRequest);

            var auditLog = entityCreationFactoryService.CreateFromResetPassword(userRecoveryRequest);
            await uows.AuditLogs.AddAsync(auditLog);
            await context.SaveChangesAsync();

            await emailNotificationService.SendPasswordResetVerificationEmail(userRecoveryRequest.User.Email);
            return Ok("Password has been successfully reset.");
        }
        catch (SecurityTokenException)
        {
            return BadRequest("Invalid or tampered token.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Unexpected error: {ex.Message}");
        }
    }

    private static ClaimsPrincipal? ValidateJwtToken(string token, string secretKey)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secretKey);

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        try
        {
            return tokenHandler.ValidateToken(token, validationParams, out _);
        }
        catch
        {
            return null;
        }
    }
}