namespace Prototype.DTOs.BulkUpload
{
    public class BulkUploadResponse
    {
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int FailedRecords { get; set; }
        public string TableType { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
        public List<BulkUploadError> Errors { get; set; } = new List<BulkUploadError>();
        public TimeSpan ProcessingTime { get; set; }
        
        // Additional properties for multiple file support
        public string? FileName { get; set; }
        public int? FileIndex { get; set; }
        public int? TotalFiles { get; set; }
    }

    public class MultipleBulkUploadResponse
    {
        public int TotalFiles { get; set; }
        public int ProcessedFiles { get; set; }
        public int FailedFiles { get; set; }
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int FailedRecords { get; set; }
        public DateTime ProcessedAt { get; set; }
        public TimeSpan TotalProcessingTime { get; set; }
        public List<BulkUploadResponse> FileResults { get; set; } = new List<BulkUploadResponse>();
        public List<string> GlobalErrors { get; set; } = new List<string>();
        public bool OverallSuccess { get; set; }
    }

    public class BulkUploadError
    {
        public int RowNumber { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public Dictionary<string, object>? RowData { get; set; }
        
        // Additional property for file context
        public string? FileName { get; set; }
    }
}