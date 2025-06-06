using Microsoft.EntityFrameworkCore;
using Xunit;
using Prototype.Controllers;
using Prototype.Data;
using Prototype.Models;
using Prototype.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Threading.Tasks;

namespace Prototype.Tests.Controllers
{
    public class VerifyUserControllerTest
    {
        [Fact]
        public async Task VerifyEmailWithValidEmailAndCode()
        {
            var options = new DbContextOptionsBuilder<SentinelContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new SentinelContext(options);

            var tempEmail = "verify@example.com";
            var verificationCode = "654321";

            var tempUser = new TemporaryUserModel
            {
                TemporaryUserId = Guid.NewGuid(),
                Username = "test",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
                FirstName = "Test",
                LastName = "User",
                Email = tempEmail,
                PhoneNumber = "301-283-2832",
                VerificationCode = verificationCode,
                CreatedAt = DateTime.Now
            };

            context.TemporaryUsers.Add(tempUser);
            context.SaveChanges();

            var mockSaveService = new Mock<IEntitySaveService<UserModel>>();
            var mockFactory = new Mock<IEntityCreationFactoryService>();
            var mockEmail = new Mock<IEmailNotificationService>();

            var finalUser = new UserModel
            {
                UserId = Guid.NewGuid(),
                Username = tempUser.Username,
                PasswordHash = tempUser.PasswordHash,
                Email = tempUser.Email,
                FirstName = "Test",
                LastName = "User",
                PhoneNumber = "555-5555",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            
            mockFactory.Setup(f => f.CreateUserFromTemporaryUser(It.IsAny<TemporaryUserModel>()))
                .Returns(finalUser);
            mockSaveService.Setup(s => s.CreateAsync(It.IsAny<UserModel>()))
                .ReturnsAsync(finalUser);
            mockEmail.Setup(s => s.SendAccountCreationEmail(finalUser.Email, finalUser.Username))
                .Returns(Task.CompletedTask);

            var controller = new VerifyUserController(
                mockSaveService.Object,
                mockFactory.Object,
                mockEmail.Object,
                context
            );
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = await controller.VerifyEmail(tempEmail, verificationCode);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Your email has been verified. You may now log in.", okResult.Value);

            Assert.False(await context.TemporaryUsers.AnyAsync(t => t.Email == tempEmail && t.VerificationCode == verificationCode));

            mockEmail.Verify(e => e.SendAccountCreationEmail(finalUser.Email, finalUser.Username), Times.Once);
        }
    }
}