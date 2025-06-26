using Prototype.DTOs.BulkUpload;
using Prototype.Services.BulkUpload.Mappers;

namespace Prototype.Services.Interfaces;

public interface IBulkTemplateService
{
    Task<FileTemplateInfo?> GenerateTemplateAsync(string tableType);
    Task<FileTemplateInfo?> GenerateExcelTemplateAsync(ITableMapper mapper);
    Task<byte[]> CreateExcelTemplateWithSampleDataAsync(ITableMapper mapper);
    List<string> GetSupportedTableTypes();
}