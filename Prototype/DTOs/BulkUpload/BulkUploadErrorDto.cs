namespace Prototype.DTOs.BulkUpload;

public class BulkUploadErrorDto
{
    public int RowNumber { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public Dictionary<string, object>? RowData { get; set; }
    public string? FileName { get; set; }
}