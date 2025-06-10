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
            if (!DataTypeInferenceUtility.IsValidFile(file)) continue;
            var tableName = DataTypeInferenceUtility.GetTableNameFromFile(file);
            if (processedTableNames.Contains(tableName)) continue;

            try
            {
                var columns = await ExtractSchemaColumnsAsync(file);
                if (columns.Count == 0) continue;

                processedTableNames.Add(tableName);
                processedSchemas.Add(DataTypeInferenceUtility.CreateTableSchema(tableName, columns));
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex, file?.FileName);
            }
        }

        return processedSchemas;
    }

    private async Task<List<ColumnSchemaDto>> ExtractSchemaColumnsAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        var jsonDoc = await JsonDocument.ParseAsync(stream);

        if (jsonDoc.RootElement.ValueKind != JsonValueKind.Array) return new List<ColumnSchemaDto>();

        var firstRow = jsonDoc.RootElement.EnumerateArray().FirstOrDefault();
        if (firstRow.ValueKind != JsonValueKind.Object) return new List<ColumnSchemaDto>();

        return GetColumnSchemasFromJsonObject(firstRow);
    }

    private List<ColumnSchemaDto> GetColumnSchemasFromJsonObject(JsonElement rowObject)
    {
        return rowObject.EnumerateObject().Select(prop => new ColumnSchemaDto { ColumnName = prop.Name, DataType = DataTypeInferenceUtility.InferColumnDataType(prop.Value.ToString()) }).ToList();
    }
}