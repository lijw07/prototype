using ClosedXML.Excel;
using Prototype.DTOs;
using Prototype.Services.Interfaces;
using Prototype.Utility;

namespace Prototype.Services.DataParser;

public class ExcelDataDumpParserService : IDataDumpParserService
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
                await using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null) continue;

                var columns = new List<ColumnSchemaDto>();
                int colCount = worksheet.Row(1).CellCount();
                
                for (int col = 1; col <= colCount; col++)
                {
                    var colName = worksheet.Cell(1, col).GetString();
                    var value = worksheet.Cell(2, col).GetString();
                    columns.Add(new ColumnSchemaDto
                    {
                        ColumnName = colName,
                        DataType = DataTypeInferenceUtility.InferColumnDataType(value)
                    });
                }

                processedTableNames.Add(tableName);
                processedSchemas.Add(new TableSchemaDto
                {
                    TableName = tableName,
                    Columns = columns
                });
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex, file?.FileName);
            }
        }
        return processedSchemas;
    }
}