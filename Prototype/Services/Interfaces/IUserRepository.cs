using Prototype.Models;

namespace Prototype.Services.Interfaces;

public interface IUserRepository
{
    Task<UserModel?> GetUserByEmailAsync(string email);
    Task<UserModel?> GetUserByUsernameAsync(string username);
    Task<UserModel?> GetUserByIdAsync(Guid userId);
    Task<List<UserModel>> GetAllUsersAsync();
    Task<UserModel> CreateUserAsync(UserModel user);
    Task<UserModel> UpdateUserAsync(UserModel user);
    Task DeleteUserAsync(Guid userId);
    Task<bool> UserExistsAsync(string email);
    Task<bool> UsernameExistsAsync(string username);
}