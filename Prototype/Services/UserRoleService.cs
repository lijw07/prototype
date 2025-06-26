using Microsoft.EntityFrameworkCore;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class UserRoleService(SentinelContext context) : IUserRoleService
{
    public async Task<IEnumerable<UserRoleModel>> GetAllRolesAsync()
    {
        return await context.UserRoles
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<UserRoleModel?> GetRoleByIdAsync(Guid roleId)
    {
        return await context.UserRoles
            .FirstOrDefaultAsync(r => r.UserRoleId == roleId);
    }

    public async Task<UserRoleModel> CreateRoleAsync(string roleName, string createdBy)
    {
        var role = await CreateRoleWithoutSaveAsync(roleName, createdBy);
        await context.SaveChangesAsync();
        return role;
    }
    
    public Task<UserRoleModel> CreateRoleWithoutSaveAsync(string roleName, string createdBy)
    {
        var role = new UserRoleModel
        {
            UserRoleId = Guid.NewGuid(),
            RoleName = roleName,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        context.UserRoles.Add(role);
        return Task.FromResult(role);
    }

    public async Task<UserRoleModel?> UpdateRoleAsync(Guid roleId, string roleName)
    {
        var role = await UpdateRoleWithoutSaveAsync(roleId, roleName);
        if (role != null)
            await context.SaveChangesAsync();
        return role;
    }
    
    public async Task<UserRoleModel?> UpdateRoleWithoutSaveAsync(Guid roleId, string roleName)
    {
        var role = await context.UserRoles
            .FirstOrDefaultAsync(r => r.UserRoleId == roleId);

        if (role == null)
            return null;

        role.RoleName = roleName;
        return role;
    }

    public async Task<bool> DeleteRoleAsync(Guid roleId)
    {
        var deleted = await DeleteRoleWithoutSaveAsync(roleId);
        if (deleted)
            await context.SaveChangesAsync();
        return deleted;
    }
    
    public async Task<bool> DeleteRoleWithoutSaveAsync(Guid roleId)
    {
        var role = await context.UserRoles
            .FirstOrDefaultAsync(r => r.UserRoleId == roleId);

        if (role == null)
            return false;

        context.UserRoles.Remove(role);
        return true;
    }

    public async Task<bool> RoleExistsAsync(string roleName)
    {
        return await context.UserRoles
            .AnyAsync(r => r.RoleName.ToLower() == roleName.ToLower());
    }
}