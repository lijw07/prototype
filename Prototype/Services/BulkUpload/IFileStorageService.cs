using Prototype.Helpers;

namespace Prototype.Services.BulkUpload
{
    public interface IFileStorageService
    {
        Task<UploadedFileInfo> SaveFileAsync(IFormFile file, string tableType, Guid userId);
        Task<Result<byte[]>> GetFileAsync(Guid fileId, Guid userId);
        Task<Result<bool>> DeleteFileAsync(Guid fileId, Guid userId);
        Task<List<StoredFileInfo>> GetUserFilesAsync(Guid userId);
    }

    public class UploadedFileInfo
    {
        public Guid FileId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class StoredFileInfo
    {
        public Guid FileId { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string TableType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
        public int RecordCount { get; set; }
    }
}