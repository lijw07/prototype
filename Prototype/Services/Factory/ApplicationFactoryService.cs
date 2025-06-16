using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class ApplicationFactoryService : IApplicationFactoryService
{
    public ApplicationModel CreateApplication(ApplicationRequestDto requestDto)
    {
        
        var applicationGuid = Guid.NewGuid();
        
        var connectionSource = new ApplicationConnectionModel
        {
            ApplicationConnectionId = Guid.NewGuid(),
            ApplicationId = applicationGuid,
            Instance = requestDto.ConnectionSource.Instance,
            Host = requestDto.ConnectionSource.Host,
            Port = requestDto.ConnectionSource.Port,
            AuthenticationType = requestDto.ConnectionSource.AuthenticationType,
            DatabaseName = requestDto.ConnectionSource.DatabaseName,
            Url = requestDto.ConnectionSource.Url,
            Username = requestDto.ConnectionSource.Username,
            Password = requestDto.ConnectionSource.Password,
            AuthenticationDatabase = requestDto.ConnectionSource.AuthenticationDatabase,
            AwsAccessKeyId = requestDto.ConnectionSource.AwsAccessKeyId,
            AwsSecretAccessKey = requestDto.ConnectionSource.AwsSecretAccessKey,
            AwsSessionToken = requestDto.ConnectionSource.AwsSessionToken,
            Principal = requestDto.ConnectionSource.Principal,
            ServiceName = requestDto.ConnectionSource.ServiceName,
            ServiceRealm = requestDto.ConnectionSource.ServiceRealm,
            CanonicalizeHostName = requestDto.ConnectionSource.CanonicalizeHostName,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        
        return new ApplicationModel
        {
            ApplicationId = applicationGuid,
            ApplicationName = requestDto.ApplicationName,
            ApplicationDescription = requestDto.ApplicationDescription,
            ApplicationDataSourceType = requestDto.DataSourceType,
            ApplicationConnections = connectionSource,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    public ApplicationModel UpdateApplication(ApplicationModel application, ApplicationRequestDto requestDto)
    {
        application.ApplicationName = requestDto.ApplicationName;
        application.ApplicationDescription = requestDto.ApplicationDescription;
        application.ApplicationDataSourceType = requestDto.DataSourceType;
        application.UpdatedAt = DateTime.Now;
        
        var conn = application.ApplicationConnections;

        conn.Instance = requestDto.ConnectionSource.Instance;
        conn.Host = requestDto.ConnectionSource.Host;
        conn.Port = requestDto.ConnectionSource.Port;
        conn.AuthenticationType = requestDto.ConnectionSource.AuthenticationType;
        conn.DatabaseName = requestDto.ConnectionSource.DatabaseName;
        conn.Url = requestDto.ConnectionSource.Url;
        conn.Username = requestDto.ConnectionSource.Username;
        conn.Password = requestDto.ConnectionSource.Password;
        conn.AuthenticationDatabase = requestDto.ConnectionSource.AuthenticationDatabase;
        conn.AwsAccessKeyId = requestDto.ConnectionSource.AwsAccessKeyId;
        conn.AwsSecretAccessKey = requestDto.ConnectionSource.AwsSecretAccessKey;
        conn.AwsSessionToken = requestDto.ConnectionSource.AwsSessionToken;
        conn.Principal = requestDto.ConnectionSource.Principal;
        conn.ServiceName = requestDto.ConnectionSource.ServiceName;
        conn.ServiceRealm = requestDto.ConnectionSource.ServiceRealm;
        conn.CanonicalizeHostName = requestDto.ConnectionSource.CanonicalizeHostName;
        conn.UpdatedAt = DateTime.Now;

        return application;
    }
}