
using MongoDB.Bson;
using MongoDB.Driver;
using Prototype.Data.Interface;
using Prototype.DTOs;

namespace Prototype.Data.Strategy.Mongodb;

public class MongodbUserPasswordStrategy : IMongoAuthStrategy
{
    public async Task<(bool success, string message)> ConnectAsync(ApplicationRequestDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.ConnectionSource.Username) || string.IsNullOrWhiteSpace(dto.ConnectionSource.Password))
                return (false, "Username and Password are required for MongoDB UserPassword authentication.");
            
            var authDatabase = string.IsNullOrWhiteSpace(dto.ConnectionSource.AuthenticationDatabase)
                ? "admin"
                : dto.ConnectionSource.AuthenticationDatabase;
            
            var credential = MongoCredential.CreateCredential(authDatabase, dto.ConnectionSource.Username, dto.ConnectionSource.Password);
            
            var url = new MongoUrl(dto.ConnectionSource.Url);
            var settings = MongoClientSettings.FromUrl(url);
            settings.Credential = credential;

            var client = new MongoClient(settings);
            await client.GetDatabase(dto.ConnectionSource.DatabaseName).RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));

            return (true, "MongoDB UserPassword authentication successful.");
        }
        catch (Exception ex)
        {
            return (false, $"MongoDB UserPassword authentication failed: {ex.Message}");
        }
    }
}