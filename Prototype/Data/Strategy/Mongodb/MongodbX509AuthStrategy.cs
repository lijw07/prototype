using MongoDB.Bson;
using MongoDB.Driver;
using Prototype.Data.Interface;
using Prototype.DTOs;

namespace Prototype.Data.Strategy.Mongodb;

public class MongodbX509AuthStrategy : IMongoAuthStrategy
{
    public async Task<(bool success, string message)> ConnectAsync(ApplicationRequestDto dto)
    {
        try
        {
            var credential = MongoCredential.CreateMongoX509Credential(dto.ConnectionSource.Username);

            var settings = MongoClientSettings.FromConnectionString(dto.ConnectionSource.Url);
            settings.Credential = credential;

            var client = new MongoClient(settings);
            await client.GetDatabase(dto.ConnectionSource.DatabaseName).RunCommandAsync((Command<BsonDocument>)"{ping:1}");
            return (true, "MongoDB X.509 connection successful.");
        }
        catch (Exception ex)
        {
            return (false, $"MongoDB X.509 connection failed: {ex.Message}");
        }
    }
}