using System.Data;
using Prototype.DTOs.BulkUpload;
using Prototype.Helpers;

namespace Prototype.Services.BulkUpload;

public interface IBulkDataProcessingService
{
    Task<Result<BulkUploadResponseDto>> ProcessDataAsync(
        DataTable dataTable, 
        string tableType, 
        Guid userId, 
        bool ignoreErrors = false,
        CancellationToken cancellationToken = default,
        Guid? jobId = null);
        
    Task<Result<BulkUploadResponseDto>> ProcessDataWithProgressAsync(
        DataTable dataTable, 
        string tableType, 
        Guid userId, 
        bool ignoreErrors = false,
        CancellationToken cancellationToken = default);
}