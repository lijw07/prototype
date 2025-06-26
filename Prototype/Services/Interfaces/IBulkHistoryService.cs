using Prototype.DTOs.BulkUpload;
using Prototype.Utility;

namespace Prototype.Services.Interfaces;

public interface IBulkHistoryService
{
    Task LogUploadHistoryAsync(
        Guid userId, 
        string tableType, 
        BulkUploadResponseDto response, 
        string fileName, 
        int fileSize);
        
    Task<PaginatedResult<BulkUploadHistoryDto>> GetUploadHistoryAsync(
        Guid userId, 
        int page = 1, 
        int pageSize = 50);
        
    Task<List<BulkUploadHistoryDto>> GetRecentUploadsAsync(
        Guid userId, 
        int limit = 10);
        
    Task<BulkUploadHistoryDto?> GetUploadByIdAsync(Guid uploadId);
    
    Task DeleteUploadHistoryAsync(Guid uploadId, Guid userId);
}