using System.Data;
using Prototype.DTOs.BulkUpload;
using Prototype.Services.BulkUpload.Mappers;

namespace Prototype.Services.Interfaces;

public interface IBulkValidationService
{
    Task<Dictionary<int, ValidationResultDto>> ValidateDataAsync(
        DataTable dataTable, 
        ITableMapper mapper, 
        CancellationToken cancellationToken = default);
        
    Task<Dictionary<int, ValidationResultDto>> ValidateBatchAsync(
        DataTable dataTable, 
        IBatchTableMapper mapper, 
        CancellationToken cancellationToken = default);
        
    Task<Dictionary<int, ValidationResultDto>> ValidateParallelAsync(
        DataTable dataTable, 
        ITableMapper mapper, 
        CancellationToken cancellationToken = default);
        
    Task<Dictionary<int, ValidationResultDto>> ValidateRowAsync(
        DataRow row, 
        int rowNumber, 
        ITableMapper mapper);
        
    List<string> GetValidationErrorSummary(Dictionary<int, ValidationResultDto> validationResults);
    int GetValidRecordCount(Dictionary<int, ValidationResultDto> validationResults);
}