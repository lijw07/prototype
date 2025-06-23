using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Prototype.Controllers;
using Prototype.Data;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Utility;
using System.Security.Claims;
using Xunit;

namespace Prototype.Tests.Controllers;

public class DashboardControllerTest : IDisposable
{
    private readonly SentinelContext _context;
    private readonly Mock<IAuthenticatedUserAccessor> _mockUserAccessor;
    private readonly Mock<ILogger<DashboardController>> _mockLogger;
    private readonly DashboardController _controller;
    private readonly UserModel _testUser;

    public DashboardControllerTest()
    {
        var options = new DbContextOptionsBuilder<SentinelContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new SentinelContext(options);

        _mockUserAccessor = new Mock<IAuthenticatedUserAccessor>();
        _mockLogger = new Mock<ILogger<DashboardController>>();
        _controller = new DashboardController(_context, _mockUserAccessor.Object, _mockLogger.Object);

        // Setup test user
        _testUser = new UserModel
        {
            UserId = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "555-0123",
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true,
            PasswordHash = "hashed_password"
        };

        // Setup ClaimsPrincipal for the controller
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testUser.UserId.ToString()),
            new Claim(ClaimTypes.Name, _testUser.Username),
            new Claim(ClaimTypes.Email, _testUser.Email)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetDashboardStatistics_WithAuthenticatedUser_ReturnsStatistics()
    {
        // Arrange
        await SeedTestData();

        _mockUserAccessor.Setup(x => x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_testUser);

        // Act
        var result = await _controller.GetDashboardStatistics();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as dynamic;
        Assert.NotNull(response);
        Assert.True(response!.success);
        Assert.NotNull(response.data);

        _mockUserAccessor.Verify(x => x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    [Fact]
    public async Task GetDashboardStatistics_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        _mockUserAccessor.Setup(x => x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((UserModel?)null);

        // Act
        var result = await _controller.GetDashboardStatistics();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = unauthorizedResult.Value as dynamic;
        Assert.NotNull(response);
        Assert.False(response!.success);
        Assert.Equal("User not authenticated", response.message);
    }

    [Fact]
    public async Task GetDashboardStatistics_WithUserApplications_ReturnsCorrectApplicationCount()
    {
        // Arrange
        await SeedTestDataWithApplications();

        _mockUserAccessor.Setup(x => x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_testUser);

        // Act
        var result = await _controller.GetDashboardStatistics();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as dynamic;
        Assert.NotNull(response);
        Assert.Equal(2, response!.data.totalApplications); // Based on seeded data
    }

    [Fact]
    public async Task GetDashboardStatistics_WithUserActivity_ReturnsRecentActivityCount()
    {
        // Arrange
        await SeedTestDataWithActivity();

        _mockUserAccessor.Setup(x => x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_testUser);

        // Act
        var result = await _controller.GetDashboardStatistics();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as dynamic;
        Assert.NotNull(response);
        Assert.True(response!.data.recentActivity >= 0);
        Assert.NotNull(response.data.recentActivities);
    }

    [Fact]
    public async Task GetDashboardStatistics_WithRoles_ReturnsCorrectRoleCount()
    {
        // Arrange
        await SeedTestDataWithRoles();

        _mockUserAccessor.Setup(x => x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_testUser);

        // Act
        var result = await _controller.GetDashboardStatistics();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as dynamic;
        Assert.NotNull(response);
        Assert.Equal(3, response!.data.totalRoles); // Based on seeded data
    }

    [Fact]
    public async Task GetDashboardStatistics_WithUsers_ReturnsCorrectUserCounts()
    {
        // Arrange
        await SeedTestDataWithUsers();

        _mockUserAccessor.Setup(x => x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_testUser);

        // Act
        var result = await _controller.GetDashboardStatistics();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as dynamic;
        Assert.NotNull(response);
        Assert.Equal(2, response!.data.totalVerifiedUsers); // Including test user
        Assert.Equal(1, response.data.totalTemporaryUsers);
        Assert.Equal(3, response.data.totalUsers);
    }

    [Fact]
    public async Task GetDashboardStatistics_WhenExceptionOccurs_ReturnsInternalServerError()
    {
        // Arrange
        _mockUserAccessor.Setup(x => x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _controller.GetDashboardStatistics();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        
        var response = statusCodeResult.Value as dynamic;
        Assert.NotNull(response);
        Assert.False(response!.success);
        Assert.Equal("Internal server error", response.message);

        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error retrieving dashboard statistics")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetDashboardStatistics_IncludesSystemHealthStatus()
    {
        // Arrange
        await SeedTestData();

        _mockUserAccessor.Setup(x => x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_testUser);

        // Act
        var result = await _controller.GetDashboardStatistics();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as dynamic;
        Assert.NotNull(response);
        Assert.Equal("healthy", response!.data.systemHealth);
    }

    [Fact]
    public async Task GetDashboardStatistics_LogsSuccessfulRetrieval()
    {
        // Arrange
        await SeedTestData();

        _mockUserAccessor.Setup(x => x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_testUser);

        // Act
        var result = await _controller.GetDashboardStatistics();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Dashboard statistics retrieved for user")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private async Task SeedTestData()
    {
        _context.Users.Add(_testUser);
        await _context.SaveChangesAsync();
    }

    private async Task SeedTestDataWithApplications()
    {
        _context.Users.Add(_testUser);

        var app1 = new ApplicationModel
        {
            ApplicationId = Guid.NewGuid(),
            ApplicationName = "Test App 1",
            ApplicationDataSourceType = DataSourceTypeEnum.MicrosoftSqlServer,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var app2 = new ApplicationModel
        {
            ApplicationId = Guid.NewGuid(),
            ApplicationName = "Test App 2",
            ApplicationDataSourceType = DataSourceTypeEnum.MicrosoftSqlServer,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Applications.AddRange(app1, app2);

        var userApp1 = new UserApplicationModel
        {
            UserApplicationId = Guid.NewGuid(),
            UserId = _testUser.UserId,
            User = _testUser,
            ApplicationId = app1.ApplicationId,
            Application = app1,
            ApplicationConnectionId = Guid.NewGuid(),
            ApplicationConnection = null!,
            CreatedAt = DateTime.UtcNow
        };

        var userApp2 = new UserApplicationModel
        {
            UserApplicationId = Guid.NewGuid(),
            UserId = _testUser.UserId,
            User = _testUser,
            ApplicationId = app2.ApplicationId,
            Application = app2,
            ApplicationConnectionId = Guid.NewGuid(),
            ApplicationConnection = null!,
            CreatedAt = DateTime.UtcNow
        };

        _context.UserApplications.AddRange(userApp1, userApp2);
        await _context.SaveChangesAsync();
    }

    private async Task SeedTestDataWithActivity()
    {
        _context.Users.Add(_testUser);

        var activity1 = new UserActivityLogModel
        {
            UserActivityLogId = Guid.NewGuid(),
            UserId = _testUser.UserId,
            ActionType = ActionTypeEnum.Login,
            Description = "User logged in",
            DeviceInformation = "Test Device",
            Timestamp = DateTime.UtcNow.AddHours(-1),
            IpAddress = "192.168.1.1"
        };

        var activity2 = new UserActivityLogModel
        {
            UserActivityLogId = Guid.NewGuid(),
            UserId = _testUser.UserId,
            ActionType = ActionTypeEnum.ApplicationAdded,
            Description = "Application added",
            DeviceInformation = "Test Device",
            Timestamp = DateTime.UtcNow.AddHours(-2),
            IpAddress = "192.168.1.1"
        };

        _context.UserActivityLogs.AddRange(activity1, activity2);
        await _context.SaveChangesAsync();
    }

    private async Task SeedTestDataWithRoles()
    {
        _context.Users.Add(_testUser);

        var roles = new[]
        {
            new UserRoleModel
            {
                UserRoleId = Guid.NewGuid(),
                Role = "Admin",
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow
            },
            new UserRoleModel
            {
                UserRoleId = Guid.NewGuid(),
                Role = "User",
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow
            },
            new UserRoleModel
            {
                UserRoleId = Guid.NewGuid(),
                Role = "Manager",
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow
            }
        };

        _context.UserRoles.AddRange(roles);
        await _context.SaveChangesAsync();
    }

    private async Task SeedTestDataWithUsers()
    {
        _context.Users.Add(_testUser);

        var user2 = new UserModel
        {
            UserId = Guid.NewGuid(),
            Username = "testuser2",
            Email = "test2@example.com",
            FirstName = "Test2",
            LastName = "User2",
            PhoneNumber = "555-0124",
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true,
            PasswordHash = "hashed_password2"
        };

        _context.Users.Add(user2);

        var tempUser = new TemporaryUserModel
        {
            TemporaryUserId = Guid.NewGuid(),
            Username = "tempuser",
            Email = "temp@example.com",
            FirstName = "Temp",
            LastName = "User",
            PasswordHash = "temp_password_hash",
            PhoneNumber = "555-0125",
            Token = "verification-token",
            CreatedAt = DateTime.UtcNow
        };

        _context.TemporaryUsers.Add(tempUser);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}