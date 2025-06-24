using System.Data;
using Prototype.DTOs.BulkUpload;
using Prototype.Helpers;

namespace Prototype.Services.BulkUpload
{
    public interface ITableMappingService
    {
        ITableMapper? GetMapper(string tableType);
        List<string> GetSupportedTableTypes();
    }

    public interface ITableMapper
    {
        Task<ValidationResult> ValidateRowAsync(DataRow row, int rowNumber);
        Task<Result<bool>> SaveRowAsync(DataRow row, Guid userId);
        List<TableColumnInfo> GetTemplateColumns();
        List<Dictionary<string, object>> GetExampleData();
        string TableType { get; }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}