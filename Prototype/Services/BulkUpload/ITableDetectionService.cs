using Prototype.DTOs.BulkUpload;

namespace Prototype.Services.BulkUpload
{
    public interface ITableDetectionService
    {
        Task<DetectedTableInfo?> DetectTableTypeAsync(byte[] fileData, string fileExtension);
        Task<List<SupportedTableInfo>> GetSupportedTablesAsync();
        bool IsTableSupported(string tableName);
    }

    public class DetectedTableInfo
    {
        public string TableType { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
        public List<string> DetectedColumns { get; set; } = new List<string>();
        public Dictionary<string, string> SuggestedMappings { get; set; } = new Dictionary<string, string>();
    }
}