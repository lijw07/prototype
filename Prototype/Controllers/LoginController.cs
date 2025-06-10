using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers;

[ApiController]
[Route("[controller]")]
public class LoginController(
    IEntityCreationFactoryService entityCreationFactory,
    IEntitySaveService<UserActivityLogModel> userService,
    SentinelContext context) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto requestDto)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Username == requestDto.Username);
        
        if (user == null || !BCrypt.Net.BCrypt.Verify(requestDto.Password, user.PasswordHash))
        {
            return Unauthorized(new {message = "Invalid username or password"});
        }
        
        var userActivityLog = entityCreationFactory.CreateUserActivityLogFromLogin(user, HttpContext);
        await userService.CreateAsync(userActivityLog);
        return Ok(new {message = "Login Successful"});
    }
}