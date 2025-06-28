using Microsoft.AspNetCore.Mvc;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation;

[Route("navigation/temporary-user-management")]
public class TemporaryUserManagementController(
    SentinelContext context,
    IAuthenticatedUserAccessor userAccessor,
    TransactionService transactionService,
    IAuditLogService auditLogService,
    ILogger<TemporaryUserManagementController> logger)
    : BaseNavigationController(logger, context, userAccessor, transactionService, auditLogService)
{
    [HttpPut("update")]
    public async Task<IActionResult> UpdateTemporaryUser([FromBody] UpdateTemporaryUserRequestDto dto)
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            return await ExecuteInTransactionWithAuditAsync(async user =>
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

            }, ActionTypeEnum.Update,
               $"Administrator updated temporary user: {dto.FirstName} {dto.LastName} (ID: {dto.TemporaryUserId})",
               "Temporary user updated successfully");
        });
    }

    [HttpDelete("delete/{temporaryUserId:guid}")]
    public async Task<IActionResult> DeleteTemporaryUser(Guid temporaryUserId)
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            // Get temporary user info before deletion
            var tempUser = await Context!.TemporaryUsers.FindAsync(temporaryUserId);
            if (tempUser == null)
            {
                Logger.LogWarning("Attempted to delete temporary user {TemporaryUserId} but user was not found in TemporaryUsers table", temporaryUserId);
                return BadRequestWithMessage($"Temporary user with ID {temporaryUserId} not found in TemporaryUsers table");
            }

            return await ExecuteInTransactionWithAuditAsync(async user =>
            {
                if (Context == null)
                    throw new InvalidOperationException("Database context is not available");

                // Remove the temporary user
                Context.TemporaryUsers.Remove(tempUser);

                return SuccessResponse(new { Message = "Temporary user deleted successfully" }, "Temporary user deleted successfully");

            }, ActionTypeEnum.Delete,
               $"Administrator deleted temporary user account: {tempUser.FirstName} {tempUser.LastName} (ID: {tempUser.TemporaryUserId}, Username: {tempUser.Username}, Email: {tempUser.Email})",
               "Temporary user deleted successfully");
        });
    }

}