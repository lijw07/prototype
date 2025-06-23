using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Prototype.Controllers.Login;
using Prototype.DTOs;
using Prototype.DTOs.Responses;
using Prototype.Services.Interfaces;
using Xunit;

namespace Prototype.Tests.Controllers.Login;

public class LoginControllerTest
{
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<ILogger<LoginController>> _mockLogger;
    private readonly LoginController _controller;

    public LoginControllerTest()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockLogger = new Mock<ILogger<LoginController>>();
        _controller = new LoginController(_mockAuthService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkResult()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Username = "testuser",
            Password = "Password123!"
        };

        var loginResponse = new LoginResponse
        {
            Success = true,
            Message = "Login successful",
            Token = "valid-jwt-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        _mockAuthService.Setup(x => x.AuthenticateAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(loginResponse);

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<LoginResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Login successful", response.Message);
        Assert.Equal("valid-jwt-token", response.Token);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Username = "testuser",
            Password = "wrongpassword"
        };

        var loginResponse = new LoginResponse
        {
            Success = false,
            Message = "Invalid username or password",
            Token = string.Empty
        };

        _mockAuthService.Setup(x => x.AuthenticateAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(loginResponse);

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<LoginResponse>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Invalid username or password", response.Message);
    }

    [Fact]
    public async Task Login_WithServiceException_ReturnsInternalServerError()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Username = "testuser",
            Password = "Password123!"
        };

        _mockAuthService.Setup(x => x.AuthenticateAsync(It.IsAny<LoginRequestDto>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
        
        var responseValue = statusResult.Value as dynamic;
        Assert.NotNull(responseValue);
        
        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error during login for username: testuser")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Login_WithSuccessfulLogin_CallsAuthenticationService()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Username = "testuser",
            Password = "Password123!"
        };

        var loginResponse = new LoginResponse
        {
            Success = true,
            Message = "Login successful",
            Token = "valid-jwt-token"
        };

        _mockAuthService.Setup(x => x.AuthenticateAsync(loginRequest))
            .ReturnsAsync(loginResponse);

        // Act
        await _controller.Login(loginRequest);

        // Assert
        _mockAuthService.Verify(x => x.AuthenticateAsync(loginRequest), Times.Once);
    }

    [Fact]
    public async Task Login_WithValidationFailure_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Username = "",
            Password = ""
        };

        var loginResponse = new LoginResponse
        {
            Success = false,
            Message = "Validation failed",
            Token = string.Empty
        };

        _mockAuthService.Setup(x => x.AuthenticateAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(loginResponse);

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<LoginResponse>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Validation failed", response.Message);
    }
}