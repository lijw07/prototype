namespace Prototype.DTOs.BulkUpload
{
    public class SupportedTableInfo
    {
        public string TableName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public List<TableColumnInfo> Columns { get; set; } = new List<TableColumnInfo>();
        public List<string> RequiredColumns { get; set; } = new List<string>();
        public string Description { get; set; } = string.Empty;
        public bool SupportsUpdate { get; set; }
        public string PrimaryKeyColumn { get; set; } = string.Empty;
    }

    public class TableColumnInfo
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
}