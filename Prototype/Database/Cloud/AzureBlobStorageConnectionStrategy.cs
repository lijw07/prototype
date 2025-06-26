using Azure.Storage.Blobs;
using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;

namespace Prototype.Database.Cloud;

public class AzureBlobStorageConnectionStrategy(
    PasswordEncryptionService encryptionService,
    ILogger<AzureBlobStorageConnectionStrategy> logger)
    : IFileConnectionStrategy
{
    public DataSourceTypeEnum ConnectionType => DataSourceTypeEnum.AzureBlobStorage;

    public Dictionary<AuthenticationTypeEnum, bool> GetSupportedAuthTypes()
    {
        return new Dictionary<AuthenticationTypeEnum, bool>
        {
            { AuthenticationTypeEnum.AzureStorageKey, true },
            { AuthenticationTypeEnum.AzureSas, true },
            { AuthenticationTypeEnum.AzureAdDefault, true },
            { AuthenticationTypeEnum.NoAuth, false } // Azure Storage always requires credentials
        };
    }

    public async Task<object> ReadDataAsync(ConnectionSourceRequestDto sourceRequest)
    {
        try
        {
            var blobClient = CreateBlobClient(sourceRequest);
            var containerName = ExtractContainerName(sourceRequest.FilePath);
            var blobName = ExtractBlobName(sourceRequest.FilePath);

            var containerClient = blobClient.GetBlobContainerClient(containerName);
            var blobClientFile = containerClient.GetBlobClient(blobName);

            var response = await blobClientFile.DownloadAsync();
            using var reader = new StreamReader(response.Value.Content);
            var content = await reader.ReadToEndAsync();

            var properties = await blobClientFile.GetPropertiesAsync();

            return new
            {
                ContainerName = containerName,
                BlobName = blobName,
                ContentLength = properties.Value.ContentLength,
                LastModified = properties.Value.LastModified,
                ContentType = properties.Value.ContentType,
                Content = content,
                ETag = properties.Value.ETag.ToString()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read Azure Blob: {FilePath}", sourceRequest.FilePath);
            throw;
        }
    }

    public async Task<object> ReadDataAsync(ApplicationConnectionModel source)
    {
        var dto = MapToDto(source);
        return await ReadDataAsync(dto);
    }

    public async Task<bool> TestConnectionAsync(ConnectionSourceRequestDto sourceRequest)
    {
        try
        {
            var blobClient = CreateBlobClient(sourceRequest);

            if (!string.IsNullOrEmpty(sourceRequest.FilePath))
            {
                // Test specific blob access
                var containerName = ExtractContainerName(sourceRequest.FilePath);
                var blobName = ExtractBlobName(sourceRequest.FilePath);

                var containerClient = blobClient.GetBlobContainerClient(containerName);
                var blobClientFile = containerClient.GetBlobClient(blobName);

                var exists = await blobClientFile.ExistsAsync();
                return exists.Value;
            }
            else
            {
                // Test general account access by listing containers
                var containers = blobClient.GetBlobContainersAsync();
                var enumerator = containers.GetAsyncEnumerator();
                try
                {
                    if (await enumerator.MoveNextAsync())
                    {
                        // If we can enumerate at least one container, the connection is good
                        return true;
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync();
                }
                return true; // Empty account is still a valid connection
            }
        }
        catch (Azure.RequestFailedException azEx)
        {
            logger.LogError(azEx, "Azure Blob Storage connection test failed: {StatusCode} - {Message}", azEx.Status, azEx.Message);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Azure Blob Storage connection test failed: {Error}", ex.Message);
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync(ApplicationConnectionModel source)
    {
        var dto = MapToDto(source);
        return await TestConnectionAsync(dto);
    }

    public string GetConnectionDescription()
    {
        return "Azure Blob Storage connection with Storage Key, SAS Token, and Azure AD authentication support";
    }

    private BlobServiceClient CreateBlobClient(ConnectionSourceRequestDto sourceRequest)
    {
        switch (sourceRequest.AuthenticationType)
        {
            case AuthenticationTypeEnum.AzureStorageKey:
                if (string.IsNullOrEmpty(sourceRequest.AzureStorageAccountName) || string.IsNullOrEmpty(sourceRequest.AzureStorageAccountKey))
                    throw new ArgumentException("Azure Storage Account Name and Key are required for Storage Key authentication.");
                
                var connectionString = $"DefaultEndpointsProtocol=https;AccountName={sourceRequest.AzureStorageAccountName};AccountKey={sourceRequest.AzureStorageAccountKey};EndpointSuffix=core.windows.net";
                return new BlobServiceClient(connectionString);

            case AuthenticationTypeEnum.AzureSas:
                if (string.IsNullOrEmpty(sourceRequest.AzureSasToken))
                    throw new ArgumentException("Azure SAS Token is required for SAS authentication.");
                
                var sasUri = new Uri(sourceRequest.AzureSasToken);
                return new BlobServiceClient(sasUri);

            case AuthenticationTypeEnum.AzureAdDefault:
                if (string.IsNullOrEmpty(sourceRequest.AzureStorageAccountName))
                    throw new ArgumentException("Azure Storage Account Name is required for Azure AD authentication.");
                
                var accountUri = new Uri($"https://{sourceRequest.AzureStorageAccountName}.blob.core.windows.net");
                return new BlobServiceClient(accountUri, new Azure.Identity.DefaultAzureCredential());

            default:
                throw new NotSupportedException($"Authentication type '{sourceRequest.AuthenticationType}' is not supported for Azure Blob Storage.");
        }
    }

    private string ExtractContainerName(string? blobPath)
    {
        if (string.IsNullOrEmpty(blobPath))
            throw new ArgumentException("Blob path is required");

        // Handle https://accountname.blob.core.windows.net/container/blob format
        if (blobPath.StartsWith("https://"))
        {
            var uri = new Uri(blobPath);
            var pathParts = uri.AbsolutePath.TrimStart('/').Split('/', 2);
            return pathParts[0];
        }

        // Handle container/blob format
        var parts = blobPath.Split('/', 2);
        return parts[0];
    }

    private string ExtractBlobName(string? blobPath)
    {
        if (string.IsNullOrEmpty(blobPath))
            throw new ArgumentException("Blob path is required");

        // Handle https://accountname.blob.core.windows.net/container/blob format
        if (blobPath.StartsWith("https://"))
        {
            var uri = new Uri(blobPath);
            var pathParts = uri.AbsolutePath.TrimStart('/').Split('/', 2);
            return pathParts.Length > 1 ? pathParts[1] : "";
        }

        // Handle container/blob format
        var parts = blobPath.Split('/', 2);
        return parts.Length > 1 ? parts[1] : "";
    }

    private ConnectionSourceRequestDto MapToDto(ApplicationConnectionModel source)
    {
        return new ConnectionSourceRequestDto
        {
            Host = source.Host,
            Port = source.Port,
            Url = source.Url,
            AuthenticationType = source.AuthenticationType,
            Username = source.Username,
            Password = string.IsNullOrEmpty(source.Password) ? null : encryptionService.Decrypt(source.Password),
            AzureStorageAccountName = source.AzureStorageAccountName,
            AzureStorageAccountKey = string.IsNullOrEmpty(source.AzureStorageAccountKey) ? null : encryptionService.Decrypt(source.AzureStorageAccountKey),
            AzureSasToken = string.IsNullOrEmpty(source.AzureSasToken) ? null : encryptionService.Decrypt(source.AzureSasToken),
            FilePath = source.FilePath,
            FileFormat = source.FileFormat,
            Delimiter = source.Delimiter,
            Encoding = source.Encoding,
            HasHeader = source.HasHeader,
            CustomProperties = source.CustomProperties
        };
    }
}