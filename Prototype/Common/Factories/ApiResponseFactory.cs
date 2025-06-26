using Prototype.Common.Responses;
using Prototype.Constants;

namespace Prototype.Common.Factories
{
    public static class ApiResponseFactory
    {
        #region Success Responses

        public static ApiResponse Success(string? message = null)
        {
            var successMessage = message ?? ApplicationConstants.SuccessMessages.OperationSuccess;
            return ApiResponse.SuccessResponse(successMessage);
        }

        public static ApiResponse<T> Success<T>(T data, string? message = null)
        {
            var successMessage = message ?? ApplicationConstants.SuccessMessages.OperationSuccess;
            return ApiResponse<T>.Success(data, successMessage);
        }

        public static ApiResponse<T> CreatedSuccess<T>(T data, string? message = null)
        {
            var successMessage = message ?? "Resource created successfully";
            return ApiResponse<T>.Success(data, successMessage);
        }

        public static ApiResponse<T> UpdatedSuccess<T>(T data, string? message = null)
        {
            var successMessage = message ?? "Resource updated successfully";
            return ApiResponse<T>.Success(data, successMessage);
        }

        public static ApiResponse DeletedSuccess(string? message = null)
        {
            var successMessage = message ?? "Resource deleted successfully";
            return ApiResponse.SuccessResponse(successMessage);
        }

        #endregion

        #region Error Responses

        public static ApiResponse BadRequest(string? message = null, List<string>? errors = null)
        {
            var errorMessage = message ?? ApplicationConstants.ErrorMessages.InvalidRequest;
            return ApiResponse.FailureResponse(errorMessage, errors);
        }

        public static ApiResponse<T> BadRequest<T>(string? message = null, List<string>? errors = null)
        {
            var errorMessage = message ?? ApplicationConstants.ErrorMessages.InvalidRequest;
            return ApiResponse<T>.Failure(errorMessage, errors);
        }

        public static ApiResponse Unauthorized(string? message = null)
        {
            var errorMessage = message ?? ApplicationConstants.ErrorMessages.UnauthorizedAccess;
            return ApiResponse.FailureResponse(errorMessage);
        }

        public static ApiResponse<T> Unauthorized<T>(string? message = null)
        {
            var errorMessage = message ?? ApplicationConstants.ErrorMessages.UnauthorizedAccess;
            return ApiResponse<T>.Failure(errorMessage);
        }

        public static ApiResponse NotFound(string? message = null)
        {
            var errorMessage = message ?? "Resource not found";
            return ApiResponse.FailureResponse(errorMessage);
        }

        public static ApiResponse<T> NotFound<T>(string? message = null)
        {
            var errorMessage = message ?? "Resource not found";
            return ApiResponse<T>.Failure(errorMessage);
        }

        public static ApiResponse InternalServerError(string? message = null)
        {
            var errorMessage = message ?? ApplicationConstants.ErrorMessages.ServerError;
            return ApiResponse.FailureResponse(errorMessage);
        }

        public static ApiResponse<T> InternalServerError<T>(string? message = null)
        {
            var errorMessage = message ?? ApplicationConstants.ErrorMessages.ServerError;
            return ApiResponse<T>.Failure(errorMessage);
        }

        #endregion

        #region Validation Responses

        public static ApiResponse ValidationError(List<string> validationErrors)
        {
            return ApiResponse.FailureResponse(ApplicationConstants.ErrorMessages.InvalidRequest, validationErrors);
        }

        public static ApiResponse<T> ValidationError<T>(List<string> validationErrors)
        {
            return ApiResponse<T>.Failure(ApplicationConstants.ErrorMessages.InvalidRequest, validationErrors);
        }

        #endregion

        #region Authentication Responses

        public static ApiResponse<T> LoginSuccess<T>(T userData, string? message = null)
        {
            var successMessage = message ?? ApplicationConstants.SuccessMessages.LoginSuccess;
            return ApiResponse<T>.Success(userData, successMessage);
        }

        public static ApiResponse LogoutSuccess(string? message = null)
        {
            var successMessage = message ?? ApplicationConstants.SuccessMessages.LogoutSuccess;
            return ApiResponse.SuccessResponse(successMessage);
        }

        public static ApiResponse InvalidCredentials(string? message = null)
        {
            var errorMessage = message ?? ApplicationConstants.ErrorMessages.InvalidCredentials;
            return ApiResponse.FailureResponse(errorMessage);
        }

        #endregion

        #region Pagination Responses

        public static ApiResponse<object> PaginatedSuccess<T>(
            IEnumerable<T> data, 
            int page, 
            int pageSize, 
            int totalCount,
            string? message = null)
        {
            var successMessage = message ?? ApplicationConstants.SuccessMessages.OperationSuccess;
            var paginatedData = new
            {
                Data = data,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
            return ApiResponse<object>.Success(paginatedData, successMessage);
        }

        #endregion
    }
}