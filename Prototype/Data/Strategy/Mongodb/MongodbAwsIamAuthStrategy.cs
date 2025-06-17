using MongoDB.Bson;
using MongoDB.Driver;
using Prototype.Data.Interface;
using Prototype.DTOs;

namespace Prototype.Data.Strategy.Mongodb;

public class MongodbAwsIamAuthStrategy : IMongoAuthStrategy
{
    public async Task<(bool success, string message)> ConnectAsync(ApplicationRequestDto dto)
    {
        try
        {
            var url = new MongoUrl(dto.ConnectionSource.Url);
            var settings = MongoClientSettings.FromUrl(url);
            
            if (!string.IsNullOrEmpty(dto.ConnectionSource.AwsAccessKeyId) && !string.IsNullOrEmpty(dto.ConnectionSource.AwsSecretAccessKey))
            {
                Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", dto.ConnectionSource.AwsAccessKeyId);
                Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", dto.ConnectionSource.AwsSecretAccessKey);

                if (!string.IsNullOrEmpty(dto.ConnectionSource.AwsSessionToken))
                {
                    Environment.SetEnvironmentVariable("AWS_SESSION_TOKEN", dto.ConnectionSource.AwsSessionToken);
                }
            }

            var client = new MongoClient(settings);
            await client.GetDatabase(dto.ConnectionSource.DatabaseName).RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));

            return (true, "MongoDB AWS IAM authentication successful.");
        }
        catch (Exception ex)
        {
            return (false, $"MongoDB AWS IAM authentication failed: {ex.Message}");
        }
    }
}