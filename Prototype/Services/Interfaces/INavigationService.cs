using System.Security.Claims;
using Prototype.DTOs;
using Prototype.Models;

namespace Prototype.Services
{
    public interface INavigationService
    {
        /// <summary>
        /// Gets the current authenticated user from claims principal
        /// </summary>
        Task<UserModel?> GetCurrentUserAsync(ClaimsPrincipal user);

        /// <summary>
        /// Gets paginated data from any queryable source
        /// </summary>
        Task<(IEnumerable<T> data, int totalCount)> GetPaginatedDataAsync<T>(IQueryable<T> query, int page, int pageSize);

        /// <summary>
        /// Creates a standardized paginated response object
        /// </summary>
        Task<object> CreatePaginatedResponseAsync<T>(IQueryable<T> query, int page, int pageSize);

        /// <summary>
        /// Maps UserModel to UserDto consistently
        /// </summary>
        UserDto MapUserToDto(UserModel user);

        /// <summary>
        /// Gets a user by ID
        /// </summary>
        Task<UserModel?> GetUserByIdAsync(Guid userId);

        /// <summary>
        /// Gets an application by ID
        /// </summary>
        Task<ApplicationModel?> GetApplicationByIdAsync(Guid applicationId);

        /// <summary>
        /// Validates if a user has access to a specific application
        /// </summary>
        Task<bool> ValidateUserApplicationAccessAsync(Guid userId, Guid applicationId);

        /// <summary>
        /// Gets all users visible to the current user (based on business rules)
        /// </summary>
        Task<IEnumerable<UserDto>> GetUsersForCurrentUserAsync(UserModel currentUser);

        /// <summary>
        /// Gets all applications accessible to a user
        /// </summary>
        Task<IEnumerable<ApplicationModel>> GetApplicationsForUserAsync(Guid userId);

        /// <summary>
        /// Gets paginated audit logs with user information
        /// </summary>
        Task<object> GetAuditLogsPagedAsync(int page, int pageSize);

        /// <summary>
        /// Gets paginated user activity logs with user information
        /// </summary>
        Task<object> GetUserActivityLogsPagedAsync(int page, int pageSize);

        /// <summary>
        /// Gets paginated application logs with application information
        /// </summary>
        Task<object> GetApplicationLogsPagedAsync(int page, int pageSize);

        /// <summary>
        /// Extracts client IP address and device information from HTTP context
        /// </summary>
        (string ipAddress, string deviceInfo) GetClientInformation(HttpContext httpContext);

        /// <summary>
        /// Checks if a user is authorized to perform a specific operation
        /// </summary>
        Task<bool> IsUserAuthorizedForOperationAsync(UserModel user, string operation);
    }
}