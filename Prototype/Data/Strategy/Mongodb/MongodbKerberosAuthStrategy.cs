using MongoDB.Bson;
using MongoDB.Driver;
using Prototype.Data.Interface;
using Prototype.DTOs;

namespace Prototype.Data.Strategy.Mongodb;

public class MongodbKerberosAuthStrategy : IMongoAuthStrategy
{
    public async Task<(bool success, string message)> ConnectAsync(ApplicationRequestDto dto)
    {
        try
        {
            var credential = MongoCredential.CreateGssapiCredential(dto.ConnectionSource.Principal);

            var settings = MongoClientSettings.FromConnectionString(dto.ConnectionSource.Url);
            settings.Credential = credential;

            var client = new MongoClient(settings);
            await client.GetDatabase(dto.ConnectionSource.DatabaseName).RunCommandAsync((Command<BsonDocument>)"{ping:1}");
            return (true, "MongoDB Kerberos (GSSAPI) connection successful.");
        }
        catch (Exception ex)
        {
            return (false, $"MongoDB GSSAPI connection failed: {ex.Message}");
        }
    }
}