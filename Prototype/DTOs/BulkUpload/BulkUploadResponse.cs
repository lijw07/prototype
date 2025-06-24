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
        public string? SavedFilePath { get; set; }
        public Guid? SavedFileId { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }

    public class BulkUploadError
    {
        public int RowNumber { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public Dictionary<string, object>? RowData { get; set; }
    }
}