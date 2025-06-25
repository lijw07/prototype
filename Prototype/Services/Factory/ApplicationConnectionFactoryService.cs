using Prototype.DTOs;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class ApplicationConnectionFactoryService(PasswordEncryptionService encryptionService)
    : IApplicationConnectionFactoryService
{
    public ApplicationConnectionModel CreateApplicationConnection(Guid applicationId, ConnectionSourceDto dto)
    {
        return new ApplicationConnectionModel
        {
            ApplicationConnectionId = Guid.NewGuid(),
            ApplicationId = applicationId,
            Instance = dto.Instance,
            Host = dto.Host,
            Port = dto.Port,
            AuthenticationType = dto.AuthenticationType,
            DatabaseName = dto.DatabaseName,
            Url = dto.Url,
            Username = dto.Username,
            Password = encryptionService.Encrypt(dto.Password ?? string.Empty),
            AuthenticationDatabase = dto.AuthenticationDatabase,
            AwsAccessKeyId = encryptionService.Encrypt(dto.AwsAccessKeyId ?? string.Empty),
            AwsSecretAccessKey = encryptionService.Encrypt(dto.AwsSecretAccessKey ?? string.Empty),
            AwsSessionToken = encryptionService.Encrypt(dto.AwsSessionToken ?? string.Empty),
            Principal = dto.Principal,
            ServiceName = dto.ServiceName,
            ServiceRealm = dto.ServiceRealm,
            CanonicalizeHostName = dto.CanonicalizeHostName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }
}