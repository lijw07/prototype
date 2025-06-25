using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prototype.Data;
using Prototype.Models;
using static BCrypt.Net.BCrypt;

namespace Prototype.Utility;

public class AuthenticatedUserAccessor(
    SentinelContext context,
    IMemoryCache cache,
    ILogger<AuthenticatedUserAccessor> logger)
    : IAuthenticatedUserAccessor
{
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

    public Task<UserModel?> GetCurrentUserAsync()
    {
        // Implementation would need HttpContext to get current user
        throw new NotImplementedException("Use GetCurrentUserAsync(ClaimsPrincipal user) instead");
    }

    public async Task<UserModel?> GetUserByIdAsync(Guid userId)
    {
        var cacheKey = $"user:{userId}";
        if (cache.TryGetValue(cacheKey, out UserModel? cachedUser))
            return cachedUser;

        var user = await context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        
        if (user != null)
        {
            cache.Set(cacheKey, user, _cacheExpiry);
        }

        return user;
    }

    public async Task<UserModel?> GetUserByEmailAsync(string email)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<UserModel?> GetUserByUsernameAsync(string username)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<UserModel?> GetCurrentUserAsync(ClaimsPrincipal? user)
    {
        if (user == null || !(user.Identity?.IsAuthenticated ?? false))
            return null;

        var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return null;

        var cacheKey = $"user:{userId}";
        if (cache.TryGetValue(cacheKey, out UserModel? cachedUser))
            return cachedUser;

        var userFromDb = await context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        
        if (userFromDb != null)
        {
            cache.Set(cacheKey, userFromDb, _cacheExpiry);
        }

        return userFromDb;
    }

    public async Task<UserModel?> GetUser(string username, string password)
    {
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            logger.LogWarning("User not found: {Username}", username);
            return null;
        }

        var isPasswordValid = Verify(password, user.PasswordHash);
        return isPasswordValid ? user : null;
    }
    
    public async Task<bool> ValidateUser(string username, string password)
    {
        var user = await context.Users
            .AsNoTracking()
            .Select(u => new { u.Username, u.PasswordHash })
            .FirstOrDefaultAsync(u => u.Username == username);
            
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
        var cacheKey = $"user:{userId}";
        if (cache.TryGetValue(cacheKey, out UserModel? cachedUser))
            return cachedUser;

        var user = await context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        
        if (user != null)
        {
            cache.Set(cacheKey, user, _cacheExpiry);
        }

        return user;
    }

    public async Task<UserRecoveryRequestModel?> FindUserRecoveryRequest(Guid userId)
    {
        return await context.UserRecoveryRequests
            .Where(u => u.UserId == userId && !u.IsUsed && u.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(u => u.RequestedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> TemporaryEmailExistsAsync(string email)
    {
        return await context.TemporaryUsers.AnyAsync(u => u.Email == email);
    }

    public async Task<bool> TemporaryUsernameExistsAsync(string username)
    {
        return await context.TemporaryUsers.AnyAsync(u => u.Username == username);
    }
}