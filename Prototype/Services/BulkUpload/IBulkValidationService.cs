using System.Data;
using Prototype.DTOs.BulkUpload;
using Prototype.Helpers;

namespace Prototype.Services.BulkUpload;

public interface IBulkValidationService
{
    Task<Result<bool>> ValidateDataAsync(DataTable dataTable, string tableType, CancellationToken cancellationToken = default);
    Task<Dictionary<int, ValidationResultDto>> ValidateWithResultsAsync(DataTable dataTable, string tableType, CancellationToken cancellationToken = default);
}