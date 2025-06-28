using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prototype.Common.Responses;
using Prototype.Constants;
using Prototype.Data;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;
using Prototype.Utility;

namespace Prototype.Controllers
{
    [Authorize]
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        protected readonly ILogger Logger;
        protected readonly SentinelContext? Context;
        protected readonly IAuthenticatedUserAccessor? UserAccessor;
        protected readonly TransactionService? TransactionService;
        protected readonly IAuditLogService? AuditLogService;

        protected BaseApiController(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected BaseApiController(
            ILogger logger,
            SentinelContext context,
            IAuthenticatedUserAccessor userAccessor,
            TransactionService transactionService,
            IAuditLogService auditLogService)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Context = context;
            UserAccessor = userAccessor;
            TransactionService = transactionService;
            AuditLogService = auditLogService;
        }

        #region User Context Management

        protected async Task<UserModel?> GetCurrentUserAsync()
        {
            if (UserAccessor == null) return null;
            return await UserAccessor.GetCurrentUserAsync(User);
        }

        protected IActionResult HandleUserNotAuthenticated()
        {
            return Unauthorized(ApiResponse.FailureResponse(ApplicationConstants.ErrorMessages.UnauthorizedAccess));
        }

        protected async Task<IActionResult> EnsureUserAuthenticatedAsync<T>(Func<UserModel, Task<T>> operation)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
                return HandleUserNotAuthenticated();

            try
            {
                var result = await operation(currentUser);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error executing authenticated operation for user {UserId}", currentUser.UserId);
                return InternalServerError();
            }
        }

        #endregion

        #region Response Helpers

        protected IActionResult InternalServerError(string? message = null)
        {
            var errorMessage = message ?? ApplicationConstants.ErrorMessages.ServerError;
            
            #if DEBUG
            return StatusCode(500, ApiResponse.FailureResponse(errorMessage));
            #else
            return StatusCode(500, ApiResponse.FailureResponse(ApplicationConstants.ErrorMessages.ServerError));
            #endif
        }

        protected IActionResult BadRequestWithMessage(string message)
        {
            return BadRequest(ApiResponse.FailureResponse(message));
        }

        protected IActionResult SuccessResponse<T>(T data, string? message = null)
        {
            var successMessage = message ?? ApplicationConstants.SuccessMessages.OperationSuccess;
            return Ok(ApiResponse<T>.Success(data, successMessage));
        }

        protected IActionResult SuccessResponse(string? message = null)
        {
            var successMessage = message ?? ApplicationConstants.SuccessMessages.OperationSuccess;
            return Ok(ApiResponse.SuccessResponse(successMessage));
        }

        #endregion

        #region Error Handling

        protected async Task<IActionResult> ExecuteWithErrorHandlingAsync<T>(
            Func<Task<T>> operation, 
            string errorContext)
        {
            try
            {
                var result = await operation();
                return Ok(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in {ErrorContext}", errorContext);
                return InternalServerError();
            }
        }

        protected async Task<IActionResult> ExecuteWithAuditAsync<T>(
            Func<UserModel, Task<T>> operation,
            ActionTypeEnum actionType,
            string actionDescription,
            string errorContext)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
                return HandleUserNotAuthenticated();

            try
            {
                var result = await operation(currentUser);
                
                // Log successful action
                if (AuditLogService != null)
                {
                    await AuditLogService.LogUserActionAsync(
                        currentUser.UserId, 
                        actionType, 
                        actionDescription, 
                        actionDescription);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in {ErrorContext} for user {UserId}", errorContext, currentUser.UserId);
                return InternalServerError();
            }
        }

        #endregion

        #region Pagination

        protected static (int page, int pageSize, int skip) ValidatePaginationParameters(int page, int pageSize)
        {
            if (page < ApplicationConstants.Pagination.DefaultPage) 
                page = ApplicationConstants.Pagination.DefaultPage;
            
            if (pageSize < ApplicationConstants.Pagination.MinPageSize || 
                pageSize > ApplicationConstants.Pagination.MaxPageSize) 
                pageSize = ApplicationConstants.Pagination.DefaultPageSize;
            
            var skip = (page - 1) * pageSize;
            return (page, pageSize, skip);
        }

        protected static object CreatePaginatedResponse<T>(
            IEnumerable<T> data, 
            int page, 
            int pageSize, 
            int totalCount)
        {
            return new
            {
                Data = data,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }

        #endregion

        #region Transaction Helpers

        protected async Task<IActionResult> ExecuteInTransactionAsync<T>(
            Func<Task<T>> operation,
            string successMessage = ApplicationConstants.SuccessMessages.OperationSuccess)
        {
            if (TransactionService == null)
                throw new InvalidOperationException("Transaction service not available");

            try
            {
                var result = await TransactionService.ExecuteInTransactionAsync(operation);
                return SuccessResponse(result, successMessage);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error executing transaction");
                return InternalServerError();
            }
        }

        protected async Task<IActionResult> ExecuteInTransactionWithAuditAsync<T>(
            Func<UserModel, Task<T>> operation,
            ActionTypeEnum actionType,
            string actionDescription,
            string successMessage = ApplicationConstants.SuccessMessages.OperationSuccess)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
                return HandleUserNotAuthenticated();

            if (TransactionService == null)
                throw new InvalidOperationException("Transaction service not available");

            try
            {
                var result = await TransactionService.ExecuteInTransactionAsync(() => operation(currentUser));
                
                // Log successful action
                if (AuditLogService != null)
                {
                    await AuditLogService.LogUserActionAsync(
                        currentUser.UserId, 
                        actionType, 
                        actionDescription, 
                        actionDescription);
                }

                return SuccessResponse(result, successMessage);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error executing transaction with audit for user {UserId}", currentUser.UserId);
                return InternalServerError();
            }
        }

        #endregion

        #region Client Information

        protected string GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? ApplicationConstants.DefaultIpAddress;
        }

        protected string GetClientDeviceInfo()
        {
            return HttpContext.Request.Headers.UserAgent.ToString() ?? ApplicationConstants.DefaultDeviceInfo;
        }

        #endregion

        #region Audit Logging Helpers

        protected async Task LogUserActionAsync(Guid userId, ActionTypeEnum actionType, string metadata, string? description = null)
        {
            if (AuditLogService != null)
            {
                try
                {
                    await AuditLogService.LogUserActionAsync(userId, actionType, metadata, description);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to log user action: {ActionType} for user {UserId}", actionType, userId);
                }
            }
        }

        protected async Task LogApplicationActionAsync(Guid userId, Guid applicationId, ActionTypeEnum actionType, string metadata)
        {
            if (AuditLogService != null)
            {
                try
                {
                    await AuditLogService.LogApplicationActionAsync(userId, applicationId, actionType, metadata);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to log application action: {ActionType} for user {UserId}, app {ApplicationId}", 
                        actionType, userId, applicationId);
                }
            }
        }

        #endregion
    }
}