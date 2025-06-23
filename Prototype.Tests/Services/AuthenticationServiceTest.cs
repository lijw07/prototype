using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;
using Prototype.Services.Interfaces;
using Xunit;

namespace Prototype.Tests.Services;

public class AuthenticationServiceTest : IDisposable
{
    private readonly SentinelContext _context;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly PasswordEncryptionService _passwordService;
    private readonly ValidationService _validationService;
    private readonly Mock<ILogger<AuthenticationService>> _mockLogger;
    private readonly AuthenticationService _authenticationService;

    public AuthenticationServiceTest()
    {
        var options = new DbContextOptionsBuilder<SentinelContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new SentinelContext(options);

        _mockJwtTokenService = new Mock<IJwtTokenService>();
        
        // Set environment variable for tests
        Environment.SetEnvironmentVariable("ENCRYPTION_KEY", "test-encryption-key-1234567890123456");
        
        // Create mock configuration with encryption key
        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(x => x["Encryption:Key"]).Returns("test-encryption-key-1234567890123456");
        
        // Use real instances since methods aren't virtual
        _passwordService = new PasswordEncryptionService(mockConfiguration.Object);
        _validationService = new ValidationService();
        _mockLogger = new Mock<ILogger<AuthenticationService>>();

        _authenticationService = new AuthenticationService(
            _context,
            _mockJwtTokenService.Object,
            _passwordService,
            _validationService,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ShouldReturnSuccessfulResponse()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Username = "testuser",
            Password = "Password123!"
        };

        var testUser = new UserModel
        {
            UserId = Guid.NewGuid(),
            Username = "testuser",
            PasswordHash = _passwordService.HashPassword(loginRequest.Password),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "123-456-7890",
            IsActive = true,
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();

        var expectedToken = "generated-jwt-token";
        _mockJwtTokenService.Setup(x => x.BuildUserClaims(testUser, ActionTypeEnum.Login))
            .Returns(expectedToken);

        // Act
        var result = await _authenticationService.AuthenticateAsync(loginRequest);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Login successful", result.Message);
        Assert.Equal(expectedToken, result.Token);

        // Verify user activity log was created
        var activityLog = await _context.UserActivityLogs
            .FirstOrDefaultAsync(log => log.UserId == testUser.UserId);
        Assert.NotNull(activityLog);
        Assert.Equal(ActionTypeEnum.Login, activityLog.ActionType);
        Assert.Equal("User logged in", activityLog.Description);

        // Verify last login was updated
        var updatedUser = await _context.Users.FindAsync(testUser.UserId);
        Assert.NotNull(updatedUser.LastLogin);
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidCredentials_ShouldReturnFailureResponse()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Username = "testuser",
            Password = "wrongpassword"
        };

        var testUser = new UserModel
        {
            UserId = Guid.NewGuid(),
            Username = "testuser",
            PasswordHash = _passwordService.HashPassword("correctpassword"),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "123-456-7890",
            IsActive = true,
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authenticationService.AuthenticateAsync(loginRequest);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid username or password", result.Message);
        Assert.Empty(result.Token);

        // Verify no activity log was created
        var activityLog = await _context.UserActivityLogs
            .FirstOrDefaultAsync(log => log.UserId == testUser.UserId);
        Assert.Null(activityLog);
    }

    [Fact]
    public async Task AuthenticateAsync_WithNonExistentUser_ShouldReturnFailureResponse()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Username = "nonexistentuser",
            Password = "Password123!"
        };

        // Act
        var result = await _authenticationService.AuthenticateAsync(loginRequest);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid username or password", result.Message);
        Assert.Empty(result.Token);
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidationFailure_ShouldReturnValidationError()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Username = "",
            Password = ""
        };

        // Act
        var result = await _authenticationService.AuthenticateAsync(loginRequest);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Validation failed", result.Message);
        Assert.Empty(result.Token);
    }

    [Fact]
    public async Task AuthenticateAsync_WithException_ShouldReturnErrorResponse()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Username = "testuser",
            Password = "Password123!"
        };

        // Dispose context to simulate database error
        _context.Dispose();

        // Act
        var result = await _authenticationService.AuthenticateAsync(loginRequest);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("An error occurred during authentication", result.Message);
        Assert.Empty(result.Token);

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error during authentication")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldLogWarningForFailedAttempt()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Username = "testuser",
            Password = "wrongpassword"
        };

        // Act
        await _authenticationService.AuthenticateAsync(loginRequest);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed login attempt for username: testuser")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldLogInformationForSuccessfulLogin()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Username = "testuser",
            Password = "Password123!"
        };

        var testUser = new UserModel
        {
            UserId = Guid.NewGuid(),
            Username = "testuser",
            PasswordHash = _passwordService.HashPassword(loginRequest.Password),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "123-456-7890",
            IsActive = true,
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();

        _mockJwtTokenService.Setup(x => x.BuildUserClaims(testUser, ActionTypeEnum.Login))
            .Returns("token");

        // Act
        await _authenticationService.AuthenticateAsync(loginRequest);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successful login for user: testuser")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}