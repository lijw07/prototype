using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.Models;

namespace Prototype.Services.Common;

public interface IMappingService
{
    // User mappings
    UserDto MapToUserDto(UserModel user);
    UserModel MapToUserModel(RegisterRequestDto dto);
    void MapUpdateUserRequest(UpdateUserRequestDto dto, UserModel user);
    
    // Role mappings
    RoleDto MapToRoleDto(UserRoleModel role);
    
    // Application mappings
    object MapToApplicationDto(ApplicationModel application, ApplicationConnectionModel connection);
    
    // Audit log mappings
    object MapToAuditLogDto(AuditLogModel auditLog);
    object MapToUserActivityLogDto(UserActivityLogModel activityLog);
    
    // Bulk operations
    List<TDto> MapToList<TModel, TDto>(IEnumerable<TModel> models, Func<TModel, TDto> mapper);
}

public class MappingService : IMappingService
{
    public UserDto MapToUserDto(UserModel user)
    {
        return new UserDto
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.Username,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    public UserModel MapToUserModel(RegisterRequestDto dto)
    {
        return new UserModel
        {
            UserId = Guid.NewGuid(),
            FirstName = dto.FirstName ?? string.Empty,
            LastName = dto.LastName ?? string.Empty,
            Username = dto.Username,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber ?? string.Empty,
            Role = "User", // Default role
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void MapUpdateUserRequest(UpdateUserRequestDto dto, UserModel user)
    {
        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Username = dto.Username;
        user.Email = dto.Email;
        user.PhoneNumber = dto.PhoneNumber ?? string.Empty;
        user.Role = dto.Role;
        user.IsActive = dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
    }

    public RoleDto MapToRoleDto(UserRoleModel role)
    {
        return new RoleDto
        {
            UserRoleId = role.UserRoleId,
            Role = role.RoleName,
            CreatedAt = role.CreatedAt,
            CreatedBy = role.CreatedBy
        };
    }

    public object MapToApplicationDto(ApplicationModel application, ApplicationConnectionModel connection)
    {
        return new
        {
            application.ApplicationId,
            application.ApplicationName,
            application.ApplicationDescription,
            application.ApplicationDataSourceType,
            application.CreatedAt,
            application.UpdatedAt,
            Connection = new
            {
                host = connection.Host,
                port = connection.Port,
                databaseName = connection.DatabaseName,
                authenticationType = connection.AuthenticationType.ToString(),
                username = connection.Username,
                authenticationDatabase = connection.AuthenticationDatabase,
                awsAccessKeyId = connection.AwsAccessKeyId,
                principal = connection.Principal,
                serviceName = connection.ServiceName,
                serviceRealm = connection.ServiceRealm,
                canonicalizeHostName = connection.CanonicalizeHostName
                // Note: Password and secret keys are intentionally not returned for security
            }
        };
    }

    public object MapToAuditLogDto(AuditLogModel auditLog)
    {
        return new
        {
            AuditLogId = auditLog.AuditLogId,
            UserId = auditLog.UserId,
            Username = auditLog.User?.Username ?? "Unknown User",
            ActionType = auditLog.ActionType,
            Metadata = auditLog.Metadata,
            CreatedAt = auditLog.CreatedAt
        };
    }

    public object MapToUserActivityLogDto(UserActivityLogModel activityLog)
    {
        return new
        {
            UserActivityLogId = activityLog.UserActivityLogId,
            UserId = activityLog.UserId,
            Username = activityLog.User?.Username ?? "Unknown User",
            ActionType = activityLog.ActionType,
            Description = activityLog.Description,
            IpAddress = activityLog.IpAddress,
            DeviceInformation = activityLog.DeviceInformation,
            Timestamp = activityLog.Timestamp
        };
    }

    public List<TDto> MapToList<TModel, TDto>(IEnumerable<TModel> models, Func<TModel, TDto> mapper)
    {
        return models.Select(mapper).ToList();
    }
}

// Extension methods for common mapping scenarios
public static class MappingExtensions
{
    public static UserDto ToDto(this UserModel user)
    {
        var mappingService = new MappingService();
        return mappingService.MapToUserDto(user);
    }

    public static RoleDto ToDto(this UserRoleModel role)
    {
        var mappingService = new MappingService();
        return mappingService.MapToRoleDto(role);
    }

    public static List<UserDto> ToDto(this IEnumerable<UserModel> users)
    {
        var mappingService = new MappingService();
        return users.Select(mappingService.MapToUserDto).ToList();
    }

    public static List<RoleDto> ToDto(this IEnumerable<UserRoleModel> roles)
    {
        var mappingService = new MappingService();
        return roles.Select(mappingService.MapToRoleDto).ToList();
    }
}