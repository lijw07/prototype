using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prototype.Common.Responses;
using Prototype.Models;
using Prototype.Services;
using Prototype.Utility;

namespace Prototype.Controllers.Settings;

[Authorize]
[ApiController]
public abstract class BaseSettingsController : ControllerBase
{
    protected readonly ILogger _logger;
    protected readonly IAuthenticatedUserAccessor? _userAccessor;
    protected readonly ValidationService? _validationService;
    protected readonly TransactionService? _transactionService;

    protected BaseSettingsController(ILogger logger)
    {
        _logger = logger;
    }

    protected BaseSettingsController(
        ILogger logger,
        IAuthenticatedUserAccessor userAccessor,
        ValidationService validationService,
        TransactionService transactionService)
    {
        _logger = logger;
        _userAccessor = userAccessor;
        _validationService = validationService;
        _transactionService = transactionService;
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
            _logger.LogError(ex, "Error in {ErrorContext}", errorContext);
            return StatusCode(500, ApiResponse.FailureResponse("An internal error occurred"));
        }
    }

    protected async Task<UserModel?> GetCurrentUserAsync()
    {
        if (_userAccessor == null) return null;
        return await _userAccessor.GetCurrentUserAsync();
    }

    protected IActionResult HandleUserNotAuthenticated()
    {
        return Unauthorized(ApiResponse.FailureResponse("User not authenticated"));
    }

    protected static (int page, int pageSize, int skip) ValidatePaginationParameters(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;
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
        if (_transactionService == null)
            throw new InvalidOperationException("Transaction service not available");

        var result = await _transactionService.ExecuteInTransactionAsync(operation);
        return Ok(result);
    }
}