using OfficeOpenXml;
using Prototype.DTOs.BulkUpload;
using Prototype.Services.BulkUpload.Mappers;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class BulkTemplateService(
    ITableMappingService tableMappingService,
    ILogger<BulkTemplateService> logger) : IBulkTemplateService
{
    private readonly Dictionary<string, string> _supportedTableTypes = new()
    {
        { "Users", "User management data" },
        { "Applications", "Application configuration data" },
        { "UserApplications", "User-application relationship data" },
        { "TemporaryUsers", "Temporary user registration data" },
        { "UserRoles", "User role assignment data" }
    };

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

                // Create header row with styling
                CreateHeaderRow(worksheet, columns);

                // Add example data rows
                AddExampleDataRows(worksheet, columns, exampleData);

                // Add data validation and formatting
                ApplyDataValidationAndFormatting(worksheet, columns);

                // Auto-fit columns for better readability
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Add instructions sheet
                CreateInstructionsSheet(package, tableType, columns);

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

    public List<string> GetSupportedTableTypes()
    {
        return _supportedTableTypes.Keys.ToList();
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

    private void AddExampleDataRows(ExcelWorksheet worksheet, List<ColumnDefinition> columns, List<Dictionary<string, object?>>? exampleData)
    {
        if (exampleData == null || !exampleData.Any())
        {
            // Add one empty row as template
            for (int i = 0; i < columns.Count; i++)
            {
                var cell = worksheet.Cells[2, i + 1];
                cell.Value = GetPlaceholderValue(columns[i]);
                cell.Style.Font.Italic = true;
                cell.Style.Font.Color.SetColor(System.Drawing.Color.Gray);
            }
            return;
        }

        var rowIndex = 2;
        foreach (var example in exampleData.Take(5)) // Limit to 5 example rows
        {
            for (int i = 0; i < columns.Count; i++)
            {
                var columnName = columns[i].ColumnName;
                var cell = worksheet.Cells[rowIndex, i + 1];
                
                if (example.ContainsKey(columnName))
                {
                    cell.Value = example[columnName]?.ToString() ?? string.Empty;
                }
                else
                {
                    cell.Value = GetPlaceholderValue(columns[i]);
                    cell.Style.Font.Italic = true;
                    cell.Style.Font.Color.SetColor(System.Drawing.Color.Gray);
                }

                // Add borders for data cells
                cell.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            }
            rowIndex++;
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

    private void CreateInstructionsSheet(ExcelPackage package, string tableType, List<ColumnDefinition> columns)
    {
        var instructionsSheet = package.Workbook.Worksheets.Add("Instructions");
        
        // Title
        instructionsSheet.Cells[1, 1].Value = $"{tableType} Template Instructions";
        instructionsSheet.Cells[1, 1].Style.Font.Bold = true;
        instructionsSheet.Cells[1, 1].Style.Font.Size = 16;

        // General instructions
        var instructions = new[]
        {
            "",
            "General Instructions:",
            "1. Fill in your data starting from row 2 in the main sheet",
            "2. Do not modify the column headers in row 1",
            "3. Required fields are marked with * and highlighted in yellow",
            "4. Follow the data format specifications below",
            "5. Remove example data before uploading",
            "",
            "Column Specifications:"
        };

        var row = 2;
        foreach (var instruction in instructions)
        {
            instructionsSheet.Cells[row, 1].Value = instruction;
            if (instruction.EndsWith(":"))
            {
                instructionsSheet.Cells[row, 1].Style.Font.Bold = true;
            }
            row++;
        }

        // Column details
        foreach (var column in columns)
        {
            instructionsSheet.Cells[row, 1].Value = $"• {column.ColumnName}";
            instructionsSheet.Cells[row, 2].Value = column.Description ?? "No description";
            instructionsSheet.Cells[row, 3].Value = column.DataType ?? "string";
            instructionsSheet.Cells[row, 4].Value = column.IsRequired ? "Required" : "Optional";
            
            if (column.IsRequired)
            {
                instructionsSheet.Cells[row, 4].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                instructionsSheet.Cells[row, 4].Style.Font.Bold = true;
            }
            
            row++;
        }

        // Auto-fit columns
        instructionsSheet.Cells[instructionsSheet.Dimension.Address].AutoFitColumns();
    }

    private string GetPlaceholderValue(ColumnDefinition column)
    {
        return column.DataType?.ToLower() switch
        {
            "datetime" or "date" => DateTime.Now.ToString("yyyy-MM-dd"),
            "bool" or "boolean" => "true",
            "int" or "integer" => "1",
            "decimal" or "double" or "float" => "0.00",
            "guid" => Guid.NewGuid().ToString(),
            _ => $"Sample {column.ColumnName}"
        };
    }
}