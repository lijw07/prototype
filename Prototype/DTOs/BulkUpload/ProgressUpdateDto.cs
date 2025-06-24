namespace Prototype.DTOs.BulkUpload
{
    public class ProgressUpdateDto
    {
        public string JobId { get; set; } = string.Empty;
        public double ProgressPercentage { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? CurrentOperation { get; set; }
        public int ProcessedRecords { get; set; }
        public int TotalRecords { get; set; }
        public string? CurrentFileName { get; set; }
        public int ProcessedFiles { get; set; }
        public int TotalFiles { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public List<string>? Errors { get; set; }
    }

    public class JobStartDto
    {
        public string JobId { get; set; } = string.Empty;
        public string JobType { get; set; } = string.Empty;
        public int TotalFiles { get; set; }
        public int EstimatedTotalRecords { get; set; }
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
    }

    public class JobCompleteDto
    {
        public string JobId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public BulkUploadResponse? Data { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan TotalDuration { get; set; }
    }
}