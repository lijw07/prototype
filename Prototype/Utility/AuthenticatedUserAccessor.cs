using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.Models;
using static BCrypt.Net.BCrypt;

namespace Prototype.Utility;

public class AuthenticatedUserAccessor(SentinelContext context) : IAuthenticatedUserAccessor
{
    public async Task<UserModel?> GetCurrentUserAsync(ClaimsPrincipal? user)
    {
        if (user == null || !(user.Identity?.IsAuthenticated ?? false))
            return null;

        var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return null;

        return await context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<UserModel?> GetUser(string username, string password)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return null;

        var isPasswordValid = Verify(password, user.PasswordHash);

        return isPasswordValid ? user : null;
    }
    
    public async Task<bool> ValidateUser(string username, string password)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
            return false;

        return Verify(password, user.PasswordHash);
    }
    
    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await context.Users.AnyAsync(u => u.Username == username);
    }
    
    public async Task<bool> EmailExistsAsync(string email)
    {
        return await context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<TemporaryUserModel?> FindTemporaryUserByEmail(string email)
    {
        return await context.TemporaryUsers.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<UserModel?> FindUserByEmail(string email)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<UserModel?> FindUserById(Guid userId)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<UserRecoveryRequestModel?> FindUserRecoveryRequest(Guid userId)
    {
        return await context.UserRecoveryRequests
            .Where(u => u.UserId == userId && !u.IsUsed && u.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(u => u.RequestedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> TemporaryEmailExistsAsync(string requestDtoEmail)
    {
        return await context.TemporaryUsers.AnyAsync(u => u.Email == requestDtoEmail);
    }

    public async Task<bool> TemporaryUsernameExistsAsync(string requestDtoUsername)
    {
        return await context.TemporaryUsers.AnyAsync(u => u.Username == requestDtoUsername);
    }
}