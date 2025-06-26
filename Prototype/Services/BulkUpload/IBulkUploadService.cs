using Prototype.DTOs.BulkUpload;
using Prototype.Utility;

namespace Prototype.Services.BulkUpload
{
    public interface IBulkUploadService
    {
        Task<Result<bool>> ValidateDataAsync(byte[] fileData, string tableType, string fileExtension, CancellationToken cancellationToken = default);
        Task<Result<BulkUploadResponseDto>> ProcessBulkDataAsync(byte[] fileData, string tableType, string fileExtension, Guid userId, bool ignoreErrors = false, CancellationToken cancellationToken = default);
        Task<Result<BulkUploadResponseDto>> ProcessBulkDataWithProgressAsync(byte[] fileData, string tableType, string fileExtension, Guid userId, string jobId, string? fileName = null, int fileIndex = 0, int totalFiles = 1, bool ignoreErrors = false, CancellationToken cancellationToken = default);
        Task<FileTemplateInfo?> GetTemplateAsync(string tableType);
        Task<PaginatedResult<BulkUploadHistory>> GetUploadHistoryAsync(Guid userId, int page, int pageSize);
    }
}