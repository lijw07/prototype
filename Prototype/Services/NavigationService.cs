using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Models;
using Prototype.Constants;
using Prototype.Utility;

namespace Prototype.Services
{
    public class NavigationService : INavigationService
    {
        private readonly SentinelContext _context;
        private readonly IAuthenticatedUserAccessor _userAccessor;
        private readonly ILogger<NavigationService> _logger;

        public NavigationService(
            SentinelContext context,
            IAuthenticatedUserAccessor userAccessor,
            ILogger<NavigationService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UserModel?> GetCurrentUserAsync(ClaimsPrincipal user)
        {
            return await _userAccessor.GetCurrentUserAsync(user);
        }

        public async Task<(IEnumerable<T> data, int totalCount)> GetPaginatedDataAsync<T>(
            IQueryable<T> query, 
            int page, 
            int pageSize)
        {
            var validatedParams = ValidatePaginationParameters(page, pageSize);
            page = validatedParams.page;
            pageSize = validatedParams.pageSize;
            var skip = validatedParams.skip;

            var totalCount = await query.CountAsync();
            var data = await query.Skip(skip).Take(pageSize).ToListAsync();

            return (data, totalCount);
        }

        public async Task<object> CreatePaginatedResponseAsync<T>(
            IQueryable<T> query, 
            int page, 
            int pageSize)
        {
            var (data, totalCount) = await GetPaginatedDataAsync(query, page, pageSize);
            var validatedParams = ValidatePaginationParameters(page, pageSize);
            
            return new
            {
                Data = data,
                Page = validatedParams.page,
                PageSize = validatedParams.pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / validatedParams.pageSize)
            };
        }

        public UserDto MapUserToDto(UserModel user)
        {
            return new UserDto
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                Role = user.Role,
                LastLogin = user.LastLogin,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<UserModel?> GetUserByIdAsync(Guid userId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<ApplicationModel?> GetApplicationByIdAsync(Guid applicationId)
        {
            return await _context.Applications.FirstOrDefaultAsync(a => a.ApplicationId == applicationId);
        }

        public async Task<bool> ValidateUserApplicationAccessAsync(Guid userId, Guid applicationId)
        {
            return await _context.UserApplications
                .AnyAsync(ua => ua.UserId == userId && ua.ApplicationId == applicationId);
        }

        public async Task<IEnumerable<UserDto>> GetUsersForCurrentUserAsync(UserModel currentUser)
        {
            // Business logic: users can typically see other users in their organization
            var users = await _context.Users
                .Where(u => u.IsActive) // Example business rule
                .ToListAsync();

            return users.Select(MapUserToDto);
        }

        public async Task<IEnumerable<ApplicationModel>> GetApplicationsForUserAsync(Guid userId)
        {
            return await _context.UserApplications
                .Where(ua => ua.UserId == userId)
                .Include(ua => ua.Application)
                .Select(ua => ua.Application!)
                .ToListAsync();
        }

        public async Task<object> GetAuditLogsPagedAsync(int page, int pageSize)
        {
            var query = _context.AuditLogs
                .Include(log => log.User)
                .OrderByDescending(log => log.CreatedAt)
                .Select(log => new
                {
                    AuditLogId = log.AuditLogId,
                    UserId = log.UserId,
                    Username = log.User != null ? log.User.Username : "Unknown User",
                    ActionType = log.ActionType,
                    Metadata = log.Metadata,
                    CreatedAt = log.CreatedAt
                });

            return await CreatePaginatedResponseAsync(query, page, pageSize);
        }

        public async Task<object> GetUserActivityLogsPagedAsync(int page, int pageSize)
        {
            var query = _context.UserActivityLogs
                .Include(log => log.User)
                .OrderByDescending(log => log.Timestamp)
                .Select(log => new
                {
                    log.UserActivityLogId,
                    log.UserId,
                    Username = log.User != null ? log.User.Username : "Unknown User",
                    log.IpAddress,
                    log.DeviceInformation,
                    log.ActionType,
                    log.Description,
                    log.Timestamp
                });

            return await CreatePaginatedResponseAsync(query, page, pageSize);
        }

        public async Task<object> GetApplicationLogsPagedAsync(int page, int pageSize)
        {
            var query = _context.ApplicationLogs
                .Include(log => log.Application)
                .OrderByDescending(log => log.CreatedAt)
                .Select(log => new
                {
                    log.ApplicationLogId,
                    log.ApplicationId,
                    ApplicationName = log.Application != null ? log.Application.ApplicationName : "Unknown Application",
                    log.ActionType,
                    log.Metadata,
                    log.CreatedAt,
                    log.UpdatedAt
                });

            return await CreatePaginatedResponseAsync(query, page, pageSize);
        }

        public (string ipAddress, string deviceInfo) GetClientInformation(HttpContext httpContext)
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? ApplicationConstants.DefaultIpAddress;
            var deviceInfo = httpContext.Request.Headers.UserAgent.ToString() ?? ApplicationConstants.DefaultDeviceInfo;
            return (ipAddress, deviceInfo);
        }

        public async Task<bool> IsUserAuthorizedForOperationAsync(UserModel user, string operation)
        {
            // Example authorization logic - can be expanded based on business requirements
            switch (operation.ToLower())
            {
                case "delete_user":
                case "manage_applications":
                    return user.Role == "Admin" || user.Role == "SuperAdmin";
                case "view_audit_logs":
                    return user.Role == "Admin" || user.Role == "SuperAdmin" || user.Role == "Auditor";
                case "modify_profile":
                    return true; // All authenticated users can modify their own profile
                default:
                    return false;
            }
        }

        private static (int page, int pageSize, int skip) ValidatePaginationParameters(int page, int pageSize)
        {
            if (page < ApplicationConstants.Pagination.DefaultPage) 
                page = ApplicationConstants.Pagination.DefaultPage;
            
            if (pageSize < ApplicationConstants.Pagination.MinPageSize || 
                pageSize > ApplicationConstants.Pagination.MaxPageSize) 
                pageSize = ApplicationConstants.Pagination.DefaultPageSize;
            
            var skip = (page - 1) * pageSize;
            return (page, pageSize, skip);
        }
    }
}