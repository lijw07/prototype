using Prototype.Enum;

namespace Prototype.DTOs.BulkUpload;

public class FileQueueRequestDto
{
    public string JobId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public List<QueuedFileInfoRequestDto> Files { get; set; } = new();
    public QueueStatusEnum Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IgnoreErrors { get; set; }
    public bool ContinueOnError { get; set; }
}