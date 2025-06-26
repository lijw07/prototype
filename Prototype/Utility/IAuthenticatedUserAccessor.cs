using System.Security.Claims;
using Prototype.Models;

namespace Prototype.Utility;

public interface IAuthenticatedUserAccessor
{
    Task<UserModel?> GetUserByIdAsync(Guid userId);
    Task<UserModel?> GetUserByEmailAsync(string email);
    Task<UserModel?> GetUserByUsernameAsync(string username);
    Task<UserModel?> GetCurrentUserAsync(ClaimsPrincipal? user);
    Task<bool> ValidateUser(string username, string password);
    Task<UserModel?> GetUser(string username, string password);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task<TemporaryUserModel?> FindTemporaryUserByEmail(string email);
    Task<UserModel?> FindUserByEmail(string email);
    Task<UserModel?> FindUserById(Guid userId);
    Task<UserRecoveryRequestModel?> FindUserRecoveryRequest(Guid userId);
    Task<bool> TemporaryEmailExistsAsync(string requestDtoEmail);
    Task<bool> TemporaryUsernameExistsAsync(string requestDtoUsername);
}