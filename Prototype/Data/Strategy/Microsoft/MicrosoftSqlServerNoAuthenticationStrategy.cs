using Microsoft.Data.SqlClient;
using Prototype.Data.Interface;
using Prototype.DTOs;

namespace Prototype.Data.Strategy.Microsoft;

public class MicrosoftSqlServerNoAuthenticationStrategy : IDatabaseTypeSpecificValidator
{
    public async Task<(bool success, string message)> TestConnectionAsync(ApplicationRequestDto dto)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = $"{dto.ConnectionSource.Host},{dto.ConnectionSource.Port}",
                InitialCatalog = dto.ConnectionSource.DatabaseName,
                IntegratedSecurity = true,
                ConnectTimeout = 5
            };

            using var conn = new SqlConnection(builder.ToString());
            await conn.OpenAsync();
            return (true, "Connection successful (no username/password)");
        }
        catch (Exception ex)
        {
            return (false, $"Connection Failed: {ex.Message}");
        }
    }
}