using System.Text.Json;
using Prototype.DTOs;
using Prototype.Services.Interfaces;
using Prototype.Utility;

namespace Prototype.Services.DataParser;

public class JsonDataDumpParserService : IDataDumpParserService
{
    public async Task<List<TableSchemaDto>> ParseAndInferSchemasAsync(ICollection<IFormFile> files)
    {
        var processedSchemas = new List<TableSchemaDto>();
        var processedTableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (!IsValidFile(file)) continue;

            var tableName = GetTableNameFromFile(file);
            if (processedTableNames.Contains(tableName)) continue;

            try
            {
                var columns = await ExtractSchemaColumnsAsync(file);
                if (columns.Count == 0) continue;

                processedTableNames.Add(tableName);
                processedSchemas.Add(CreateTableSchema(tableName, columns));
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex, file?.FileName);
            }
        }

        return processedSchemas;
    }

    private bool IsValidFile(IFormFile? file)
    {
        return file != null && file.Length > 0;
    }

    private string GetTableNameFromFile(IFormFile file)
    {
        return Path.GetFileNameWithoutExtension(file.FileName);
    }

    private async Task<List<ColumnSchemaDto>> ExtractSchemaColumnsAsync(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        var jsonDoc = await JsonDocument.ParseAsync(stream);

        if (jsonDoc.RootElement.ValueKind != JsonValueKind.Array) return new List<ColumnSchemaDto>();

        var firstRow = jsonDoc.RootElement.EnumerateArray().FirstOrDefault();
        if (firstRow.ValueKind != JsonValueKind.Object) return new List<ColumnSchemaDto>();

        return GetColumnSchemasFromJsonObject(firstRow);
    }

    private List<ColumnSchemaDto> GetColumnSchemasFromJsonObject(JsonElement rowObject)
    {
        var columns = new List<ColumnSchemaDto>();
        foreach (var prop in rowObject.EnumerateObject())
        {
            columns.Add(new ColumnSchemaDto
            {
                ColumnName = prop.Name,
                DataType = InferColumnDataType(prop.Value)
            });
        }
        return columns;
    }

    private string InferColumnDataType(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => "string", // You can refine with extra logic if needed
            JsonValueKind.String => IsGuid(element.GetString()) ? "Guid" : "string",
            JsonValueKind.True or JsonValueKind.False => "bool",
            JsonValueKind.Object => "object",
            JsonValueKind.Array => "array",
            _ => "string"
        };
    }

    private bool IsGuid(string? value)
    {
        return Guid.TryParse(value, out _);
    }

    private TableSchemaDto CreateTableSchema(string tableName, List<ColumnSchemaDto> columns)
    {
        return new TableSchemaDto
        {
            TableName = tableName,
            Columns = columns
        };
    }
}