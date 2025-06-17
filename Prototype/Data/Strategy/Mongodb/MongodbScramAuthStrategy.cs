using MongoDB.Bson;
using MongoDB.Driver;
using Prototype.Data.Interface;
using Prototype.DTOs;

namespace Prototype.Data.Strategy.Mongodb;

public class MongodbScramAuthStrategy(string mechanism) : IMongoAuthStrategy
{
    public async Task<(bool success, string message)> ConnectAsync(ApplicationRequestDto dto)
    {
        try
        {
            var credential = MongoCredential.CreateCredential(
                dto.ConnectionSource.AuthenticationDatabase ?? "admin",
                dto.ConnectionSource.Username,
                dto.ConnectionSource.Password
            );

            var settings = MongoClientSettings.FromUrl(new MongoUrl(dto.ConnectionSource.Url));
            settings.Credential = credential;

            var client = new MongoClient(settings);
            await client.GetDatabase(dto.ConnectionSource.DatabaseName).RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));

            return (true, $"MongoDB ({mechanism}) connection successful.");
        }
        catch (Exception ex)
        {
            return (false, $"MongoDB SCRAM connection failed: {ex.Message}");
        }
    }
}