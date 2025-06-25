using Prototype.Services.BulkUpload.Mappers;

namespace Prototype.Services.BulkUpload;

public class TableMappingService(IServiceProvider serviceProvider) : ITableMappingService
{
    private readonly Dictionary<string, ITableMapper> _mappers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Users"] = serviceProvider.GetRequiredService<UserTableMapper>(),
        ["Applications"] = serviceProvider.GetRequiredService<ApplicationTableMapper>(),
        ["UserApplications"] = serviceProvider.GetRequiredService<UserApplicationTableMapper>(),
        ["TemporaryUsers"] = serviceProvider.GetRequiredService<TemporaryUserTableMapper>(),
        ["UserRoles"] = serviceProvider.GetRequiredService<UserRoleTableMapper>()
    };

    public ITableMapper? GetMapper(string tableType)
    {
        _mappers.TryGetValue(tableType, out var mapper);
        return mapper;
    }

    public List<string> GetSupportedTableTypes()
    {
        return _mappers.Keys.ToList();
    }
}