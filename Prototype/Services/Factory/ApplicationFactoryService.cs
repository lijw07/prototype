using Prototype.DTOs;
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
}