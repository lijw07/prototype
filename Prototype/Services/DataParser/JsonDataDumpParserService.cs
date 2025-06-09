using System.Text.Json;
using Prototype.DTOs;
using Prototype.Services.Interfaces;

namespace Prototype.Services.DataParser;

public class JsonDataDumpParserService : IDataDumpParserService
{
    public async Task<List<TableSchemaDto>> ParseAndInferSchemasAsync(ICollection<IFormFile> files)
    {
        var allSchemas = new List<TableSchemaDto>();

        foreach (var file in files)
        {
            if (file == null || file.Length == 0)
                continue;

            using var stream = file.OpenReadStream();
            try
            {
                var jsonDocument = await JsonDocument.ParseAsync(stream);
                var root = jsonDocument.RootElement;

                var schema = new TableSchemaDto
                {
                    TableName = file.FileName,
                    Columns = new List<ColumnSchemaDto>()
                };

                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    var firstObject = root[0];
                    if (firstObject.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var property in firstObject.EnumerateObject())
                        {
                            schema.Columns.Add(new ColumnSchemaDto
                            {
                                ColumnName = property.Name,
                                DataType = property.Value.ValueKind.ToString()
                            });
                        }
                    }
                }

                allSchemas.Add(schema);
            }
            catch (JsonException)
            {
                // Handle or log JSON parsing errors if necessary
                // For now, skip this file
                continue;
            }
        }

        return allSchemas;
    }
}