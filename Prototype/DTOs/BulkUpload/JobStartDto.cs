namespace Prototype.DTOs.BulkUpload;

public class JobStartDto
{
    public string JobId { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public int TotalFiles { get; set; }
    public int EstimatedTotalRecords { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
}