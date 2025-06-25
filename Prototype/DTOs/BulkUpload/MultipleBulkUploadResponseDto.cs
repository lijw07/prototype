namespace Prototype.DTOs.BulkUpload;

public class MultipleBulkUploadResponseDto
{
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public int FailedFiles { get; set; }
    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public int FailedRecords { get; set; }
    public DateTime ProcessedAt { get; set; }
    public TimeSpan TotalProcessingTime { get; set; }
    public List<BulkUploadResponseDto> FileResults { get; set; } = new List<BulkUploadResponseDto>();
    public List<string> GlobalErrors { get; set; } = new List<string>();
    public bool OverallSuccess { get; set; }
}