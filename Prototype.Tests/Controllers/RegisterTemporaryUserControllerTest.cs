using Microsoft.EntityFrameworkCore;
using Xunit;
using Prototype.Controllers;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Models;
using Prototype.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Prototype.Tests.Controllers
{
    public class RegisterTemporaryUserControllerTest
    {
        [Fact]
        public async Task RegisterWithValidCredentials()
        {
            var options = new DbContextOptionsBuilder<SentinelContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new SentinelContext(options);

            var mockSaveService = new Mock<IEntitySaveService<TemporaryUserModel>>();
            var mockVerificationService = new Mock<IVerificationService>();
            var mockFactoryService = new Mock<IEntityCreationFactoryService>();
            var mockEmailService = new Mock<IEmailNotificationService>();

            var controller = new RegisterTemporaryUserController(
                mockSaveService.Object,
                mockVerificationService.Object,
                mockFactoryService.Object,
                mockEmailService.Object,
                context
            );

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var testRegisterUser = new RegisterRequestDto
            {
                Username = "test",
                Password = "password",
                FirstName = "Test",
                LastName = "User",
                Email = "test@test.com",
                PhoneNumber = "301-283-2832"
            };
            
            var fakeTempUser = new TemporaryUserModel
            {
                TemporaryUserId = Guid.NewGuid(),
                Username = testRegisterUser.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(testRegisterUser.Password),
                FirstName = testRegisterUser.FirstName,
                LastName = testRegisterUser.LastName,
                Email = testRegisterUser.Email,
                PhoneNumber = testRegisterUser.PhoneNumber,
                VerificationCode = "123456",
                CreatedAt = DateTime.Now.Date
            };

            mockVerificationService.Setup(s => s.GenerateVerificationCode()).Returns("123456");
            mockFactoryService.Setup(f => f.CreateTemporaryUserFromRequest(It.IsAny<RegisterRequestDto>(), It.IsAny<string>()))
                .Returns(fakeTempUser);
            mockSaveService.Setup(s => s.CreateAsync(It.IsAny<TemporaryUserModel>()))
                .ReturnsAsync(fakeTempUser);
            mockEmailService.Setup(e => e.SendVerificationEmail(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            
            var result = await controller.Register(testRegisterUser);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
        }
    }
}