using System.Data;
using Prototype.DTOs.BulkUpload;
using Prototype.Utility;

namespace Prototype.Services.BulkUpload;

public interface ITableMapper
{
    Task<ValidationResultDto> ValidateRowAsync(DataRow row, int rowNumber, CancellationToken cancellationToken = default);
    Task<Result<bool>> SaveRowAsync(DataRow row, Guid userId, CancellationToken cancellationToken = default);
    List<TableColumnInfoDto> GetTemplateColumns();
    List<Dictionary<string, object>> GetExampleData();
    string TableType { get; }
}