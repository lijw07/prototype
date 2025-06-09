using System.Text.Json;
using Prototype.DTOs;
using Prototype.Services.Interfaces;

namespace Prototype.Services.DataParser;

public class JsonDataDumpParserService : IDataDumpParserService
{
    public async Task<List<TableSchemaDto>> ParseAndInferSchemasAsync(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        var jsonDoc = await JsonDocument.ParseAsync(stream);
        
        if (jsonDoc.RootElement.ValueKind != JsonValueKind.Array)
            return new List<TableSchemaDto>();

        var firstRow = jsonDoc.RootElement.EnumerateArray().FirstOrDefault();
        if (firstRow.ValueKind != JsonValueKind.Object)
            return new List<TableSchemaDto>();

        var columns = new List<ColumnSchemaDto>();

        foreach (var prop in firstRow.EnumerateObject())
        {
            columns.Add(new ColumnSchemaDto
            {
                ColumnName = prop.Name,
                DataType = InferDataType(prop.Value)
            });
        }

        
        var tableSchema = new TableSchemaDto
        {
            TableName = Path.GetFileNameWithoutExtension(file.FileName),
            Columns = columns
        };

        return new List<TableSchemaDto> { tableSchema };
    }

    private string InferDataType(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => "number",
            JsonValueKind.String => IsGuid(element.GetString()) ? "Guid" : "string",
            JsonValueKind.True or JsonValueKind.False => "bool",
            JsonValueKind.Object => "object",
            JsonValueKind.Array => "array",
            _ => "string"
        };
    }

    private bool IsGuid(string? value)
    {
        if (value == null) return false;
        return Guid.TryParse(value, out _);
    }
}