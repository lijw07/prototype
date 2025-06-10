using Prototype.DTOs;

namespace Prototype.Utility;

public static class DataTypeInferenceUtility
{
    public static string InferColumnDataType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "string";
        if (Guid.TryParse(value, out _))
            return "Guid";
        if (bool.TryParse(value, out _))
            return "bool";
        if (int.TryParse(value, out _))
            return "int";
        if (double.TryParse(value, out _))
            return "double";
        if (DateTime.TryParse(value, out _))
            return "DateTime";
        return "string";
    }
    
    public static TableSchemaDto CreateTableSchema(string tableName, List<ColumnSchemaDto> columns)
    {
        return new TableSchemaDto
        {
            TableName = tableName,
            Columns = columns
        };
    }
    
    public static bool IsValidFile(IFormFile? file)
    {
        return file != null && file.Length > 0;
    }
    
    public static string GetTableNameFromFile(IFormFile file)
    {
        return Path.GetFileNameWithoutExtension(file.FileName);
    }
}