using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class UserRoleService : IUserRoleService
{
    private readonly SentinelContext _context;

    public UserRoleService(SentinelContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<UserRoleModel>> GetAllRolesAsync()
    {
        return await _context.UserRoles
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<UserRoleModel?> GetRoleByIdAsync(Guid roleId)
    {
        return await _context.UserRoles
            .FirstOrDefaultAsync(r => r.UserRoleId == roleId);
    }

    public async Task<UserRoleModel> CreateRoleAsync(string roleName, string createdBy)
    {
        var role = await CreateRoleWithoutSaveAsync(roleName, createdBy);
        await _context.SaveChangesAsync();
        return role;
    }
    
    public Task<UserRoleModel> CreateRoleWithoutSaveAsync(string roleName, string createdBy)
    {
        var role = new UserRoleModel
        {
            UserRoleId = Guid.NewGuid(),
            Role = roleName,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        _context.UserRoles.Add(role);
        return Task.FromResult(role);
    }

    public async Task<UserRoleModel?> UpdateRoleAsync(Guid roleId, string roleName)
    {
        var role = await UpdateRoleWithoutSaveAsync(roleId, roleName);
        if (role != null)
            await _context.SaveChangesAsync();
        return role;
    }
    
    public async Task<UserRoleModel?> UpdateRoleWithoutSaveAsync(Guid roleId, string roleName)
    {
        var role = await _context.UserRoles
            .FirstOrDefaultAsync(r => r.UserRoleId == roleId);

        if (role == null)
            return null;

        role.Role = roleName;
        return role;
    }

    public async Task<bool> DeleteRoleAsync(Guid roleId)
    {
        var deleted = await DeleteRoleWithoutSaveAsync(roleId);
        if (deleted)
            await _context.SaveChangesAsync();
        return deleted;
    }
    
    public async Task<bool> DeleteRoleWithoutSaveAsync(Guid roleId)
    {
        var role = await _context.UserRoles
            .FirstOrDefaultAsync(r => r.UserRoleId == roleId);

        if (role == null)
            return false;

        _context.UserRoles.Remove(role);
        return true;
    }

    public async Task<bool> RoleExistsAsync(string roleName)
    {
        return await _context.UserRoles
            .AnyAsync(r => r.Role.ToLower() == roleName.ToLower());
    }
}