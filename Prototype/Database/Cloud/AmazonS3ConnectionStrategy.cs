using Amazon.S3;
using Amazon.S3.Model;
using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;

namespace Prototype.Database.Cloud;

public class AmazonS3ConnectionStrategy(
    PasswordEncryptionService encryptionService,
    ILogger<AmazonS3ConnectionStrategy> logger)
    : IFileConnectionStrategy
{
    public DataSourceTypeEnum ConnectionType => DataSourceTypeEnum.AmazonS3;

    public Dictionary<AuthenticationTypeEnum, bool> GetSupportedAuthTypes()
    {
        return new Dictionary<AuthenticationTypeEnum, bool>
        {
            { AuthenticationTypeEnum.AwsIam, true },
            { AuthenticationTypeEnum.AwsAccessKey, true },
            { AuthenticationTypeEnum.AwsSessionToken, true },
            { AuthenticationTypeEnum.NoAuth, false } // S3 always requires credentials
        };
    }

    public async Task<object> ReadDataAsync(ConnectionSourceRequestDto sourceRequest)
    {
        try
        {
            using var s3Client = CreateS3Client(sourceRequest);
            var bucketName = ExtractBucketName(sourceRequest.FilePath);
            var objectKey = ExtractObjectKey(sourceRequest.FilePath);

            var request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey
            };

            using var response = await s3Client.GetObjectAsync(request);
            using var reader = new StreamReader(response.ResponseStream);
            var content = await reader.ReadToEndAsync();

            return new
            {
                BucketName = bucketName,
                ObjectKey = objectKey,
                ContentLength = response.ContentLength,
                LastModified = response.LastModified,
                ContentType = response.Headers.ContentType,
                Content = content,
                ETag = response.ETag
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read S3 object: {FilePath}", sourceRequest.FilePath);
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
            using var s3Client = CreateS3Client(sourceRequest);

            if (!string.IsNullOrEmpty(sourceRequest.FilePath))
            {
                // Test specific object access
                var bucketName = ExtractBucketName(sourceRequest.FilePath);
                var objectKey = ExtractObjectKey(sourceRequest.FilePath);

                var request = new GetObjectMetadataRequest
                {
                    BucketName = bucketName,
                    Key = objectKey
                };

                await s3Client.GetObjectMetadataAsync(request);
                return true;
            }
            else
            {
                // Test bucket listing (general access)
                var request = new ListBucketsRequest();
                await s3Client.ListBucketsAsync(request);
                return true;
            }
        }
        catch (AmazonS3Exception s3Ex)
        {
            logger.LogError(s3Ex, "S3 connection test failed: {ErrorCode} - {Message}", s3Ex.ErrorCode, s3Ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "S3 connection test failed: {Error}", ex.Message);
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
        return "Amazon S3 storage connection with IAM, Access Key, and Session Token authentication support";
    }

    private AmazonS3Client CreateS3Client(ConnectionSourceRequestDto sourceRequest)
    {
        var config = new AmazonS3Config();

        // Set region if specified in custom properties
        if (!string.IsNullOrEmpty(sourceRequest.CustomProperties))
        {
            try
            {
                var properties = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(sourceRequest.CustomProperties);
                if (properties?.ContainsKey("Region") == true)
                {
                    config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(properties["Region"]);
                }
                if (properties?.ContainsKey("ServiceURL") == true)
                {
                    config.ServiceURL = properties["ServiceURL"]; // For S3-compatible services
                }
                if (properties?.ContainsKey("ForcePathStyle") == true && bool.TryParse(properties["ForcePathStyle"], out bool forcePathStyle))
                {
                    config.ForcePathStyle = forcePathStyle;
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                logger.LogWarning(ex, "Failed to parse S3 custom properties: {Properties}", sourceRequest.CustomProperties);
            }
        }

        switch (sourceRequest.AuthenticationType)
        {
            case AuthenticationTypeEnum.AwsAccessKey:
                if (string.IsNullOrEmpty(sourceRequest.AwsAccessKeyId) || string.IsNullOrEmpty(sourceRequest.AwsSecretAccessKey))
                    throw new ArgumentException("AWS Access Key ID and Secret Access Key are required for AWS Access Key authentication.");
                
                return new AmazonS3Client(sourceRequest.AwsAccessKeyId, sourceRequest.AwsSecretAccessKey, config);

            case AuthenticationTypeEnum.AwsSessionToken:
                if (string.IsNullOrEmpty(sourceRequest.AwsAccessKeyId) || string.IsNullOrEmpty(sourceRequest.AwsSecretAccessKey) || string.IsNullOrEmpty(sourceRequest.AwsSessionToken))
                    throw new ArgumentException("AWS Access Key ID, Secret Access Key, and Session Token are required for AWS Session Token authentication.");
                
                return new AmazonS3Client(sourceRequest.AwsAccessKeyId, sourceRequest.AwsSecretAccessKey, sourceRequest.AwsSessionToken, config);

            case AuthenticationTypeEnum.AwsIam:
                // Use default credential chain (IAM roles, environment variables, etc.)
                return new AmazonS3Client(config);

            default:
                throw new NotSupportedException($"Authentication type '{sourceRequest.AuthenticationType}' is not supported for Amazon S3.");
        }
    }

    private string ExtractBucketName(string? s3Path)
    {
        if (string.IsNullOrEmpty(s3Path))
            throw new ArgumentException("S3 path is required");

        // Handle s3://bucket/key format
        if (s3Path.StartsWith("s3://"))
        {
            var pathParts = s3Path.Substring(5).Split('/', 2);
            return pathParts[0];
        }

        // Handle bucket/key format
        var parts = s3Path.Split('/', 2);
        return parts[0];
    }

    private string ExtractObjectKey(string? s3Path)
    {
        if (string.IsNullOrEmpty(s3Path))
            throw new ArgumentException("S3 path is required");

        // Handle s3://bucket/key format
        if (s3Path.StartsWith("s3://"))
        {
            var pathParts = s3Path.Substring(5).Split('/', 2);
            return pathParts.Length > 1 ? pathParts[1] : "";
        }

        // Handle bucket/key format
        var parts = s3Path.Split('/', 2);
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
            AwsAccessKeyId = source.AwsAccessKeyId,
            AwsSecretAccessKey = string.IsNullOrEmpty(source.AwsSecretAccessKey) ? null : encryptionService.Decrypt(source.AwsSecretAccessKey),
            AwsSessionToken = string.IsNullOrEmpty(source.AwsSessionToken) ? null : encryptionService.Decrypt(source.AwsSessionToken),
            FilePath = source.FilePath,
            FileFormat = source.FileFormat,
            Delimiter = source.Delimiter,
            Encoding = source.Encoding,
            HasHeader = source.HasHeader,
            CustomProperties = source.CustomProperties
        };
    }
}