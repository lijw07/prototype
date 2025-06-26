using Prototype.DTOs;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class ApplicationFactoryService(PasswordEncryptionService encryptionService) : IApplicationFactoryService
{
    public ApplicationModel CreateApplication(Guid applicationGuid, ApplicationRequestDto requestDto)
    {
        return new ApplicationModel
        {
            ApplicationId = applicationGuid,
            ApplicationName = requestDto.ApplicationName,
            ApplicationDescription = requestDto.ApplicationDescription,
            ApplicationDataSourceType = requestDto.DataSourceType,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public ApplicationModel UpdateApplication(ApplicationModel application, ApplicationConnectionModel connectionSource, ApplicationRequestDto requestDto)
    {
        application.ApplicationName = requestDto.ApplicationName;
        application.ApplicationDescription = requestDto.ApplicationDescription;
        application.ApplicationDataSourceType = requestDto.DataSourceType;
        application.UpdatedAt = DateTime.UtcNow;
        
        var conn = connectionSource;
        conn.Instance = requestDto.ConnectionSourceRequest.Instance;
        conn.Host = requestDto.ConnectionSourceRequest.Host;
        conn.Port = requestDto.ConnectionSourceRequest.Port;
        conn.AuthenticationType = requestDto.ConnectionSourceRequest.AuthenticationType;
        conn.DatabaseName = requestDto.ConnectionSourceRequest.DatabaseName;
        conn.Url = requestDto.ConnectionSourceRequest.Url;
        conn.Username = requestDto.ConnectionSourceRequest.Username;
        // Only update password if provided (not empty)
        if (!string.IsNullOrEmpty(requestDto.ConnectionSourceRequest.Password))
        {
            conn.Password = encryptionService.Encrypt(requestDto.ConnectionSourceRequest.Password);
        }
        conn.AuthenticationDatabase = requestDto.ConnectionSourceRequest.AuthenticationDatabase;
        // Only update AWS credentials if provided
        if (!string.IsNullOrEmpty(requestDto.ConnectionSourceRequest.AwsAccessKeyId))
        {
            conn.AwsAccessKeyId = encryptionService.Encrypt(requestDto.ConnectionSourceRequest.AwsAccessKeyId);
        }
        if (!string.IsNullOrEmpty(requestDto.ConnectionSourceRequest.AwsSecretAccessKey))
        {
            conn.AwsSecretAccessKey = encryptionService.Encrypt(requestDto.ConnectionSourceRequest.AwsSecretAccessKey);
        }
        if (!string.IsNullOrEmpty(requestDto.ConnectionSourceRequest.AwsSessionToken))
        {
            conn.AwsSessionToken = encryptionService.Encrypt(requestDto.ConnectionSourceRequest.AwsSessionToken);
        }
        conn.Principal = requestDto.ConnectionSourceRequest.Principal;
        conn.ServiceName = requestDto.ConnectionSourceRequest.ServiceName;
        conn.ServiceRealm = requestDto.ConnectionSourceRequest.ServiceRealm;
        conn.CanonicalizeHostName = requestDto.ConnectionSourceRequest.CanonicalizeHostName;
        conn.UpdatedAt = DateTime.UtcNow;

        return application;
    }
}