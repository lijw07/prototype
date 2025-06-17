using Prototype.Data.Interface;
using Prototype.DTOs;
using Prototype.Enum;

namespace Prototype.Data.Validator;

public class DatabaseConnectionValidator(IServiceProvider serviceProvider) : IDatabaseConnectionValidator
{
    public async Task<(bool success, string message)> TestConnectionAsync(ApplicationRequestDto source)
    {
        IDatabaseTypeSpecificValidator validator = source.DataSourceType switch
        {
            DataSourceTypeEnum.MicrosoftSqlServer => serviceProvider.GetRequiredService<MicrosoftSqlValidator>(),
            DataSourceTypeEnum.MySql => serviceProvider.GetRequiredService<MySqlValidator>(),
            DataSourceTypeEnum.MongoDb => serviceProvider.GetRequiredService<MongoDbValidator>(),
            _ => throw new NotSupportedException($"Unsupported data source: {source.DataSourceType}")
        };

        return await validator.TestConnectionAsync(source);
    }
}