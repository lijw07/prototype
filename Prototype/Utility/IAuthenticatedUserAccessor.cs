using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.Models;

namespace Prototype.Utility;

public interface IAuthenticatedUserAccessor
{
    Task<UserModel?> GetUserFromTokenAsync(ClaimsPrincipal user);
    Task<bool> ValidateUser(string username, string password);
    Task<UserModel?> GetUser(string username, string password);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task<TemporaryUserModel?> FindTemporaryUserByEmail(string email);
    Task<UserModel?> FindUserByEmail(string email);
    Task<UserRecoveryRequestModel?> FindUserRecoveryRequest(Guid userId);
}