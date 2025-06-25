using Prototype.DTOs.BulkUpload;

namespace Prototype.Services.BulkUpload;

public interface ITableDetectionService
{
    Task<DetectedTableInfoDto?> DetectTableTypeAsync(byte[] fileData, string fileExtension);
    Task<List<SupportedTableInfoDto>> GetSupportedTablesAsync();
    bool IsTableSupported(string tableName);
}