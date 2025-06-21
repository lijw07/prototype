using Prototype.DTOs;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class ApplicationFactoryService : IApplicationFactoryService
{
    private readonly PasswordEncryptionService _encryptionService;

    public ApplicationFactoryService(PasswordEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

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
        conn.Password = _encryptionService.Encrypt(requestDto.ConnectionSource.Password ?? string.Empty);
        conn.AuthenticationDatabase = requestDto.ConnectionSource.AuthenticationDatabase;
        conn.AwsAccessKeyId = _encryptionService.Encrypt(requestDto.ConnectionSource.AwsAccessKeyId ?? string.Empty);
        conn.AwsSecretAccessKey = _encryptionService.Encrypt(requestDto.ConnectionSource.AwsSecretAccessKey ?? string.Empty);
        conn.AwsSessionToken = _encryptionService.Encrypt(requestDto.ConnectionSource.AwsSessionToken ?? string.Empty);
        conn.Principal = requestDto.ConnectionSource.Principal;
        conn.ServiceName = requestDto.ConnectionSource.ServiceName;
        conn.ServiceRealm = requestDto.ConnectionSource.ServiceRealm;
        conn.CanonicalizeHostName = requestDto.ConnectionSource.CanonicalizeHostName;
        conn.UpdatedAt = DateTime.UtcNow;

        return application;
    }
}