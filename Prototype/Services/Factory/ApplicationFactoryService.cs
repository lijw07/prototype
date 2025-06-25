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
        conn.Instance = requestDto.ConnectionSource.Instance;
        conn.Host = requestDto.ConnectionSource.Host;
        conn.Port = requestDto.ConnectionSource.Port;
        conn.AuthenticationType = requestDto.ConnectionSource.AuthenticationType;
        conn.DatabaseName = requestDto.ConnectionSource.DatabaseName;
        conn.Url = requestDto.ConnectionSource.Url;
        conn.Username = requestDto.ConnectionSource.Username;
        // Only update password if provided (not empty)
        if (!string.IsNullOrEmpty(requestDto.ConnectionSource.Password))
        {
            conn.Password = encryptionService.Encrypt(requestDto.ConnectionSource.Password);
        }
        conn.AuthenticationDatabase = requestDto.ConnectionSource.AuthenticationDatabase;
        // Only update AWS credentials if provided
        if (!string.IsNullOrEmpty(requestDto.ConnectionSource.AwsAccessKeyId))
        {
            conn.AwsAccessKeyId = encryptionService.Encrypt(requestDto.ConnectionSource.AwsAccessKeyId);
        }
        if (!string.IsNullOrEmpty(requestDto.ConnectionSource.AwsSecretAccessKey))
        {
            conn.AwsSecretAccessKey = encryptionService.Encrypt(requestDto.ConnectionSource.AwsSecretAccessKey);
        }
        if (!string.IsNullOrEmpty(requestDto.ConnectionSource.AwsSessionToken))
        {
            conn.AwsSessionToken = encryptionService.Encrypt(requestDto.ConnectionSource.AwsSessionToken);
        }
        conn.Principal = requestDto.ConnectionSource.Principal;
        conn.ServiceName = requestDto.ConnectionSource.ServiceName;
        conn.ServiceRealm = requestDto.ConnectionSource.ServiceRealm;
        conn.CanonicalizeHostName = requestDto.ConnectionSource.CanonicalizeHostName;
        conn.UpdatedAt = DateTime.UtcNow;

        return application;
    }
}