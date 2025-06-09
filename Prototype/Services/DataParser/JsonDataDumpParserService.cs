using System.Text.Json;
using Prototype.DTOs;
using Prototype.Services.Interfaces;

namespace Prototype.Services.DataParser;

public class JsonDataDumpParserService : IDataDumpParserService
{
    public async Task<List<TableSchemaDto>> ParseAndInferSchemasAsync(ICollection<IFormFile> files)
    {
        var allSchemas = new List<TableSchemaDto>();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (file == null || file.Length == 0)
                continue;

            try
            {
                using var stream = file.OpenReadStream();
                var jsonDoc = await JsonDocument.ParseAsync(stream);

                if (jsonDoc.RootElement.ValueKind != JsonValueKind.Array)
                    continue;

                var firstRow = jsonDoc.RootElement.EnumerateArray().FirstOrDefault();
                if (firstRow.ValueKind != JsonValueKind.Object)
                    continue;

                var columns = new List<ColumnSchemaDto>();

                foreach (var prop in firstRow.EnumerateObject())
                {
                    columns.Add(new ColumnSchemaDto
                    {
                        ColumnName = prop.Name,
                        DataType = InferDataType(prop.Value)
                    });
                }

                // Handle duplicate file names
                var baseTableName = Path.GetFileNameWithoutExtension(file.FileName);
                var tableName = baseTableName;
                int suffix = 1;
                while (usedNames.Contains(tableName))
                {
                    tableName = $"{baseTableName}_{suffix++}";
                }
                usedNames.Add(tableName);

                var tableSchema = new TableSchemaDto
                {
                    TableName = tableName,
                    Columns = columns
                };

                allSchemas.Add(tableSchema);
            }
            catch (JsonException jex)
            {
                Console.WriteLine($"JSON parsing failed for {file?.FileName ?? "Unknown File"}: {jex.Message}");
            }
            catch (Exception ex)
            {
                if (ex is IOException && file != null)
                    Console.WriteLine($"File error for {file.FileName}: {ex.Message}");
                else if (ex is ArgumentException && file != null)
                    Console.WriteLine($"Duplicate file/table name detected for {file.FileName}: {ex.Message}");
                else
                    Console.WriteLine($"Error processing {file?.FileName ?? "Unknown File"}: {ex.Message}");
            }
        }

        return allSchemas;
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