using Microsoft.EntityFrameworkCore;
using Xunit;
using Prototype.Controllers;
using Prototype.Models;
using Prototype.Data;
using Prototype.Enum;
using Prototype.Services;
using Prototype.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Threading.Tasks;
using Prototype.Utility;

namespace Prototype.Tests.Controllers;
public class LoginControllerTests
{
    [Fact]
    public async Task LoginWithValidCredentials()
    {
        var options = new DbContextOptionsBuilder<SentinelContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var context = new SentinelContext(options);

        var testUser = new UserModel
        {
            UserId = Guid.NewGuid(),
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PhoneNumber = "555-1234",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(testUser);
        context.SaveChanges();

        var mockFactory = new Mock<IEntityCreationFactoryService>();
        mockFactory.Setup(f => f.CreateUserActivityLogFromLogin(It.IsAny<UserModel>(), It.IsAny<HttpContext>()))
            .Returns(new UserActivityLogModel
            {
                UserActivityLogId = Guid.NewGuid(),
                IPAddress = "127.0.0.1",
                DeviceInformation = "UnitTestDevice",
                ActionType = ActionTypeEnum.Login,
                Description = "Test login",
                Timestamp = DateTime.UtcNow
            });

        var mockSaveService = new Mock<IEntitySaveService<UserActivityLogModel>>();
        mockSaveService.Setup(s => s.CreateAsync(It.IsAny<UserActivityLogModel>()))
            .ReturnsAsync((UserActivityLogModel log) => log);

        var controller = new LoginController(
            mockFactory.Object,
            mockSaveService.Object,
            context
        );
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var loginRequest = new LoginRequestDto
        {
            Username = "testuser",
            Password = "password123"
        };
        
        var result = await controller.Login(loginRequest);
        
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode ?? 200);
    }

    [Fact]
    public async Task LoginWithInvalidPassword()
    {
        var options = new DbContextOptionsBuilder<SentinelContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var context = new SentinelContext(options);

        var testUser = new UserModel
        {
            UserId = Guid.NewGuid(),
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PhoneNumber = "555-1234",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(testUser);
        context.SaveChanges();

        var mockFactory = new Mock<IEntityCreationFactoryService>();
        mockFactory.Setup(f => f.CreateUserActivityLogFromLogin(It.IsAny<UserModel>(), It.IsAny<HttpContext>()))
            .Returns(new UserActivityLogModel
            {
                UserActivityLogId = Guid.NewGuid(),
                IPAddress = "127.0.0.1",
                DeviceInformation = "UnitTestDevice",
                ActionType = ActionTypeEnum.FailedLogin,
                Description = "Test failed login",
                Timestamp = DateTime.UtcNow
            });

        var mockSaveService = new Mock<IEntitySaveService<UserActivityLogModel>>();
        mockSaveService.Setup(s => s.CreateAsync(It.IsAny<UserActivityLogModel>()))
            .ReturnsAsync((UserActivityLogModel log) => log);

        var controller = new LoginController(
            mockFactory.Object,
            mockSaveService.Object,
            context
        );
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var loginRequest = new LoginRequestDto
        {
            Username = "testuser",
            Password = "wrongpassword"
        };
        
        var result = await controller.Login(loginRequest);
        
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode ?? 401);
    }
}