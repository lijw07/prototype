using Google.Cloud.Storage.V1;
using Google.Apis.Auth.OAuth2;
using Google;
using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;
using System.Text;
using Prototype.DTOs.Request;

namespace Prototype.Database.Cloud;

public class GoogleCloudStorageConnectionStrategy(
    PasswordEncryptionService encryptionService,
    ILogger<GoogleCloudStorageConnectionStrategy> logger)
    : IFileConnectionStrategy
{
    public DataSourceTypeEnum ConnectionType => DataSourceTypeEnum.GoogleCloudStorage;

    public Dictionary<AuthenticationTypeEnum, bool> GetSupportedAuthTypes()
    {
        return new Dictionary<AuthenticationTypeEnum, bool>
        {
            { AuthenticationTypeEnum.ServicePrincipal, true },
            { AuthenticationTypeEnum.AccessKey, true },
            { AuthenticationTypeEnum.NoAuth, false } // GCS always requires credentials
        };
    }

    public async Task<object> ReadDataAsync(ConnectionSourceRequestDto sourceRequest)
    {
        try
        {
            var storageClient = await CreateStorageClientAsync(sourceRequest);
            var bucketName = ExtractBucketName(sourceRequest.FilePath);
            var objectName = ExtractObjectName(sourceRequest.FilePath);

            var gcsObject = await storageClient.GetObjectAsync(bucketName, objectName);
            
            using var stream = new MemoryStream();
            await storageClient.DownloadObjectAsync(gcsObject, stream);
            
            var content = Encoding.UTF8.GetString(stream.ToArray());

            return new
            {
                BucketName = bucketName,
                ObjectName = objectName,
                Size = gcsObject.Size,
                Updated = gcsObject.UpdatedDateTimeOffset?.DateTime ?? DateTime.MinValue,
                ContentType = gcsObject.ContentType,
                Content = content,
                ETag = gcsObject.ETag
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read GCS object: {FilePath}", sourceRequest.FilePath);
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
            var storageClient = await CreateStorageClientAsync(sourceRequest);

            if (!string.IsNullOrEmpty(sourceRequest.FilePath))
            {
                // Test specific object access
                var bucketName = ExtractBucketName(sourceRequest.FilePath);
                var objectName = ExtractObjectName(sourceRequest.FilePath);

                try
                {
                    await storageClient.GetObjectAsync(bucketName, objectName);
                    return true;
                }
                catch (GoogleApiException gex) when (gex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Object not found is still a valid connection, just test bucket access
                    await storageClient.GetBucketAsync(bucketName);
                    return true;
                }
            }
            else
            {
                // Test general access by listing buckets
                var projectId = ExtractProjectId(sourceRequest);
                var buckets = storageClient.ListBucketsAsync(projectId);
                var enumerator = buckets.GetAsyncEnumerator();
                try
                {
                    if (await enumerator.MoveNextAsync())
                    {
                        // If we can enumerate at least one bucket, the connection is good
                        return true;
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync();
                }
                return true; // Empty project is still a valid connection
            }
        }
        catch (GoogleApiException gcsEx)
        {
            logger.LogError(gcsEx, "Google Cloud Storage connection test failed: {StatusCode} - {Message}", gcsEx.HttpStatusCode, gcsEx.Message);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Google Cloud Storage connection test failed: {Error}", ex.Message);
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
        return "Google Cloud Storage connection with Service Account and Access Key authentication support";
    }

    private async Task<StorageClient> CreateStorageClientAsync(ConnectionSourceRequestDto sourceRequest)
    {
        switch (sourceRequest.AuthenticationType)
        {
            case AuthenticationTypeEnum.ServicePrincipal:
                if (string.IsNullOrEmpty(sourceRequest.GcpServiceAccountJson))
                    throw new ArgumentException("GCP Service Account JSON is required for Service Principal authentication.");
                
                var credential = GoogleCredential.FromJson(sourceRequest.GcpServiceAccountJson);
                return await StorageClient.CreateAsync(credential);

            case AuthenticationTypeEnum.AccessKey:
                if (string.IsNullOrEmpty(sourceRequest.GcpAccessKeyId) || string.IsNullOrEmpty(sourceRequest.GcpSecretAccessKey))
                    throw new ArgumentException("GCP Access Key ID and Secret Access Key are required for Access Key authentication.");
                
                // Note: GCS doesn't directly support HMAC keys in the same way as S3
                // This would require creating a credential from HMAC keys
                throw new NotSupportedException("HMAC key authentication for GCS is not yet implemented. Use Service Principal instead.");

            default:
                throw new NotSupportedException($"Authentication type '{sourceRequest.AuthenticationType}' is not supported for Google Cloud Storage.");
        }
    }

    private string ExtractBucketName(string? gcsPath)
    {
        if (string.IsNullOrEmpty(gcsPath))
            throw new ArgumentException("GCS path is required");

        // Handle gs://bucket/object format
        if (gcsPath.StartsWith("gs://"))
        {
            var pathParts = gcsPath.Substring(5).Split('/', 2);
            return pathParts[0];
        }

        // Handle bucket/object format
        var parts = gcsPath.Split('/', 2);
        return parts[0];
    }

    private string ExtractObjectName(string? gcsPath)
    {
        if (string.IsNullOrEmpty(gcsPath))
            throw new ArgumentException("GCS path is required");

        // Handle gs://bucket/object format
        if (gcsPath.StartsWith("gs://"))
        {
            var pathParts = gcsPath.Substring(5).Split('/', 2);
            return pathParts.Length > 1 ? pathParts[1] : "";
        }

        // Handle bucket/object format
        var parts = gcsPath.Split('/', 2);
        return parts.Length > 1 ? parts[1] : "";
    }

    private string ExtractProjectId(ConnectionSourceRequestDto sourceRequest)
    {
        // Try to extract project ID from custom properties
        if (!string.IsNullOrEmpty(sourceRequest.CustomProperties))
        {
            try
            {
                var properties = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(sourceRequest.CustomProperties);
                if (properties?.ContainsKey("ProjectId") == true)
                {
                    return properties["ProjectId"];
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                logger.LogWarning(ex, "Failed to parse GCS custom properties: {Properties}", sourceRequest.CustomProperties);
            }
        }

        // Try to extract from service account JSON
        if (!string.IsNullOrEmpty(sourceRequest.GcpServiceAccountJson))
        {
            try
            {
                var serviceAccountData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(sourceRequest.GcpServiceAccountJson);
                if (serviceAccountData?.ContainsKey("project_id") == true)
                {
                    return serviceAccountData["project_id"].ToString() ?? "";
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                logger.LogWarning(ex, "Failed to parse GCS service account JSON for project ID");
            }
        }

        throw new ArgumentException("Project ID is required for Google Cloud Storage. Provide it in CustomProperties or Service Account JSON.");
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
            GcpServiceAccountJson = string.IsNullOrEmpty(source.GcpServiceAccountJson) ? null : encryptionService.Decrypt(source.GcpServiceAccountJson),
            GcpAccessKeyId = source.GcpAccessKeyId,
            GcpSecretAccessKey = string.IsNullOrEmpty(source.GcpSecretAccessKey) ? null : encryptionService.Decrypt(source.GcpSecretAccessKey),
            FilePath = source.FilePath,
            FileFormat = source.FileFormat,
            Delimiter = source.Delimiter,
            Encoding = source.Encoding,
            HasHeader = source.HasHeader,
            CustomProperties = source.CustomProperties
        };
    }
}