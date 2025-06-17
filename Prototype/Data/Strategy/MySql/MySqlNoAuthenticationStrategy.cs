using MySql.Data.MySqlClient;
using Prototype.Data.Interface;
using Prototype.DTOs;

namespace Prototype.Data.Strategy;

public class MySqlNoAuthenticationStrategy : IDatabaseTypeSpecificValidator
{
    public async Task<(bool success, string message)> TestConnectionAsync(ApplicationRequestDto dto)
    {
        try
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = dto.ConnectionSource.Host,
                Port = uint.TryParse(dto.ConnectionSource.Port, out var port) ? port : 3306,
                Database = dto.ConnectionSource.DatabaseName,
                UserID = "",
                Password = "",
                SslMode = MySqlSslMode.Preferred,
                AllowPublicKeyRetrieval = true,
                ConnectionTimeout = 5
            };

            await using var conn = new MySqlConnection(builder.ToString());
            await conn.OpenAsync();
            return (true, "Connection successful (no authentication)");
        }
        catch (Exception ex)
        {
            return (false, $"Connection Failed: {ex.Message}");
        }
    }
}