namespace Prototype.DTOs.BulkUpload;

public class SupportedTableInfoDto
{
    public string TableName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<TableColumnInfoDto> Columns { get; set; } = new List<TableColumnInfoDto>();
    public List<string> RequiredColumns { get; set; } = new List<string>();
    public string Description { get; set; } = string.Empty;
    public bool SupportsUpdate { get; set; }
    public string PrimaryKeyColumn { get; set; } = string.Empty;
}