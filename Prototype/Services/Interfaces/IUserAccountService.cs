using Prototype.DTOs;
using Prototype.DTOs.Responses;
using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Services.Interfaces;

public interface IUserAccountService
{
    Task<UserModel?> GetUserByEmailAsync(string email);
    Task<UserModel?> GetUserByUsernameAsync(string username);
    Task<UserModel?> GetUserByIdAsync(Guid userId);
    Task<List<UserModel>> GetAllUsersAsync();
    Task<LoginResponse> RegisterTemporaryUserAsync(RegisterRequestDto request);
    Task<LoginResponse> ForgotPasswordAsync(ForgotUserRequestDto request);
    Task<LoginResponse> ResetPasswordAsync(ResetPasswordRequestDto request);
    Task<LoginResponse> RegisterNewUser(string token);
    Task<LoginResponse> UpdateUserAsync(UpdateUserRequestDto request);
    Task<LoginResponse> DeleteUserAsync(Guid userId);
    Task CreateAuditLogAsync(Guid currentUserUserId, ActionTypeEnum update, string metadata);
    Task CreateUserActivityLogAsync(Guid currentUserUserId, ActionTypeEnum update, string userUpdateRequest);
}