namespace Prototype.DTOs.BulkUpload;

public enum ErrorCategoryEnum
{
    ValidationError = 0,
    DuplicateRecord = 1,
    SystemError = 2,
    ProcessingError = 3
}

public class BulkUploadErrorDto
{
    public int RowNumber { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public Dictionary<string, object>? RowData { get; set; }
    public string? FileName { get; set; }
    public ErrorCategoryEnum ErrorCategory { get; set; } = ErrorCategoryEnum.ValidationError;
}