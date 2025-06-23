using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Prototype.Common;
using Prototype.Controllers.Login;
using Prototype.DTOs;
using Prototype.DTOs.Responses;
using Prototype.Models;
using Prototype.Services.Interfaces;
using Xunit;

namespace Prototype.Tests.Controllers.Login;

public class VerifyUserControllerTest
{
    private readonly Mock<IUserAccountService> _mockUserAccountService;
    private readonly Mock<ILogger<VerifyUserController>> _mockLogger;
    private readonly VerifyUserController _controller;

    public VerifyUserControllerTest()
    {
        _mockUserAccountService = new Mock<IUserAccountService>();
        _mockLogger = new Mock<ILogger<VerifyUserController>>();
        _controller = new VerifyUserController(_mockUserAccountService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task VerifyEmail_WithValidToken_ReturnsOkResult()
    {
        // Arrange
        var token = "valid-verification-token";
        var user = new UserModel
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

        _mockUserAccountService.Setup(x => x.RegisterNewUser(token))
            .ReturnsAsync(new LoginResponse 
            { 
                Success = true, 
                User = new UserDto
                {
                    UserId = user.UserId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Username = user.Username,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                }
            });

        // Act
        var result = await _controller.VerifyEmail(token);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<LoginResponse>(okResult.Value);
        Assert.True(returnedResult.Success);
        Assert.Equal(user.UserId, returnedResult.User?.UserId);
        
        _mockUserAccountService.Verify(x => x.RegisterNewUser(token), Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_WithInvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var token = "invalid-verification-token";
        _mockUserAccountService.Setup(x => x.RegisterNewUser(token))
            .ReturnsAsync(new LoginResponse { Success = false, Message = "Invalid or expired verification token" });

        // Act
        var result = await _controller.VerifyEmail(token);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var returnedResult = Assert.IsType<LoginResponse>(badRequestResult.Value);
        Assert.False(returnedResult.Success);
        Assert.Equal("Invalid or expired verification token", returnedResult.Message);
        
        _mockUserAccountService.Verify(x => x.RegisterNewUser(token), Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_WithNullToken_CallsServiceWithNull()
    {
        // Arrange
        string? token = null;
        _mockUserAccountService.Setup(x => x.RegisterNewUser(token!))
            .ReturnsAsync(new LoginResponse { Success = false, Message = "Token is required" });

        // Act
        var result = await _controller.VerifyEmail(token!);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        _mockUserAccountService.Verify(x => x.RegisterNewUser(token!), Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_WithEmptyToken_CallsServiceWithEmptyString()
    {
        // Arrange
        var token = "";
        _mockUserAccountService.Setup(x => x.RegisterNewUser(token))
            .ReturnsAsync(new LoginResponse { Success = false, Message = "Token cannot be empty" });

        // Act
        var result = await _controller.VerifyEmail(token);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        _mockUserAccountService.Verify(x => x.RegisterNewUser(token), Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var token = "test-token";
        var exception = new InvalidOperationException("Database connection failed");

        _mockUserAccountService.Setup(x => x.RegisterNewUser(token))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.VerifyEmail(token);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        
        var response = statusCodeResult.Value as dynamic;
        Assert.NotNull(response);
        Assert.Equal("An internal error occurred", response!.message);

        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error during email verification for token")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_WithSpecialCharactersInToken_HandlesCorrectly()
    {
        // Arrange
        var token = "token-with-special-chars!@#$%^&*()";
        _mockUserAccountService.Setup(x => x.RegisterNewUser(token))
            .ReturnsAsync(new LoginResponse 
            { 
                Success = true, 
                User = new UserDto
                {
                    UserId = Guid.NewGuid(),
                    Username = "testuser",
                    Email = "test@example.com",
                    FirstName = "Test",
                    LastName = "User",
                    PhoneNumber = "555-0123",
                    Role = "User",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                }
            });

        // Act
        var result = await _controller.VerifyEmail(token);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockUserAccountService.Verify(x => x.RegisterNewUser(token), Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_WithLongToken_HandlesCorrectly()
    {
        // Arrange
        var token = new string('a', 1000); // Very long token
        _mockUserAccountService.Setup(x => x.RegisterNewUser(token))
            .ReturnsAsync(new LoginResponse { Success = false, Message = "Token format invalid" });

        // Act
        var result = await _controller.VerifyEmail(token);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        _mockUserAccountService.Verify(x => x.RegisterNewUser(token), Times.Once);
    }
}