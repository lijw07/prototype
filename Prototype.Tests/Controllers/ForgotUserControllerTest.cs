using Microsoft.EntityFrameworkCore;
using Xunit;
using Prototype.Controllers;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;
using Prototype.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Threading.Tasks;

namespace Prototype.Tests.Controllers
{
    public class ForgotUserControllerTest
    {
        [Fact]
        public async Task ForgotUserWithValidEmail()
        {
            var options = new DbContextOptionsBuilder<SentinelContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new SentinelContext(options);

            var testEmail = "user@example.com";
            var testUser = new UserModel
            {
                UserId = Guid.NewGuid(),
                Username = "forgotuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
                FirstName = "Forgot",
                LastName = "User",
                Email = testEmail,
                PhoneNumber = "555-555-5555",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            context.Users.Add(testUser);
            context.SaveChanges();

            var mockFactory = new Mock<IEntityCreationFactoryService>();
            var mockAuditLogService = new Mock<IEntitySaveService<AuditLogModel>>();
            var mockUserRecoveryLogService = new Mock<IEntitySaveService<UserRecoveryRequestModel>>();
            var mockVerificationService = new Mock<IVerificationService>();
            var mockEmailService = new Mock<IEmailNotificationService>();

            var fakeToken = Guid.NewGuid().ToString();
            var fakeRecoveryLog = new UserRecoveryRequestModel
            {
                UserRecoveryRequestId = Guid.NewGuid(),
                UserId = testUser.UserId,
                User = testUser,
                UserRecoveryType = UserRecoveryTypeEnum.PASSWORD,
                Token = fakeToken,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(30)
            };

            var fakeAuditLog = new AuditLogModel
            {
                AuditLogId = Guid.NewGuid(),
                UserId = testUser.UserId,
                User = testUser,
                ActionType = ActionTypeEnum.ChangePassword,
                Description = "Password was reset.",
                Metadata = "{}",
                CreatedAt = DateTime.Now
            };

            mockVerificationService.Setup(s => s.GenerateVerificationCode()).Returns(fakeToken);
            mockFactory.Setup(f => f.CreateUserRecoveryRequestFronForgotUser(testUser, It.IsAny<ForgotUserRequestDto>(), fakeToken))
                .Returns(fakeRecoveryLog);
            mockUserRecoveryLogService.Setup(s => s.CreateAsync(fakeRecoveryLog)).ReturnsAsync(fakeRecoveryLog);
            mockFactory.Setup(f => f.CreateAuditLogFromForgotUser(testUser, It.IsAny<ForgotUserRequestDto>(), fakeRecoveryLog))
                .Returns(fakeAuditLog);
            mockAuditLogService.Setup(s => s.CreateAsync(fakeAuditLog)).ReturnsAsync(fakeAuditLog);
            mockEmailService.Setup(e => e.SendPasswordResetEmail(testUser.Email, fakeToken)).Returns(Task.CompletedTask);
            mockEmailService.Setup(e => e.SendUsernameEmail(testUser.Email, testUser.Username)).Returns(Task.CompletedTask);

            var controller = new ForgotUserController(
                mockFactory.Object,
                mockAuditLogService.Object,
                mockUserRecoveryLogService.Object,
                mockVerificationService.Object,
                mockEmailService.Object,
                context
            );
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var requestDto = new ForgotUserRequestDto
            {
                Email = testEmail,
                UserRecoveryType = UserRecoveryTypeEnum.PASSWORD
            };

            var result = await controller.ForgotUser(requestDto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // Fix: Serialize and read "message" property from JSON
            var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var message = doc.RootElement.GetProperty("message").GetString();

            Assert.Equal("If your account exists, you will receive an email with a link to reset your password.", message);

            mockEmailService.Verify(e => e.SendPasswordResetEmail(testUser.Email, fakeToken), Times.Once);
        }
    }
}