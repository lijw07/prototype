using System.Data;
using Prototype.DTOs.BulkUpload;
using Prototype.Services.BulkUpload.Mappers;

namespace Prototype.Services.Interfaces;

public interface IBulkProcessingService
{
    Task<int> ProcessBulkAsync(
        DataTable dataTable, 
        ITableMapper mapper, 
        Guid userId, 
        Dictionary<int, ValidationResultDto> validationResults, 
        bool ignoreErrors, 
        CancellationToken cancellationToken = default);
        
    Task<int> ProcessWithBulkInsertAsync(
        DataTable dataTable, 
        ITableMapper mapper, 
        Guid userId, 
        Dictionary<int, ValidationResultDto> validationResults, 
        bool ignoreErrors, 
        CancellationToken cancellationToken = default);
        
    Task<int> ProcessBatchAsync(
        DataTable dataTable, 
        IBatchTableMapper mapper, 
        Guid userId, 
        Dictionary<int, ValidationResultDto> validationResults, 
        bool ignoreErrors, 
        CancellationToken cancellationToken = default);
        
    Task<int> ProcessRowByRowAsync(
        DataTable dataTable, 
        ITableMapper mapper, 
        Guid userId, 
        Dictionary<int, ValidationResultDto> validationResults, 
        bool ignoreErrors, 
        CancellationToken cancellationToken = default);
        
    Task<int> ProcessRowByRowOptimizedAsync(
        DataTable dataTable, 
        ITableMapper mapper, 
        Guid userId, 
        Dictionary<int, ValidationResultDto> validationResults, 
        bool ignoreErrors, 
        CancellationToken cancellationToken = default);
        
    int CalculateOptimalBatchSize(int totalRecords, int availableMemoryMB);
}