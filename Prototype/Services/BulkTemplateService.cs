using Microsoft.SqlServer.TransactSql.ScriptDom;
using OfficeOpenXml;
using Prototype.DTOs.BulkUpload;
using Prototype.Services.BulkUpload;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class BulkTemplateService(
    ITableMappingService tableMappingService,
    ILogger<BulkTemplateService> logger) : IBulkTemplateService
{
    static BulkTemplateService()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<FileTemplateInfo?> GenerateTemplateAsync(string tableType)
    {
        try
        {
            var mapper = tableMappingService.GetMapper(tableType);
            if (mapper == null)
            {
                logger.LogWarning("No mapper found for table type: {TableType}", tableType);
                return null;
            }

            return await GenerateExcelTemplateAsync(mapper);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating template for table type: {TableType}", tableType);
            return null;
        }
    }
    
    public async Task<FileTemplateInfo?> GenerateExcelTemplateAsync(ITableMapper mapper)
    {
        try
        {
            var tableType = mapper.GetType().Name.Replace("TableMapper", "");
            var columns = mapper.GetTemplateColumns();
            var exampleData = mapper.GetExampleData();

            var fileContent = await CreateExcelTemplateWithSampleDataAsync(mapper);
            
            return new FileTemplateInfo
            {
                Content = fileContent,
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = $"{tableType}_Template_{DateTime.UtcNow:yyyyMMdd}.xlsx"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating Excel template for mapper: {MapperType}", mapper.GetType().Name);
            return null;
        }
    }

    public async Task<byte[]> CreateExcelTemplateWithSampleDataAsync(ITableMapper mapper)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var package = new ExcelPackage();
                var tableType = mapper.GetType().Name.Replace("TableMapper", "");
                var worksheet = package.Workbook.Worksheets.Add(tableType);

                var columns = mapper.GetTemplateColumns();
                var exampleData = mapper.GetExampleData();

                // Create a header row with styling
                CreateHeaderRow(worksheet, columns);

                // Add data validation and formatting
                ApplyDataValidationAndFormatting(worksheet, columns);

                // Auto-fit columns for better readability
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                logger.LogInformation("Successfully created Excel template for {TableType} with {ColumnCount} columns and {ExampleRowCount} example rows",
                    tableType, columns.Count, exampleData?.Count ?? 0);

                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating Excel template with sample data");
                throw;
            }
        });
    }

    private void CreateHeaderRow(ExcelWorksheet worksheet, List<ColumnDefinition> columns)
    {
        for (int i = 0; i < columns.Count; i++)
        {
            var cell = worksheet.Cells[1, i + 1];
            cell.Value = columns[i].ColumnName;
            
            // Header styling
            cell.Style.Font.Bold = true;
            cell.Style.Font.Size = 12;
            cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            cell.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

            // Add column description as comment if available
            if (!string.IsNullOrEmpty(columns[i].Description))
            {
                var comment = cell.AddComment(columns[i].Description, "Template Generator");
                comment.AutoFit = true;
            }

            // Add required field indicator
            if (columns[i].IsRequired)
            {
                cell.Value = $"{columns[i].ColumnName} *";
                cell.Style.Font.Color.SetColor(System.Drawing.Color.Red);
            }
        }
    }

    private void ApplyDataValidationAndFormatting(ExcelWorksheet worksheet, List<ColumnDefinition> columns)
    {
        for (int i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            var columnRange = worksheet.Cells[2, i + 1, worksheet.Dimension?.End?.Row ?? 1000, i + 1];

            // Apply data type specific formatting
            switch (column.DataType?.ToLower())
            {
                case "datetime":
                case "date":
                    columnRange.Style.Numberformat.Format = "yyyy-mm-dd";
                    break;
                case "decimal":
                case "double":
                case "float":
                    columnRange.Style.Numberformat.Format = "#,##0.00";
                    break;
                case "int":
                case "integer":
                    columnRange.Style.Numberformat.Format = "#,##0";
                    break;
                case "bool":
                case "boolean":
                    // Create dropdown for boolean values
                    var boolValidation = columnRange.DataValidation.AddListDataValidation();
                    boolValidation.Formula.Values.Add("true");
                    boolValidation.Formula.Values.Add("false");
                    boolValidation.ShowErrorMessage = true;
                    boolValidation.Error = "Please select true or false";
                    break;
            }

            // Add validation for required fields
            if (column.IsRequired)
            {
                columnRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                columnRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
            }
        }
    }
}