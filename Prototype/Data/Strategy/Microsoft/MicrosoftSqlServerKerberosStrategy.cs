using Microsoft.Data.SqlClient;
using Prototype.Data.Interface;
using Prototype.DTOs;

namespace Prototype.Data.Strategy.Microsoft;

public class MicrosoftSqlServerKerberosStrategy : IDatabaseTypeSpecificValidator
{
    public async Task<(bool success, string message)> TestConnectionAsync(ApplicationRequestDto dto)
    {
        try
        {
            var dataSource = string.IsNullOrEmpty(dto.ConnectionSource.Instance)
                ? $"{dto.ConnectionSource.Host},{dto.ConnectionSource.Port}"
                : $"{dto.ConnectionSource.Host}\\{dto.ConnectionSource.Instance},{dto.ConnectionSource.Port}";
            
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = dataSource,
                InitialCatalog = dto.ConnectionSource.DatabaseName,
                IntegratedSecurity = true,
                ConnectTimeout = 5
            };

            using var conn = new SqlConnection(builder.ToString());
            await conn.OpenAsync();
            return (true, "Kerberos auth successful");
        }
        catch (Exception ex)
        {
            return (false, $"Kerberos auth failed: {ex.Message}");
        }
    }
}