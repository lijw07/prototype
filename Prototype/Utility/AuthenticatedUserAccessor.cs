using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prototype.Data;
using Prototype.Models;
using static BCrypt.Net.BCrypt;

namespace Prototype.Utility;

public class AuthenticatedUserAccessor : IAuthenticatedUserAccessor
{
    private readonly SentinelContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AuthenticatedUserAccessor> _logger;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

    public AuthenticatedUserAccessor(
        SentinelContext context, 
        IMemoryCache cache, 
        ILogger<AuthenticatedUserAccessor> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public Task<UserModel?> GetCurrentUserAsync()
    {
        // Implementation would need HttpContext to get current user
        throw new NotImplementedException("Use GetCurrentUserAsync(ClaimsPrincipal user) instead");
    }

    public async Task<UserModel?> GetUserByIdAsync(Guid userId)
    {
        var cacheKey = $"user:{userId}";
        if (_cache.TryGetValue(cacheKey, out UserModel? cachedUser))
            return cachedUser;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        
        if (user != null)
        {
            _cache.Set(cacheKey, user, _cacheExpiry);
        }

        return user;
    }

    public async Task<UserModel?> GetUserByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<UserModel?> GetUserByUsernameAsync(string username)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<UserModel?> GetCurrentUserAsync(ClaimsPrincipal? user)
    {
        if (user == null || !(user.Identity?.IsAuthenticated ?? false))
            return null;

        var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return null;

        var cacheKey = $"user:{userId}";
        if (_cache.TryGetValue(cacheKey, out UserModel? cachedUser))
            return cachedUser;

        var userFromDb = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        
        if (userFromDb != null)
        {
            _cache.Set(cacheKey, userFromDb, _cacheExpiry);
        }

        return userFromDb;
    }

    public async Task<UserModel?> GetUser(string username, string password)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            _logger.LogWarning("User not found: {Username}", username);
            return null;
        }

        var isPasswordValid = Verify(password, user.PasswordHash);
        return isPasswordValid ? user : null;
    }
    
    public async Task<bool> ValidateUser(string username, string password)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Select(u => new { u.Username, u.PasswordHash })
            .FirstOrDefaultAsync(u => u.Username == username);
            
        if (user == null)
            return false;

        return Verify(password, user.PasswordHash);
    }
    
    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _context.Users.AnyAsync(u => u.Username == username);
    }
    
    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<TemporaryUserModel?> FindTemporaryUserByEmail(string email)
    {
        return await _context.TemporaryUsers.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<UserModel?> FindUserByEmail(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<UserModel?> FindUserById(Guid userId)
    {
        var cacheKey = $"user:{userId}";
        if (_cache.TryGetValue(cacheKey, out UserModel? cachedUser))
            return cachedUser;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        
        if (user != null)
        {
            _cache.Set(cacheKey, user, _cacheExpiry);
        }

        return user;
    }

    public async Task<UserRecoveryRequestModel?> FindUserRecoveryRequest(Guid userId)
    {
        return await _context.UserRecoveryRequests
            .Where(u => u.UserId == userId && !u.IsUsed && u.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(u => u.RequestedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> TemporaryEmailExistsAsync(string email)
    {
        return await _context.TemporaryUsers.AnyAsync(u => u.Email == email);
    }

    public async Task<bool> TemporaryUsernameExistsAsync(string username)
    {
        return await _context.TemporaryUsers.AnyAsync(u => u.Username == username);
    }
}