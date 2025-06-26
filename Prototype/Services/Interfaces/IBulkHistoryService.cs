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
        
    Task<PaginatedResult<BulkUploadHistory>> GetUploadHistoryAsync(
        Guid userId, 
        int page = 1, 
        int pageSize = 50);
        
    Task<List<BulkUploadHistory>> GetRecentUploadsAsync(
        Guid userId, 
        int limit = 10);
        
    Task<BulkUploadHistory?> GetUploadByIdAsync(Guid uploadId);
    
    Task DeleteUploadHistoryAsync(Guid uploadId, Guid userId);
}

public class BulkUploadHistory
{
    public Guid UploadId { get; set; }
    public Guid UserId { get; set; }
    public string TableType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int FileSize { get; set; }
    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public int ErrorCount { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime UploadedAt { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}