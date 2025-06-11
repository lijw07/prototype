using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Models;
using Prototype.Services.Interfaces;
using Prototype.Enum;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("[controller]")]
public class LoginController(
    IEntityCreationFactoryService entityCreationFactory,
    IUnitOfWorkService uows,
    IJwtTokenService jwtTokenService,
    SentinelContext context) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto requestDto)
    {
        var user = await GetUserWithPermissionsAsync(requestDto.Username);

        if (user is null || !IsPasswordValid(requestDto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid username or password" });
        
        var userActivityLog = entityCreationFactory.CreateUserActivityLog(user, ActionTypeEnum.Login, HttpContext);
        await uows.UserActivityLogs.AddAsync(userActivityLog);
        await uows.SaveChangesAsync();
        return Ok(new { token = jwtTokenService.BuildUserClaims(user, JwtPurposeTypeEnum.Login) });
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
}