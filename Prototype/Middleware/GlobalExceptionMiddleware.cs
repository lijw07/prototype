using System.Net;
using System.Text.Json;
using Prototype.Common.Responses;
using Prototype.Constants;
using Prototype.Exceptions;
using Prototype.Services;

namespace Prototype.Middleware;

public class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger,
    IWebHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        logger.LogError(exception, "Unhandled exception occurred. Path: {Path}, Method: {Method}, TraceId: {TraceId}", 
            context.Request.Path, context.Request.Method, context.TraceIdentifier);

        context.Response.ContentType = "application/json";
        
        var response = CreateErrorResponse(exception, context.TraceIdentifier);
        context.Response.StatusCode = response.StatusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = environment.IsDevelopment()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response.Body, jsonOptions));
    }

    private (int StatusCode, object Body) CreateErrorResponse(Exception exception, string traceId)
    {
        var isDevelopment = environment.IsDevelopment();
        
        switch (exception)
        {
            case ValidationException validationEx:
                return (
                    (int)HttpStatusCode.BadRequest,
                    new
                    {
                        success = false,
                        message = validationEx.Message,
                        errors = validationEx.ValidationErrors,
                        fieldErrors = validationEx.FieldErrors
                    }
                );

            case AuthenticationException authEx:
                logger.LogWarning("Authentication failed: {Method} from {IP}", 
                    authEx.AuthenticationMethod, authEx.IpAddress);
                return (
                    (int)HttpStatusCode.Unauthorized,
                    ApiResponse.FailureResponse(authEx.Message)
                );

            case AuthorizationException authzEx:
                logger.LogWarning("Authorization failed: User {UserId} accessing {Resource}", 
                    authzEx.UserId, authzEx.RequestedResource);
                return (
                    (int)HttpStatusCode.Forbidden,
                    ApiResponse.FailureResponse(authzEx.Message)
                );

            case DataNotFoundException notFoundEx:
                return (
                    (int)HttpStatusCode.NotFound,
                    ApiResponse.FailureResponse(notFoundEx.Message)
                );

            case BusinessLogicException businessEx:
                return (
                    (int)HttpStatusCode.BadRequest,
                    new
                    {
                        success = false,
                        message = businessEx.Message,
                        errorCode = businessEx.ErrorCode,
                        data = businessEx.ErrorData
                    }
                );

            case ExternalServiceException serviceEx:
                logger.LogError("External service error: {Service} - {Endpoint}", 
                    serviceEx.ServiceName, serviceEx.Endpoint);
                return (
                    (int)HttpStatusCode.BadGateway,
                    ApiResponse.FailureResponse(
                        isDevelopment ? serviceEx.Message : "External service temporarily unavailable")
                );

            case OperationCanceledException:
                return (
                    (int)HttpStatusCode.RequestTimeout,
                    ApiResponse.FailureResponse("Request was cancelled")
                );

            case UnauthorizedAccessException:
                return (
                    (int)HttpStatusCode.Unauthorized,
                    ApiResponse.FailureResponse(ApplicationConstants.ErrorMessages.UnauthorizedAccess)
                );

            case ArgumentException or ArgumentNullException:
                return (
                    (int)HttpStatusCode.BadRequest,
                    ApiResponse.FailureResponse(
                        isDevelopment ? exception.Message : "Invalid request parameters")
                );

            case TimeoutException:
                return (
                    (int)HttpStatusCode.RequestTimeout,
                    ApiResponse.FailureResponse("Request timeout")
                );

            case InvalidOperationException:
                return (
                    (int)HttpStatusCode.BadRequest,
                    ApiResponse.FailureResponse(
                        isDevelopment ? exception.Message : "Invalid operation")
                );

            default:
                var errorId = Guid.NewGuid().ToString();
                logger.LogError(exception, "Unhandled exception. ErrorId: {ErrorId}", errorId);
                
                if (isDevelopment)
                {
                    return (
                        (int)HttpStatusCode.InternalServerError,
                        new
                        {
                            success = false,
                            message = exception.Message,
                            type = exception.GetType().Name,
                            stackTrace = exception.StackTrace,
                            innerException = exception.InnerException?.Message,
                            errorId = errorId,
                            traceId = traceId
                        }
                    );
                }
                else
                {
                    return (
                        (int)HttpStatusCode.InternalServerError,
                        ApiResponse.FailureResponse(
                            ApplicationConstants.ErrorMessages.ServerError)
                    );
                }
        }
    }
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionMiddleware>();
    }
}