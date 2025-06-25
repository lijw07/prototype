using Prototype.Enum;

namespace Prototype.DTOs.BulkUpload;

public class QueuedFileInfoRequestDto
{
    public string FileName { get; set; } = string.Empty;
    public byte[] FileData { get; set; } = Array.Empty<byte>();
    public string FileExtension { get; set; } = string.Empty;
    public string TableType { get; set; } = string.Empty;
    public QueuedFileStatusEnum Status { get; set; }
    public int FileIndex { get; set; }
    public DateTime QueuedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int ProcessedRecords { get; set; }
    public int FailedRecords { get; set; }
    public int TotalRecords { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public List<string> Errors { get; set; } = new();
}