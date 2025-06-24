using Prototype.Services.BulkUpload.Mappers;

namespace Prototype.Services.BulkUpload
{
    public class TableMappingService : ITableMappingService
    {
        private readonly Dictionary<string, ITableMapper> _mappers;

        public TableMappingService(IServiceProvider serviceProvider)
        {
            _mappers = new Dictionary<string, ITableMapper>(StringComparer.OrdinalIgnoreCase)
            {
                ["Users"] = serviceProvider.GetRequiredService<UserTableMapper>(),
                ["Applications"] = serviceProvider.GetRequiredService<ApplicationTableMapper>(),
                ["UserApplications"] = serviceProvider.GetRequiredService<UserApplicationTableMapper>(),
                ["TemporaryUsers"] = serviceProvider.GetRequiredService<TemporaryUserTableMapper>()
            };
        }

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
}