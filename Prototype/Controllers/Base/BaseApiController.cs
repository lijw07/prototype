using Microsoft.AspNetCore.Mvc;
using Prototype.Common.Responses;
using Prototype.Enum;
using Prototype.Exceptions;
using Prototype.Models;
using Prototype.Services.Common;
using Prototype.Services.Interfaces;
using Prototype.Utility;

namespace Prototype.Controllers.Base;

public abstract class BaseApiController(
    ILogger logger,
    IErrorHandlerService errorHandler,
    IAuthenticatedUserAccessor? userAccessor = null,
    ICacheService? cacheService = null)
    : ControllerBase
{
    private readonly ILogger Logger = logger;
    private readonly IErrorHandlerService ErrorHandler = errorHandler;
    protected readonly IAuthenticatedUserAccessor? UserAccessor = userAccessor;
    protected readonly ICacheService? CacheService = cacheService;

    /// <summary>
    /// Executes an operation with authentication check and standardized error handling
    /// </summary>
    protected async Task<IActionResult> ExecuteWithAuthenticationAsync<T>(
        Func<UserModel, Task<T>> operation,
        string operationDescription,
        string? userMessage = null)
    {
        if (UserAccessor == null)
            return Unauthorized(ApiResponse.FailureResponse("Authentication service not available"));

        var currentUser = await UserAccessor.GetCurrentUserAsync(User);
        if (currentUser == null)
            return Unauthorized(ApiResponse.FailureResponse("User not authenticated"));

        return await ErrorHandler.ExecuteWithErrorHandlingAsync(
            () => operation(currentUser),
            operationDescription,
            Logger,
            userMessage);
    }

    /// <summary>
    /// Executes an operation with caching support
    /// </summary>
    protected async Task<IActionResult> ExecuteWithCacheAsync<T>(
        string cacheKey,
        Func<Task<T>> operation,
        TimeSpan? cacheExpiry = null,
        bool useSecureCache = false,
        Guid? userId = null,
        string? operationDescription = null) where T : class
    {
        try
        {
            if (CacheService == null)
                return await ExecuteStandardAsync(operation, operationDescription);

            // Check cache first
            T? cachedResult = useSecureCache && userId.HasValue
                ? await CacheService.GetSecureAsync<T>(cacheKey, userId.Value)
                : await CacheService.GetAsync<T>(cacheKey);

            if (cachedResult != null)
            {
                Logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
                return Ok(ApiResponse<T>.Success(cachedResult));
            }

            // Execute operation
            var result = await operation();
            
            // Cache result
            var expiry = cacheExpiry ?? TimeSpan.FromMinutes(10);
            if (useSecureCache && userId.HasValue)
                await CacheService.SetSecureAsync(cacheKey, result, userId.Value, expiry);
            else
                await CacheService.SetAsync(cacheKey, result, expiry);

            Logger.LogDebug("Cached result for key: {CacheKey}", cacheKey);
            return Ok(ApiResponse<T>.Success(result));
        }
        catch (Exception ex)
        {
            var description = operationDescription ?? "executing cached operation";
            Logger.LogError(ex, "Error {Description}: {Error}", description, ex.Message);
            return StatusCode(500, ApiResponse.FailureResponse("An internal error occurred"));
        }
    }

    /// <summary>
    /// Executes an operation with audit logging
    /// </summary>
    protected async Task<IActionResult> ExecuteWithAuditAsync<T>(
        Func<UserModel, Task<T>> operation,
        ActionTypeEnum actionType,
        string description,
        string? operationDescription = null)
    {
        try
        {
            if (UserAccessor == null)
                return Unauthorized(ApiResponse.FailureResponse("Authentication service not available"));

            var currentUser = await UserAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(ApiResponse.FailureResponse("User not authenticated"));

            var result = await operation(currentUser);

            // Log activity
            await LogUserActivityAsync(currentUser.UserId, actionType, description);

            return Ok(ApiResponse<T>.Success(result));
        }
        catch (Exception ex)
        {
            var opDescription = operationDescription ?? "executing audited operation";
            Logger.LogError(ex, "Error {Description}: {Error}", opDescription, ex.Message);
            return StatusCode(500, ApiResponse.FailureResponse("An internal error occurred"));
        }
    }

    /// <summary>
    /// Standard operation execution with centralized error handling
    /// </summary>
    protected async Task<IActionResult> ExecuteStandardAsync<T>(
        Func<Task<T>> operation,
        string operationDescription,
        string? userMessage = null)
    {
        return await ErrorHandler.ExecuteWithErrorHandlingAsync(
            operation,
            operationDescription,
            Logger,
            userMessage);
    }

    /// <summary>
    /// Paginated operation with caching
    /// </summary>
    protected async Task<IActionResult> ExecutePaginatedWithCacheAsync<T>(
        int page,
        int pageSize,
        Func<int, int, int, Task<List<T>>> operation,
        Func<Task<int>> countOperation,
        string cacheKeyPrefix,
        TimeSpan? cacheExpiry = null,
        string? operationDescription = null) where T : class
    {
        try
        {
            // Validate pagination
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var skip = (page - 1) * pageSize;
            var cacheKey = $"{cacheKeyPrefix}:page:{page}:size:{pageSize}";

            return await ExecuteWithCacheAsync(
                cacheKey,
                async () =>
                {
                    var totalCount = await countOperation();
                    var items = await operation(skip, pageSize, totalCount);
                    
                    return new
                    {
                        items = items,
                        pagination = new
                        {
                            page = page,
                            pageSize = pageSize,
                            totalCount = totalCount,
                            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                        }
                    };
                },
                cacheExpiry,
                operationDescription: operationDescription
            );
        }
        catch (Exception ex)
        {
            var description = operationDescription ?? "executing paginated operation";
            Logger.LogError(ex, "Error {Description}: {Error}", description, ex.Message);
            return StatusCode(500, ApiResponse.FailureResponse("An internal error occurred"));
        }
    }

    private async Task LogUserActivityAsync(Guid userId, ActionTypeEnum actionType, string description)
    {
        try
        {
            // This would need to be injected or accessed through a service
            // Implementation depends on your specific audit logging strategy
            Logger.LogInformation("User {UserId} performed {ActionType}: {Description}", 
                userId, actionType, description);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to log user activity");
        }
    }
}