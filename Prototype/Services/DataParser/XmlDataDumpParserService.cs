using System.Xml.Linq;
using Prototype.DTOs;
using Prototype.Services.Interfaces;
using Prototype.Utility;

namespace Prototype.Services.DataParser;

public class XmlDataDumpParserService : IDataDumpParserService
{
    public async Task<List<TableSchemaDto>> ParseAndInferSchemasAsync(ICollection<IFormFile> files)
    {
        var processedSchemas = new List<TableSchemaDto>();
        var processedTableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await foreach (var file in GetFilesAsync(files))
        {
            if (!DataTypeInferenceUtility.IsValidFile(file)) continue;
            var tableName = DataTypeInferenceUtility.GetTableNameFromFile(file);
            if (processedTableNames.Contains(tableName)) continue;

            try
            {
                await using var stream = file.OpenReadStream();
                var doc = await XDocument.LoadAsync(stream, LoadOptions.None, default);
                
                var firstRow = doc.Root?.Elements().FirstOrDefault();
                if (firstRow == null) continue;

                var columns = firstRow.Elements().Select(col => new ColumnSchemaDto { ColumnName = col.Name.LocalName, DataType = DataTypeInferenceUtility.InferColumnDataType(col.Value) }).ToList();
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

    private async IAsyncEnumerable<IFormFile> GetFilesAsync(ICollection<IFormFile> files)
    {
        foreach (var file in files)
        {
            await Task.Yield();
            yield return file;
        }
    }
}