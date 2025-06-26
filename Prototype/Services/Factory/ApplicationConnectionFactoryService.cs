using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class ApplicationConnectionFactoryService(PasswordEncryptionService encryptionService)
    : IApplicationConnectionFactoryService
{
    public ApplicationConnectionModel CreateApplicationConnection(Guid applicationId, ConnectionSourceRequestDto requestDto)
    {
        return new ApplicationConnectionModel
        {
            ApplicationConnectionId = Guid.NewGuid(),
            ApplicationId = applicationId,
            Instance = requestDto.Instance,
            Host = requestDto.Host,
            Port = requestDto.Port,
            AuthenticationType = requestDto.AuthenticationType,
            DatabaseName = requestDto.DatabaseName,
            Url = requestDto.Url,
            Username = requestDto.Username,
            Password = encryptionService.Encrypt(requestDto.Password ?? string.Empty),
            AuthenticationDatabase = requestDto.AuthenticationDatabase,
            AwsAccessKeyId = encryptionService.Encrypt(requestDto.AwsAccessKeyId ?? string.Empty),
            AwsSecretAccessKey = encryptionService.Encrypt(requestDto.AwsSecretAccessKey ?? string.Empty),
            AwsSessionToken = encryptionService.Encrypt(requestDto.AwsSessionToken ?? string.Empty),
            Principal = requestDto.Principal,
            ServiceName = requestDto.ServiceName,
            ServiceRealm = requestDto.ServiceRealm,
            CanonicalizeHostName = requestDto.CanonicalizeHostName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }
}