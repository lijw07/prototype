using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace Prototype.Services.BulkUpload;

public class FileParsingService : IFileParsingService
{
    private readonly ILogger<FileParsingService> _logger;

    public FileParsingService(ILogger<FileParsingService> logger)
    {
        _logger = logger;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public DataTable? ParseFileToDataTable(byte[] fileData, string fileExtension)
    {
        try
        {
            _logger.LogInformation("Parsing file with extension: {FileExtension}", fileExtension);

            return fileExtension.ToLowerInvariant() switch
            {
                ".csv" => ParseCsvToDataTable(fileData),
                ".xlsx" or ".xls" => ParseExcelToDataTable(fileData),
                ".json" => ParseJsonToDataTable(fileData),
                ".xml" => ParseXmlToDataTable(fileData),
                _ => throw new NotSupportedException($"File extension {fileExtension} is not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing file with extension {FileExtension}", fileExtension);
            return null;
        }
    }

    private DataTable ParseCsvToDataTable(byte[] fileData)
    {
        using var stream = new MemoryStream(fileData);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null
        });

        using var dr = new CsvDataReader(csv);
        var dataTable = new DataTable();
        dataTable.Load(dr);
        
        _logger.LogInformation("Successfully parsed CSV file with {RowCount} rows", dataTable.Rows.Count);
        return dataTable;
    }

    private DataTable ParseExcelToDataTable(byte[] fileData)
    {
        using var stream = new MemoryStream(fileData);
        using var package = new ExcelPackage(stream);
        
        var worksheet = package.Workbook.Worksheets[0];
        var dataTable = new DataTable();

        // Add columns from header row
        for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
        {
            var headerValue = worksheet.Cells[1, col].Value?.ToString() ?? $"Column{col}";
            dataTable.Columns.Add(headerValue);
        }

        // Add data rows
        for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            var dataRow = dataTable.NewRow();
            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                dataRow[col - 1] = worksheet.Cells[row, col].Value?.ToString() ?? string.Empty;
            }
            dataTable.Rows.Add(dataRow);
        }

        _logger.LogInformation("Successfully parsed Excel file with {RowCount} rows", dataTable.Rows.Count);
        return dataTable;
    }

    private DataTable ParseJsonToDataTable(byte[] fileData)
    {
        var jsonString = Encoding.UTF8.GetString(fileData);
        using var document = JsonDocument.Parse(jsonString);

        var dataTable = new DataTable();
        var root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
        {
            // Get columns from first object
            var firstElement = root.EnumerateArray().First();
            if (firstElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in firstElement.EnumerateObject())
                {
                    dataTable.Columns.Add(property.Name);
                }

                // Add data rows
                foreach (var element in root.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.Object)
                    {
                        var row = dataTable.NewRow();
                        foreach (var property in element.EnumerateObject())
                        {
                            if (dataTable.Columns.Contains(property.Name))
                            {
                                row[property.Name] = property.Value.ToString();
                            }
                        }
                        dataTable.Rows.Add(row);
                    }
                }
            }
        }

        _logger.LogInformation("Successfully parsed JSON file with {RowCount} rows", dataTable.Rows.Count);
        return dataTable;
    }

    private DataTable ParseXmlToDataTable(byte[] fileData)
    {
        var xmlString = Encoding.UTF8.GetString(fileData);
        var dataSet = new DataSet();
        
        using var stringReader = new StringReader(xmlString);
        using var xmlReader = XmlReader.Create(stringReader);
        
        dataSet.ReadXml(xmlReader);

        if (dataSet.Tables.Count > 0)
        {
            var dataTable = dataSet.Tables[0];
            _logger.LogInformation("Successfully parsed XML file with {RowCount} rows", dataTable.Rows.Count);
            return dataTable;
        }

        _logger.LogWarning("No data tables found in XML file");
        return new DataTable();
    }
}