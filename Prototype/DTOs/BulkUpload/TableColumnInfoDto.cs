namespace Prototype.DTOs.BulkUpload;

public class TableColumnInfoDto
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsUnique { get; set; }
    public int? MaxLength { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
    public List<string>? AllowedValues { get; set; }
}