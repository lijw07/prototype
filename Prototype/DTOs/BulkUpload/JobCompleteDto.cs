namespace Prototype.DTOs.BulkUpload;

public class JobCompleteDto
{
    public string JobId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan TotalDuration { get; set; }
}