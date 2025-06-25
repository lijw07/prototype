namespace Prototype.Services.BulkUpload;

public interface ITableMappingService
{
    ITableMapper? GetMapper(string tableType);
    List<string> GetSupportedTableTypes();
}