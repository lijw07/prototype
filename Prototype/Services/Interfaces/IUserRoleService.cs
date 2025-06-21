using Prototype.Models;

namespace Prototype.Services.Interfaces;

public interface IUserRoleService
{
    Task<IEnumerable<UserRoleModel>> GetAllRolesAsync();
    Task<UserRoleModel?> GetRoleByIdAsync(Guid roleId);
    Task<UserRoleModel> CreateRoleAsync(string roleName, string createdBy);
    Task<UserRoleModel> CreateRoleWithoutSaveAsync(string roleName, string createdBy);
    Task<UserRoleModel?> UpdateRoleAsync(Guid roleId, string roleName);
    Task<UserRoleModel?> UpdateRoleWithoutSaveAsync(Guid roleId, string roleName);
    Task<bool> DeleteRoleAsync(Guid roleId);
    Task<bool> DeleteRoleWithoutSaveAsync(Guid roleId);
    Task<bool> RoleExistsAsync(string roleName);
}