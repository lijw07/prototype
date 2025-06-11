using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Models;
using Prototype.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("[controller]")]
public class LoginController(
    IEntityCreationFactoryService entityCreationFactory,
    IUnitOfWorkService uows,
    IConfiguration config,
    SentinelContext context) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto requestDto)
    {
        var user = await GetUserWithPermissionsAsync(requestDto.Username);

        if (user is null || !IsPasswordValid(requestDto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid username or password" });
        
        var userActivityLog = entityCreationFactory.CreateFromLogin(user, HttpContext);
        await uows.UserActivityLogs.AddAsync(userActivityLog);
        await uows.SaveChangesAsync();
        
        return Ok(new { token = GenerateJwtToken(user) });
    }

    private async Task<UserModel?> GetUserWithPermissionsAsync(string username)
    {
        return await context.Users
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    private static bool IsPasswordValid(string plainTextPassword, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(plainTextPassword, hashedPassword);
    }

    private string GenerateJwtToken(UserModel user)
    {
        var claims = BuildUserClaims(user);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JwtSettings:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["JwtSettings:Issuer"],
            audience: config["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(config["JwtSettings:ExpiresInMinutes"]!)),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static List<Claim> BuildUserClaims(UserModel user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
        };

        var permissions = user.UserPermissions
            .Select(up => up.Permission.PermissionId.ToString())
            .Distinct();

        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        return claims;
    }
}