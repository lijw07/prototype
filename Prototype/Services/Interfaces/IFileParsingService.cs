using System.Data;

namespace Prototype.Services.Interfaces;

public interface IFileParsingService
{
    Task<DataTable?> ParseFileAsync(byte[] fileData, string fileExtension);
    Task<DataTable?> ParseCsvAsync(byte[] fileData);
    Task<DataTable?> ParseExcelAsync(byte[] fileData);
    Task<DataTable?> ParseJsonAsync(byte[] fileData);
    Task<DataTable?> ParseXmlAsync(byte[] fileData);
    bool IsValidFileExtension(string fileExtension);
    int CalculateMemoryOptimizedBatchSize(int availableMemoryMB, int estimatedRowSizeMB);
}