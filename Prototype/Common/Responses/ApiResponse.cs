using Prototype.Constants;

namespace Prototype.Common.Responses;

public class ApiResponse
{
    public bool Success { get; }
    public string Message { get; }
    public List<string>? Errors { get; }
    public Dictionary<string, List<string>>? FieldErrors { get; }
    public string? ErrorCode { get; }
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    protected ApiResponse(bool success, string message, List<string>? errors = null, Dictionary<string, List<string>>? fieldErrors = null, string? errorCode = null)
    {
        Success = success;
        Message = message;
        Errors = errors;
        FieldErrors = fieldErrors;
        ErrorCode = errorCode;
    }

    #region Success Responses
    public static ApiResponse SuccessResponse(string? message = null)
        => new ApiResponse(true, message ?? ApplicationConstants.SuccessMessages.OperationSuccess);

    public static ApiResponse CreatedSuccess(string? message = null)
        => new ApiResponse(true, message ?? "Resource created successfully");

    public static ApiResponse UpdatedSuccess(string? message = null)
        => new ApiResponse(true, message ?? "Resource updated successfully");

    public static ApiResponse DeletedSuccess(string? message = null)
        => new ApiResponse(true, message ?? "Resource deleted successfully");
    #endregion

    #region Error Responses
    public static ApiResponse FailureResponse(string message, List<string>? errors = null, Dictionary<string, List<string>>? fieldErrors = null, string? errorCode = null)
        => new ApiResponse(false, message, errors, fieldErrors, errorCode);

    public static ApiResponse BadRequest(string? message = null, List<string>? errors = null)
        => new ApiResponse(false, message ?? ApplicationConstants.ErrorMessages.InvalidRequest, errors);

    public static ApiResponse Unauthorized(string? message = null)
        => new ApiResponse(false, message ?? ApplicationConstants.ErrorMessages.UnauthorizedAccess);

    public static ApiResponse NotFound(string? message = null)
        => new ApiResponse(false, message ?? "Resource not found");

    public static ApiResponse InternalServerError(string? message = null)
        => new ApiResponse(false, message ?? ApplicationConstants.ErrorMessages.ServerError);

    public static ApiResponse ValidationError(List<string> validationErrors, Dictionary<string, List<string>>? fieldErrors = null)
        => new ApiResponse(false, ApplicationConstants.ErrorMessages.InvalidRequest, validationErrors, fieldErrors);
    #endregion

    #region Authentication Responses
    public static ApiResponse LoginSuccess(string? message = null)
        => new ApiResponse(true, message ?? ApplicationConstants.SuccessMessages.LoginSuccess);

    public static ApiResponse LogoutSuccess(string? message = null)
        => new ApiResponse(true, message ?? ApplicationConstants.SuccessMessages.LogoutSuccess);

    public static ApiResponse InvalidCredentials(string? message = null)
        => new ApiResponse(false, message ?? ApplicationConstants.ErrorMessages.InvalidCredentials);
    #endregion
}

public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; }

    private ApiResponse(bool success, T? data, string message, List<string>? errors = null, Dictionary<string, List<string>>? fieldErrors = null, string? errorCode = null)
        : base(success, message, errors, fieldErrors, errorCode)
    {
        Data = data;
    }

    #region Success Responses
    public static ApiResponse<T> Success(T data, string? message = null)
        => new ApiResponse<T>(true, data, message ?? ApplicationConstants.SuccessMessages.OperationSuccess);

    public static ApiResponse<T> CreatedSuccess(T data, string? message = null)
        => new ApiResponse<T>(true, data, message ?? "Resource created successfully");

    public static ApiResponse<T> UpdatedSuccess(T data, string? message = null)
        => new ApiResponse<T>(true, data, message ?? "Resource updated successfully");
    #endregion

    #region Error Responses
    public static ApiResponse<T> Failure(string message, List<string>? errors = null, Dictionary<string, List<string>>? fieldErrors = null, string? errorCode = null)
        => new ApiResponse<T>(false, default, message, errors, fieldErrors, errorCode);

    public static ApiResponse<T> BadRequest(string? message = null, List<string>? errors = null)
        => new ApiResponse<T>(false, default, message ?? ApplicationConstants.ErrorMessages.InvalidRequest, errors);

    public static ApiResponse<T> Unauthorized(string? message = null)
        => new ApiResponse<T>(false, default, message ?? ApplicationConstants.ErrorMessages.UnauthorizedAccess);

    public static ApiResponse<T> NotFound(string? message = null)
        => new ApiResponse<T>(false, default, message ?? "Resource not found");

    public static ApiResponse<T> InternalServerError(string? message = null)
        => new ApiResponse<T>(false, default, message ?? ApplicationConstants.ErrorMessages.ServerError);

    public static ApiResponse<T> ValidationError(List<string> validationErrors, Dictionary<string, List<string>>? fieldErrors = null)
        => new ApiResponse<T>(false, default, ApplicationConstants.ErrorMessages.InvalidRequest, validationErrors, fieldErrors);
    #endregion

    #region Authentication Responses
    public static ApiResponse<T> LoginSuccess(T userData, string? message = null)
        => new ApiResponse<T>(true, userData, message ?? ApplicationConstants.SuccessMessages.LoginSuccess);
    #endregion

    #region Pagination Responses
    public static ApiResponse<object> PaginatedSuccess<TData>(
        IEnumerable<TData> data, 
        int page, 
        int pageSize, 
        int totalCount,
        string? message = null)
    {
        var paginatedData = new
        {
            Data = data,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
        return new ApiResponse<object>(true, paginatedData, message ?? ApplicationConstants.SuccessMessages.OperationSuccess);
    }
    #endregion
}