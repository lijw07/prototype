namespace Prototype.DTOs.BulkUpload
{
    public class BulkUploadHistory
    {
        public Guid UploadId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string TableType { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int FailedRecords { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool HasSavedFile { get; set; }
        public Guid? SavedFileId { get; set; }
    }
}