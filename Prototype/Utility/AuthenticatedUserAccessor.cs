using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prototype.DTOs.Cache;
using Prototype.Models;
using Prototype.Services.Interfaces;
using static BCrypt.Net.BCrypt;

namespace Prototype.Utility;

public class AuthenticatedUserAccessor(
    SentinelContext context,
    IMemoryCache cache,
    ICacheService cacheService,
    ICacheInvalidationService cacheInvalidation,
    ILogger<AuthenticatedUserAccessor> logger)
    : IAuthenticatedUserAccessor
{
    private readonly TimeSpan _userCacheExpiry = TimeSpan.FromMinutes(30); // Increased from 5
    private readonly TimeSpan _lookupCacheExpiry = TimeSpan.FromMinutes(15);

    public Task<UserModel?> GetCurrentUserAsync()
    {
        // Implementation would need HttpContext to get current user
        throw new NotImplementedException("Use GetCurrentUserAsync(ClaimsPrincipal user) instead");
    }

    public async Task<UserModel?> GetUserByIdAsync(Guid userId)
    {
        // Try secure cache first
        var cachedUser = await cacheService.GetSecureAsync<UserCacheDto>($"user:profile:{userId}", userId);
        if (cachedUser != null)
        {
            return MapToUserModel(cachedUser);
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        
        if (user != null)
        {
            var userCache = MapToUserCacheDto(user);
            await cacheService.SetSecureAsync($"user:profile:{userId}", userCache, userId, _userCacheExpiry);
        }

        return user;
    }

    public async Task<UserModel?> GetUserByEmailAsync(string email)
    {
        var cacheKey = $"user:email:{email.ToLowerInvariant()}";
        
        // Check memory cache first
        if (cache.TryGetValue(cacheKey, out UserModel? cachedUser))
        {
            logger.LogDebug("Cache hit for user email: {Email}", email);
            return cachedUser;
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        
        if (user != null)
        {
            // Cache by email and by ID
            cache.Set(cacheKey, user, _lookupCacheExpiry);
            cache.Set($"user:{user.UserId}", user, _userCacheExpiry);
            logger.LogDebug("Cached user by email: {Email}", email);
        }

        return user;
    }

    public async Task<UserModel?> GetUserByUsernameAsync(string username)
    {
        var cacheKey = $"user:username:{username.ToLowerInvariant()}";
        
        // Check memory cache first
        if (cache.TryGetValue(cacheKey, out UserModel? cachedUser))
        {
            return cachedUser;
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        
        if (user != null)
        {
            // Cache by username and by ID
            cache.Set(cacheKey, user, _lookupCacheExpiry);
            cache.Set($"user:{user.UserId}", user, _userCacheExpiry);
        }

        return user;
    }

    public async Task<UserModel?> GetCurrentUserAsync(ClaimsPrincipal? user)
    {
        if (user == null || !(user.Identity?.IsAuthenticated ?? false))
            return null;

        var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return null;

        // Use secure cache for current user context
        var cachedUser = await cacheService.GetSecureAsync<UserCacheDto>($"user:current:{userId}", userId);
        if (cachedUser != null)
        {
            return MapToUserModel(cachedUser);
        }

        var userFromDb = await context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        
        if (userFromDb != null)
        {
            var userCache = MapToUserCacheDto(userFromDb);
            await cacheService.SetSecureAsync($"user:current:{userId}", userCache, userId, _userCacheExpiry);
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
        // Use the enhanced GetUserByIdAsync method which has better caching
        return await GetUserByIdAsync(userId);
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

    // Cache invalidation method
    public async Task InvalidateUserCacheAsync(Guid userId, string? email = null, string? username = null)
    {
        await cacheInvalidation.InvalidateUserCacheAsync(userId, email, username);
        
        // Also invalidate local memory cache
        cache.Remove($"user:{userId}");
        
        if (!string.IsNullOrEmpty(email))
        {
            cache.Remove($"user:email:{email.ToLowerInvariant()}");
        }
        
        if (!string.IsNullOrEmpty(username))
        {
            cache.Remove($"user:username:{username.ToLowerInvariant()}");
        }
        
        logger.LogDebug("Invalidated cache for user: {UserId}", userId);
    }

    // Mapping methods
    private UserCacheDto MapToUserCacheDto(UserModel user)
    {
        return new UserCacheDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            IsActive = user.IsActive,
            LastLoginDate = user.LastLoginDate,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    private UserModel MapToUserModel(UserCacheDto cache)
    {
        return new UserModel
        {
            UserId = cache.UserId,
            Username = cache.Username,
            Email = cache.Email,
            FirstName = cache.FirstName,
            LastName = cache.LastName,
            Role = cache.Role,
            IsActive = cache.IsActive,
            LastLoginDate = cache.LastLoginDate,
            CreatedAt = cache.CreatedAt,
            UpdatedAt = cache.UpdatedAt,
            // Don't populate sensitive fields from cache
            PasswordHash = string.Empty,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
    }
}