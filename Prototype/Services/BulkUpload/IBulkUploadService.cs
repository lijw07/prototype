using Prototype.DTOs.BulkUpload;
using Prototype.Helpers;

namespace Prototype.Services.BulkUpload
{
    public interface IBulkUploadService
    {
        Task<Result<bool>> ValidateDataAsync(byte[] fileData, string tableType, string fileExtension);
        Task<Result<BulkUploadResponse>> ProcessBulkDataAsync(byte[] fileData, string tableType, string fileExtension, Guid userId, bool ignoreErrors = false);
        Task<Result<BulkUploadResponse>> ProcessBulkDataWithProgressAsync(byte[] fileData, string tableType, string fileExtension, Guid userId, string jobId, string? fileName = null, int fileIndex = 0, int totalFiles = 1, bool ignoreErrors = false);
        Task<FileTemplateInfo?> GetTemplateAsync(string tableType);
        Task<PaginatedResult<BulkUploadHistory>> GetUploadHistoryAsync(Guid userId, int page, int pageSize);
    }
}