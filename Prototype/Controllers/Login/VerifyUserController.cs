using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prototype.Data;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("[controller]")]
public class VerifyUserController(
    IUnitOfWorkService uows,
    IEntityCreationFactoryService tempUserFactory,
    IEmailNotificationService emailNotificationService,
    SentinelContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("your-super-secret-key-goes-here");

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out _);

            var email = principal.FindFirst("email")?.Value;
            var code = principal.FindFirst("code")?.Value;
            Console.WriteLine(code);

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
                return BadRequest("Invalid token.");

            var requestedUser = await context.TemporaryUsers
                .FirstOrDefaultAsync(t => t.Email == email && t.VerificationCode == code);

            if (requestedUser == null || requestedUser.CreatedAt.AddHours(24) < DateTime.Now)
                return BadRequest("Invalid or expired verification link.");

            var tempUser = tempUserFactory.CreateUserFromTemporary(requestedUser);
            await uows.Users.AddAsync(tempUser);
            await uows.SaveChangesAsync();
            uows.TemporaryUser.Delete(requestedUser);
            await context.SaveChangesAsync();
            await emailNotificationService.SendAccountCreationEmail(tempUser.Email, tempUser.Username);

            return Ok("Your email has been verified. You may now log in.");
        }
        catch (Exception)
        {
            return BadRequest("Invalid or expired token.");
        }
    }
}