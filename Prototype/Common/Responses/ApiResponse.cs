namespace Prototype.Common.Responses;
public class ApiResponse
{
    public bool Success { get; }
    public string Message { get; }
    public List<string>? Errors { get; }
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    protected ApiResponse(bool success, string message, List<string>? errors)
    {
        Success = success;
        Message = message;
        Errors = errors;
    }

    public static ApiResponse SuccessResponse(string message = "Operation completed successfully")
        => new ApiResponse(true, message, null);

    public static ApiResponse FailureResponse(string message, List<string>? errors = null)
        => new ApiResponse(false, message, errors);
}

public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; }

    private ApiResponse(bool success, T? data, string message, List<string>? errors)
        : base(success, message, errors)
    {
        Data = data;
    }

    public static new ApiResponse<T> Success(T data, string message = "Operation completed successfully")
        => new ApiResponse<T>(true, data, message, null);

    public static ApiResponse<T> Failure(string message, List<string>? errors = null)
        => new ApiResponse<T>(false, default, message, errors);
}