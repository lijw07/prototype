using Microsoft.EntityFrameworkCore;
using Xunit;
using Prototype.Controllers;
using Prototype.Data;
using Prototype.Models;
using Prototype.Services;
using Prototype.DTOs;
using Prototype.Enum;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Prototype.Tests.Controllers
{
    public class PasswordResetControllerTest
    {
        [Fact]
        public async Task ResetPasswordWithValidToken()
        {
            var options = new DbContextOptionsBuilder<SentinelContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new SentinelContext(options);

            var testUser = new UserModel
            {
                UserId = Guid.NewGuid(),
                Username = "resetuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpassword"),
                FirstName = "Reset",
                LastName = "User",
                Email = "reset@example.com",
                PhoneNumber = "555-555-1234",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var testToken = Guid.NewGuid().ToString();
            var testRecoveryRequest = new UserRecoveryRequestModel
            {
                UserRecoveryRequestId = Guid.NewGuid(),
                UserId = testUser.UserId,
                User = testUser,
                UserRecoveryType = UserRecoveryTypeEnum.PASSWORD,
                Token = testToken,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(30)
            };

            context.Users.Add(testUser);
            context.UserRecoveryRequests.Add(testRecoveryRequest);
            context.SaveChanges();

            var mockFactory = new Mock<IEntityCreationFactoryService>();
            mockFactory.Setup(f => f.CreateAuditLogFromResetPassword(It.IsAny<UserRecoveryRequestModel>()))
                .Returns(new AuditLogModel
                {
                    AuditLogId = Guid.NewGuid(),
                    UserId = testUser.UserId,
                    User = testUser,
                    ActionType = ActionTypeEnum.ChangePassword,
                    Description = "Password was reset.",
                    Metadata = "{}",
                    CreatedAt = DateTime.UtcNow,
                });

            var mockAuditLogService = new Mock<IEntitySaveService<AuditLogModel>>();
            mockAuditLogService.Setup(s => s.CreateAsync(It.IsAny<AuditLogModel>()))
                .ReturnsAsync((AuditLogModel log) => log);

            var mockEmailService = new Mock<IEmailNotificationService>();
            mockEmailService.Setup(s => s.SendPasswordResetVerificationEmail(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var controller = new PasswordResetController(
                mockFactory.Object,
                mockAuditLogService.Object,
                mockEmailService.Object,
                context
            );
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var requestDto = new ResetPasswordRequestDto
            {
                Token = testToken,
                password = "newSecurePassword!"
            };
            
            var result = await controller.ResetPassword(requestDto);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Password has been successfully reset.", okResult.Value);
            
            var updatedUser = await context.Users.FirstAsync(u => u.UserId == testUser.UserId);
            Assert.True(BCrypt.Net.BCrypt.Verify("newSecurePassword!", updatedUser.PasswordHash));
        }
    }
}