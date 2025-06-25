using Prototype.DTOs.BulkUpload;
using Prototype.Enum;

namespace Prototype.Services.BulkUpload;

public interface IFileQueueService
{
    Task<string> QueueMultipleFilesAsync(QueuedFileUploadRequestDto request);
    Task ProcessQueueAsync(string jobId, CancellationToken cancellationToken = default);
    QueueStatusEnum GetQueueStatus(string jobId);
    List<QueuedFileInfoRequestDto> GetQueuedFiles(string jobId);
    bool CancelQueue(string jobId);
}