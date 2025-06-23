using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.Models;
using Prototype.Services;
using Xunit;

namespace Prototype.Tests.Services;

public class UserRoleServiceTest : IDisposable
{
    private readonly SentinelContext _context;
    private readonly UserRoleService _userRoleService;

    public UserRoleServiceTest()
    {
        var options = new DbContextOptionsBuilder<SentinelContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new SentinelContext(options);
        _userRoleService = new UserRoleService(_context);
    }

    [Fact]
    public async Task GetAllRolesAsync_ReturnsAllRoles()
    {
        // Arrange
        var roles = new[]
        {
            new UserRoleModel
            {
                UserRoleId = Guid.NewGuid(),
                Role = "Admin",
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new UserRoleModel
            {
                UserRoleId = Guid.NewGuid(),
                Role = "User",
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new UserRoleModel
            {
                UserRoleId = Guid.NewGuid(),
                Role = "Manager",
                CreatedBy = "admin",
                CreatedAt = DateTime.UtcNow
            }
        };

        _context.UserRoles.AddRange(roles);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userRoleService.GetAllRolesAsync();

        // Assert
        Assert.Equal(3, result.Count());
        
        // Verify ordering (newest first)
        var orderedResult = result.ToList();
        Assert.Equal("Manager", orderedResult[0].Role);
        Assert.Equal("User", orderedResult[1].Role);
        Assert.Equal("Admin", orderedResult[2].Role);
    }

    [Fact]
    public async Task GetRoleByIdAsync_WithExistingRole_ReturnsRole()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var role = new UserRoleModel
        {
            UserRoleId = roleId,
            Role = "TestRole",
            CreatedBy = "testuser",
            CreatedAt = DateTime.UtcNow
        };

        _context.UserRoles.Add(role);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userRoleService.GetRoleByIdAsync(roleId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(roleId, result.UserRoleId);
        Assert.Equal("TestRole", result.Role);
        Assert.Equal("testuser", result.CreatedBy);
    }

    [Fact]
    public async Task GetRoleByIdAsync_WithNonExistentRole_ReturnsNull()
    {
        // Arrange
        var nonExistentRoleId = Guid.NewGuid();

        // Act
        var result = await _userRoleService.GetRoleByIdAsync(nonExistentRoleId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateRoleAsync_WithValidData_CreatesAndReturnsRole()
    {
        // Arrange
        var roleName = "NewRole";
        var createdBy = "testuser";

        // Act
        var result = await _userRoleService.CreateRoleAsync(roleName, createdBy);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(roleName, result.Role);
        Assert.Equal(createdBy, result.CreatedBy);
        Assert.True(result.UserRoleId != Guid.Empty);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);

        // Verify role was saved to database
        var savedRole = await _context.UserRoles.FindAsync(result.UserRoleId);
        Assert.NotNull(savedRole);
        Assert.Equal(roleName, savedRole.Role);
    }

    [Fact]
    public async Task CreateRoleWithoutSaveAsync_WithValidData_CreatesRoleWithoutSaving()
    {
        // Arrange
        var roleName = "TempRole";
        var createdBy = "testuser";

        // Act
        var result = await _userRoleService.CreateRoleWithoutSaveAsync(roleName, createdBy);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(roleName, result.Role);
        Assert.Equal(createdBy, result.CreatedBy);
        Assert.True(result.UserRoleId != Guid.Empty);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task UpdateRoleAsync_WithExistingRole_UpdatesAndReturnsRole()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var originalRole = new UserRoleModel
        {
            UserRoleId = roleId,
            Role = "OriginalRole",
            CreatedBy = "creator",
            CreatedAt = DateTime.UtcNow
        };

        _context.UserRoles.Add(originalRole);
        await _context.SaveChangesAsync();

        var newRoleName = "UpdatedRole";

        // Act
        var result = await _userRoleService.UpdateRoleAsync(roleId, newRoleName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(roleId, result.UserRoleId);
        Assert.Equal(newRoleName, result.Role);

        // Verify role was updated in database
        var updatedRole = await _context.UserRoles.FindAsync(roleId);
        Assert.NotNull(updatedRole);
        Assert.Equal(newRoleName, updatedRole.Role);
    }

    [Fact]
    public async Task UpdateRoleAsync_WithNonExistentRole_ReturnsNull()
    {
        // Arrange
        var nonExistentRoleId = Guid.NewGuid();
        var newRoleName = "UpdatedRole";

        // Act
        var result = await _userRoleService.UpdateRoleAsync(nonExistentRoleId, newRoleName);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteRoleAsync_WithExistingRole_DeletesRoleAndReturnsTrue()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var role = new UserRoleModel
        {
            UserRoleId = roleId,
            Role = "ToDeleteRole",
            CreatedBy = "creator",
            CreatedAt = DateTime.UtcNow
        };

        _context.UserRoles.Add(role);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userRoleService.DeleteRoleAsync(roleId);

        // Assert
        Assert.True(result);

        // Verify role was deleted from database
        var deletedRole = await _context.UserRoles.FindAsync(roleId);
        Assert.Null(deletedRole);
    }

    [Fact]
    public async Task DeleteRoleAsync_WithNonExistentRole_ReturnsFalse()
    {
        // Arrange
        var nonExistentRoleId = Guid.NewGuid();

        // Act
        var result = await _userRoleService.DeleteRoleAsync(nonExistentRoleId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RoleExistsAsync_WithExistingRole_ReturnsTrue()
    {
        // Arrange
        var roleName = "ExistingRole";
        var role = new UserRoleModel
        {
            UserRoleId = Guid.NewGuid(),
            Role = roleName,
            CreatedBy = "creator",
            CreatedAt = DateTime.UtcNow
        };

        _context.UserRoles.Add(role);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userRoleService.RoleExistsAsync(roleName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RoleExistsAsync_WithNonExistentRole_ReturnsFalse()
    {
        // Arrange
        var nonExistentRoleName = "NonExistentRole";

        // Act
        var result = await _userRoleService.RoleExistsAsync(nonExistentRoleName);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RoleExistsAsync_IsCaseInsensitive()
    {
        // Arrange
        var roleName = "TestRole";
        var role = new UserRoleModel
        {
            UserRoleId = Guid.NewGuid(),
            Role = roleName,
            CreatedBy = "creator",
            CreatedAt = DateTime.UtcNow
        };

        _context.UserRoles.Add(role);
        await _context.SaveChangesAsync();

        // Act & Assert
        Assert.True(await _userRoleService.RoleExistsAsync("testrole"));
        Assert.True(await _userRoleService.RoleExistsAsync("TESTROLE"));
        Assert.True(await _userRoleService.RoleExistsAsync("TestRole"));
    }

    [Fact]
    public async Task CreateRoleAsync_WithEmptyRoleName_CreatesRoleSuccessfully()
    {
        // Arrange
        var emptyRoleName = "";
        var createdBy = "testuser";

        // Act
        var result = await _userRoleService.CreateRoleAsync(emptyRoleName, createdBy);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("", result.Role);
        Assert.Equal("testuser", result.CreatedBy);
    }

    [Fact]
    public async Task CreateRoleAsync_WithNullCreatedBy_ThrowsDbUpdateException()
    {
        // Arrange
        var roleName = "TestRole";
        string nullCreatedBy = null!;

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            _userRoleService.CreateRoleAsync(roleName, nullCreatedBy));
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}