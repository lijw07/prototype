using CsvHelper;
using System.Globalization;
using System.Text;
using Prototype.DTOs;
using Prototype.Services.Interfaces;
using Prototype.Utility;

namespace Prototype.Services.DataParser;

public class CsvDataDumpParserService : IDataDumpParserService
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
        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: false);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        
        if (!await csv.ReadAsync() || !csv.ReadHeader())
            return new List<ColumnSchemaDto>();

        var header = csv.HeaderRecord;
        if (header == null || header.Length == 0)
            return new List<ColumnSchemaDto>();
        
        var dataRow = new string[header.Length];
        if (await csv.ReadAsync())
        {
            for (int i = 0; i < header.Length; i++)
                dataRow[i] = csv.GetField(i) ?? string.Empty;
        }
        
        var columns = new List<ColumnSchemaDto>();
        for (int i = 0; i < header.Length; i++)
        {
            columns.Add(new ColumnSchemaDto
            {
                ColumnName = header[i],
                DataType = DataTypeInferenceUtility.InferColumnDataType(dataRow.ElementAtOrDefault(i))
            });
        }

        return columns;
    }
}