using Microsoft.EntityFrameworkCore;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class UserRepository(
    SentinelContext context,
    ILogger<UserRepository> logger) : IUserRepository
{
    public async Task<UserModel?> GetUserByEmailAsync(string email)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<UserModel?> GetUserByUsernameAsync(string username)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<UserModel?> GetUserByIdAsync(Guid userId)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<List<UserModel>> GetAllUsersAsync()
    {
        return await context.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
    }

    public async Task<UserModel> CreateUserAsync(UserModel user)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync();
        logger.LogInformation("User created successfully with ID {UserId}", user.UserId);
        return user;
    }

    public async Task<UserModel> UpdateUserAsync(UserModel user)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync();
        logger.LogInformation("User updated successfully with ID {UserId}", user.UserId);
        return user;
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user != null)
        {
            context.Users.Remove(user);
            await context.SaveChangesAsync();
            logger.LogInformation("User deleted successfully with ID {UserId}", userId);
        }
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        return await context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await context.Users.AnyAsync(u => u.Username == username);
    }
}