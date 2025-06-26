using System.Data;
using Prototype.DTOs.BulkUpload;
using Prototype.Utility;

namespace Prototype.Services.BulkUpload;

public interface IBatchTableMapper : ITableMapper
{
    Task<Dictionary<int, ValidationResultDto>> ValidateBatchAsync(DataTable dataTable, CancellationToken cancellationToken = default);
    Task<Result<int>> SaveBatchAsync(DataTable dataTable, Guid userId, Dictionary<int, ValidationResultDto> validationResults, bool ignoreErrors = false, CancellationToken cancellationToken = default);
}