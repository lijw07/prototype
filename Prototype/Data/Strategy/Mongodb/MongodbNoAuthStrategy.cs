using MongoDB.Bson;
using MongoDB.Driver;
using Prototype.Data.Interface;
using Prototype.DTOs;

namespace Prototype.Data.Strategy.Mongodb;

public class MongodbNoAuthStrategy : IMongoAuthStrategy
{
    public async Task<(bool success, string message)> ConnectAsync(ApplicationRequestDto dto)
    {
        try
        {
            var client = new MongoClient(dto.ConnectionSource.Url);
            await client.GetDatabase(dto.ConnectionSource.DatabaseName).RunCommandAsync((Command<BsonDocument>)"{ping:1}");
            return (true, "MongoDB (No Auth) connection successful.");
        }
        catch (Exception ex)
        {
            return (false, $"MongoDB NoAuth connection failed: {ex.Message}");
        }
    }
}