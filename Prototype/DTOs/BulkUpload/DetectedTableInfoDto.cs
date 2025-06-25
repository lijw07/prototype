namespace Prototype.DTOs.BulkUpload;

public class DetectedTableInfoDto
{
    public string TableType { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public List<string> DetectedColumns { get; set; } = new List<string>();
    public Dictionary<string, string> SuggestedMappings { get; set; } = new Dictionary<string, string>();
}