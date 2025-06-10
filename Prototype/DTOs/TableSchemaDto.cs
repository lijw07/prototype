namespace Prototype.DTOs;

public class TableSchemaDto
{
    public string TableName { get; set; }
    public List<ColumnSchemaDto> Columns { get; set; }
}