namespace Prototype.DTOs.BulkUpload;

public class BulkUploadResponseDto
{
    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public int FailedRecords { get; set; }
    public string TableType { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public List<BulkUploadErrorDto> Errors { get; set; } = new List<BulkUploadErrorDto>();
    public TimeSpan ProcessingTime { get; set; }
    
    public string? FileName { get; set; }
    public int? FileIndex { get; set; }
    public int? TotalFiles { get; set; }
}