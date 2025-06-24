using Prototype.DTOs.BulkUpload;
using Prototype.Helpers;

namespace Prototype.Services.BulkUpload
{
    public interface IBulkUploadService
    {
        Task<Result<bool>> ValidateDataAsync(byte[] fileData, string tableType);
        Task<Result<BulkUploadResponse>> ProcessBulkDataAsync(byte[] fileData, string tableType, Guid userId, bool ignoreErrors = false);
        Task<FileTemplateInfo?> GetTemplateAsync(string tableType);
        Task<PaginatedResult<BulkUploadHistory>> GetUploadHistoryAsync(Guid userId, int page, int pageSize);
    }
}