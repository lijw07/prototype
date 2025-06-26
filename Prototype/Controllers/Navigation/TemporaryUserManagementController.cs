using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Controllers;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation;

[Route("settings/user/temporary")]
public class TemporaryUserManagementController : BaseApiController
{
    public TemporaryUserManagementController(
        SentinelContext context,
        IAuthenticatedUserAccessor userAccessor,
        TransactionService transactionService,
        IAuditLogService auditLogService,
        ILogger<TemporaryUserManagementController> logger)
        : base(logger, context, userAccessor, transactionService, auditLogService)
    {
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateTemporaryUser([FromBody] UpdateTemporaryUserRequestDto dto)
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (Context == null)
                    throw new InvalidOperationException("Database context is not available");

                var tempUser = await Context.TemporaryUsers.FindAsync(dto.TemporaryUserId);
                if (tempUser == null)
                    return BadRequestWithMessage("Temporary user not found");

                // Store original values for audit logging
                var originalFirstName = tempUser.FirstName;
                var originalLastName = tempUser.LastName;
                var originalEmail = tempUser.Email;
                var originalUsername = tempUser.Username;
                var originalPhoneNumber = tempUser.PhoneNumber;

                // Update the temporary user
                tempUser.FirstName = dto.FirstName;
                tempUser.LastName = dto.LastName;
                tempUser.Email = dto.Email;
                tempUser.Username = dto.Username;
                tempUser.PhoneNumber = dto.PhoneNumber ?? tempUser.PhoneNumber;

                await Context.SaveChangesAsync();

                // Create change details for audit log
                var changeDetails = new List<string>();
                if (originalFirstName != dto.FirstName)
                    changeDetails.Add($"First Name: {originalFirstName} → {dto.FirstName}");
                if (originalLastName != dto.LastName)
                    changeDetails.Add($"Last Name: {originalLastName} → {dto.LastName}");
                if (originalEmail != dto.Email)
                    changeDetails.Add($"Email: {originalEmail} → {dto.Email}");
                if (originalUsername != dto.Username)
                    changeDetails.Add($"Username: {originalUsername} → {dto.Username}");
                if (originalPhoneNumber != dto.PhoneNumber)
                    changeDetails.Add($"Phone: {originalPhoneNumber} → {dto.PhoneNumber}");

                if (changeDetails.Any())
                {
                    await LogAuditActivityAsync(currentUser.UserId, ActionTypeEnum.Update,
                        $"Administrator updated temporary user: {dto.FirstName} {dto.LastName} (ID: {dto.TemporaryUserId}) - {string.Join(", ", changeDetails)}",
                        $"Administrator modified temporary user: {dto.FirstName} {dto.LastName} (Username: {dto.Username})");
                }

                var result = new {
                    TemporaryUserId = tempUser.TemporaryUserId,
                    FirstName = tempUser.FirstName,
                    LastName = tempUser.LastName,
                    Username = tempUser.Username,
                    Email = tempUser.Email,
                    PhoneNumber = tempUser.PhoneNumber,
                    CreatedAt = tempUser.CreatedAt
                };

                return SuccessResponse(new { Message = "Temporary user updated successfully", User = result }, "Temporary user updated successfully");

            }, "updating temporary user");
        });
    }

    [HttpDelete("delete/{temporaryUserId:guid}")]
    public async Task<IActionResult> DeleteTemporaryUser(Guid temporaryUserId)
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (Context == null)
                    throw new InvalidOperationException("Database context is not available");

                var tempUser = await Context.TemporaryUsers.FindAsync(temporaryUserId);
                if (tempUser == null)
                {
                    Logger.LogWarning("Attempted to delete temporary user {TemporaryUserId} but user was not found in TemporaryUsers table", temporaryUserId);
                    return BadRequestWithMessage($"Temporary user with ID {temporaryUserId} not found in TemporaryUsers table");
                }

                // Store user details for audit logging before deletion
                var deletedUserInfo = $"{tempUser.FirstName} {tempUser.LastName} (ID: {tempUser.TemporaryUserId}, Username: {tempUser.Username}, Email: {tempUser.Email})";

                // Remove the temporary user
                Context.TemporaryUsers.Remove(tempUser);
                await Context.SaveChangesAsync();

                // Create audit log for the administrator who deleted the temporary user
                await LogAuditActivityAsync(currentUser.UserId, ActionTypeEnum.Delete,
                    $"Administrator deleted temporary user account: {deletedUserInfo}",
                    $"Administrator deleted temporary user account: {tempUser.FirstName} {tempUser.LastName} (Username: {tempUser.Username})");

                return SuccessResponse(new { Message = "Temporary user deleted successfully" }, "Temporary user deleted successfully");

            }, "deleting temporary user");
        });
    }

    private async Task LogAuditActivityAsync(Guid userId, ActionTypeEnum actionType, string auditMetadata, string activityDescription)
    {
        if (Context == null)
            return;

        // Create audit log for the administrator who made the change
        var auditLog = new AuditLogModel
        {
            AuditLogId = Guid.NewGuid(),
            UserId = userId,
            User = null,
            ActionType = actionType,
            Metadata = auditMetadata,
            CreatedAt = DateTime.UtcNow
        };

        // Create user activity log for the administrator
        var activityLog = new UserActivityLogModel
        {
            UserActivityLogId = Guid.NewGuid(),
            UserId = userId,
            User = null,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString() ?? "Unknown",
            ActionType = actionType,
            Description = activityDescription,
            Timestamp = DateTime.UtcNow
        };

        // Add logs to database context and save
        Context.AuditLogs.Add(auditLog);
        Context.UserActivityLogs.Add(activityLog);
        await Context.SaveChangesAsync();
    }
}