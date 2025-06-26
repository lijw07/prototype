using Microsoft.AspNetCore.Mvc;
using Prototype.Common.Responses;
using Prototype.Exceptions;

namespace Prototype.Services.Common;

public interface IErrorHandlerService
{
    Task<IActionResult> HandleErrorAsync<T>(
        Exception ex,
        string operationContext,
        ILogger logger,
        string? userMessage = null);
    
    Task<IActionResult> HandleValidationErrorAsync(
        List<string> validationErrors,
        string operationContext,
        ILogger logger);
    
    Task<IActionResult> ExecuteWithErrorHandlingAsync<T>(
        Func<Task<T>> operation,
        string operationContext,
        ILogger logger,
        string? userMessage = null);
}

public class ErrorHandlerService : IErrorHandlerService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ErrorHandlerService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> HandleErrorAsync<T>(
        Exception ex,
        string operationContext,
        ILogger logger,
        string? userMessage = null)
    {
        var correlationId = GetOrCreateCorrelationId();
        var userId = GetCurrentUserId();
        
        // Log with structured information
        logger.LogError(ex,
            "Operation failed: {Operation} | UserId: {UserId} | CorrelationId: {CorrelationId} | Error: {Error}",
            operationContext, userId, correlationId, ex.Message);

        var statusCode = DetermineStatusCode(ex);
        var message = DetermineUserMessage(ex, userMessage);
        var errors = ExtractErrorDetails(ex, correlationId);

        return new ObjectResult(ApiResponse<T>.Failure(message, errors))
        {
            StatusCode = statusCode
        };
    }

    public async Task<IActionResult> HandleValidationErrorAsync(
        List<string> validationErrors,
        string operationContext,
        ILogger logger)
    {
        var correlationId = GetOrCreateCorrelationId();
        var userId = GetCurrentUserId();

        logger.LogWarning(
            "Validation failed: {Operation} | UserId: {UserId} | CorrelationId: {CorrelationId} | Errors: {Errors}",
            operationContext, userId, correlationId, string.Join(", ", validationErrors));

        return new BadRequestObjectResult(ApiResponse<object>.Failure(
            "Validation failed",
            validationErrors));
    }

    public async Task<IActionResult> ExecuteWithErrorHandlingAsync<T>(
        Func<Task<T>> operation,
        string operationContext,
        ILogger logger,
        string? userMessage = null)
    {
        try
        {
            var result = await operation();
            return new OkObjectResult(ApiResponse<T>.Success(result));
        }
        catch (ValidationException validationEx)
        {
            return await HandleValidationErrorAsync(
                validationEx.Errors,
                operationContext,
                logger);
        }
        catch (Exception ex)
        {
            return await HandleErrorAsync<T>(ex, operationContext, logger, userMessage);
        }
    }

    private int DetermineStatusCode(Exception ex)
    {
        return ex switch
        {
            ValidationException => 400,
            UnauthorizedAccessException => 401,
            ForbiddenException => 403,
            NotFoundException => 404,
            ConflictException => 409,
            ExternalServiceException => 502,
            TimeoutException => 504,
            _ => 500
        };
    }

    private string DetermineUserMessage(Exception ex, string? userMessage)
    {
        if (!string.IsNullOrEmpty(userMessage))
            return userMessage;

        return ex switch
        {
            ValidationException validationEx => validationEx.Message,
            UnauthorizedAccessException => "Access denied",
            NotFoundException => "Resource not found",
            ConflictException => "Resource conflict",
            ExternalServiceException => "External service unavailable",
            TimeoutException => "Operation timed out",
            _ => "An internal error occurred"
        };
    }

    private List<string> ExtractErrorDetails(Exception ex, string correlationId)
    {
        var errors = new List<string> { $"Reference ID: {correlationId}" };

        if (ex is ValidationException validationEx)
        {
            errors.AddRange(validationEx.Errors);
        }
        else if (ex is DomainException domainEx)
        {
            errors.Add(domainEx.UserMessage);
        }

        return errors;
    }

    private string GetOrCreateCorrelationId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return Guid.NewGuid().ToString();

        const string correlationIdKey = "X-Correlation-ID";
        
        if (context.Request.Headers.TryGetValue(correlationIdKey, out var existingId))
        {
            return existingId.FirstOrDefault() ?? Guid.NewGuid().ToString();
        }

        var newId = Guid.NewGuid().ToString();
        context.Response.Headers[correlationIdKey] = newId;
        return newId;
    }

    private string? GetCurrentUserId()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.User?.FindFirst("userId")?.Value;
    }
}