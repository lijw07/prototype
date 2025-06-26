using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prototype.Common.Responses;
using Prototype.Constants;
using Prototype.Models;
using Prototype.Services;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation;

[Authorize]
[ApiController]
public abstract class BaseNavigationController : ControllerBase
{
    protected readonly ILogger Logger;
    protected readonly IAuthenticatedUserAccessor? UserAccessor;
    protected readonly TransactionService? TransactionService;

    protected BaseNavigationController(ILogger logger)
    {
        Logger = logger;
    }

    protected BaseNavigationController(
        ILogger logger,
        IAuthenticatedUserAccessor userAccessor,
        TransactionService transactionService)
    {
        Logger = logger;
        UserAccessor = userAccessor;
        TransactionService = transactionService;
    }

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
            
            // In development, return the actual error message
            #if DEBUG
            return StatusCode(500, ApiResponse.FailureResponse($"Error: {ex.Message}. Inner: {ex.InnerException?.Message}"));
            #else
            return StatusCode(500, ApiResponse.FailureResponse(ApplicationConstants.ErrorMessages.ServerError));
            #endif
        }
    }

    protected async Task<UserModel?> GetCurrentUserAsync()
    {
        if (UserAccessor == null) return null;
        return await UserAccessor.GetCurrentUserAsync();
    }

    protected IActionResult HandleUserNotAuthenticated()
    {
        return Unauthorized(ApiResponse.FailureResponse(ApplicationConstants.ErrorMessages.UnauthorizedAccess));
    }

    protected static (int page, int pageSize, int skip) ValidatePaginationParameters(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50; // TODO: Move to constants
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

    protected async Task<IActionResult> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        string successMessage = "Operation completed successfully")
    {
        if (TransactionService == null)
            throw new InvalidOperationException("Transaction service not available");

        var result = await TransactionService.ExecuteInTransactionAsync(operation);
        return Ok(result);
    }
}