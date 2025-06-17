using Microsoft.Data.SqlClient;
using Prototype.Data.Interface;
using Prototype.DTOs;

namespace Prototype.Data.Validator;

public class MicrosoftSqlValidator : IDatabaseTypeSpecificValidator
{
    public async Task<(bool success, string message)> TestConnectionAsync(ApplicationRequestDto source)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = $"{source.ConnectionSource.Host},{source.ConnectionSource.Port}",
                InitialCatalog = source.ConnectionSource.DatabaseName,
                UserID = source.ConnectionSource.Username,
                Password = source.ConnectionSource.Password,
                ConnectTimeout = 5
            };

            using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();

            return (true, "Connection to Microsoft SQL Server successful.");
        }
        catch (Exception ex)
        {
            return (false, $"Microsoft SQL Server connection failed: {ex.Message}");
        }
    }
}