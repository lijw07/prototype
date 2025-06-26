using System.Data;
using System.Text;
using System.Text.Json;
using System.Xml;
using Microsoft.Extensions.Options;
using OfficeOpenXml;
using Prototype.Configuration;
using Prototype.DTOs.BulkUpload;
using Prototype.Exceptions;

namespace Prototype.Services.BulkUpload;

/// <summary>
/// Single responsibility: Parse files into DataTable format
/// Extracted from the massive BulkUploadService
/// </summary>
public interface IFileParsingService
{
    Task<DataTable> ParseFileToDataTableAsync(BulkUploadRequest request);
    bool IsValidFileExtension(string extension);
    Task<int> GetEstimatedRowCountAsync(byte[] fileData, string fileExtension);
}

public class FileParsingService : IFileParsingService
{
    private readonly BulkUploadConfiguration _configuration;
    private readonly ILogger<FileParsingService> _logger;

    public FileParsingService(
        IOptions<BulkUploadConfiguration> settings,
        ILogger<FileParsingService> logger)
    {
        _configuration = settings.Value;
        _logger = logger;
    }

    public async Task<DataTable> ParseFileToDataTableAsync(BulkUploadRequest request)
    {
        _logger.LogDebug("Parsing file: {FileName} ({Extension})", 
            request.FileName, request.FileExtension);

        if (!IsValidFileExtension(request.FileExtension))
        {
            throw new ValidationException($"Unsupported file extension: {request.FileExtension}");
        }

        if (request.FileData.Length > _configuration.MaxFileSize)
        {
            throw new ValidationException($"File size exceeds maximum allowed size of {_configuration.MaxFileSize / 1_000_000}MB");
        }

        try
        {
            return request.FileExtension.ToLowerInvariant() switch
            {
                ".csv" => await ParseCsvToDataTableAsync(request.FileData),
                ".json" => await ParseJsonToDataTableAsync(request.FileData),
                ".xml" => await ParseXmlToDataTableAsync(request.FileData),
                ".xlsx" or ".xls" => await ParseExcelToDataTableAsync(request.FileData),
                _ => throw new ValidationException($"Unsupported file type: {request.FileExtension}")
            };
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to parse file {FileName}", request.FileName);
            throw new ValidationException($"Failed to parse file: {ex.Message}");
        }
    }

    public bool IsValidFileExtension(string extension)
    {
        return _configuration.AllowedFileExtensions.Contains(extension.ToLowerInvariant());
    }

    public async Task<int> GetEstimatedRowCountAsync(byte[] fileData, string fileExtension)
    {
        try
        {
            return fileExtension.ToLowerInvariant() switch
            {
                ".csv" => await EstimateCsvRowCountAsync(fileData),
                ".json" => await EstimateJsonRowCountAsync(fileData),
                ".xml" => await EstimateXmlRowCountAsync(fileData),
                ".xlsx" or ".xls" => await EstimateExcelRowCountAsync(fileData),
                _ => 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to estimate row count for file extension {Extension}", fileExtension);
            return 0;
        }
    }

    private async Task<DataTable> ParseCsvToDataTableAsync(byte[] fileData)
    {
        var dataTable = new DataTable();
        var content = Encoding.UTF8.GetString(fileData);
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0)
            throw new ValidationException("CSV file is empty");

        // Parse header
        var headers = ParseCsvLine(lines[0]);
        foreach (var header in headers)
        {
            dataTable.Columns.Add(header.Trim());
        }

        // Parse data rows
        for (int i = 1; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i]);
            if (values.Length == headers.Length)
            {
                dataTable.Rows.Add(values);
            }
        }

        _logger.LogDebug("Parsed CSV: {Rows} rows, {Columns} columns", 
            dataTable.Rows.Count, dataTable.Columns.Count);

        return dataTable;
    }

    private async Task<DataTable> ParseJsonToDataTableAsync(byte[] fileData)
    {
        var json = Encoding.UTF8.GetString(fileData);
        using var document = JsonDocument.Parse(json);
        
        var dataTable = new DataTable();
        var root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
        {
            // Get column names from first object
            var firstItem = root[0];
            foreach (var property in firstItem.EnumerateObject())
            {
                dataTable.Columns.Add(property.Name);
            }

            // Add data rows
            foreach (var item in root.EnumerateArray())
            {
                var row = dataTable.NewRow();
                foreach (var property in item.EnumerateObject())
                {
                    row[property.Name] = property.Value.ToString();
                }
                dataTable.Rows.Add(row);
            }
        }

        _logger.LogDebug("Parsed JSON: {Rows} rows, {Columns} columns", 
            dataTable.Rows.Count, dataTable.Columns.Count);

        return dataTable;
    }

    private async Task<DataTable> ParseXmlToDataTableAsync(byte[] fileData)
    {
        var xml = Encoding.UTF8.GetString(fileData);
        var dataTable = new DataTable();
        
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xml);

        // Assuming XML structure like: <root><item><field>value</field></item></root>
        var nodes = xmlDoc.SelectNodes("//item") ?? xmlDoc.DocumentElement?.ChildNodes;
        
        if (nodes != null && nodes.Count > 0)
        {
            // Get columns from first node
            var firstNode = nodes[0];
            if (firstNode?.ChildNodes != null)
            {
                foreach (XmlNode child in firstNode.ChildNodes)
                {
                    if (child.NodeType == XmlNodeType.Element)
                    {
                        dataTable.Columns.Add(child.Name);
                    }
                }

                // Add data rows
                foreach (XmlNode node in nodes)
                {
                    if (node.ChildNodes != null)
                    {
                        var row = dataTable.NewRow();
                        foreach (XmlNode child in node.ChildNodes)
                        {
                            if (child.NodeType == XmlNodeType.Element)
                            {
                                row[child.Name] = child.InnerText;
                            }
                        }
                        dataTable.Rows.Add(row);
                    }
                }
            }
        }

        _logger.LogDebug("Parsed XML: {Rows} rows, {Columns} columns", 
            dataTable.Rows.Count, dataTable.Columns.Count);

        return dataTable;
    }

    private async Task<DataTable> ParseExcelToDataTableAsync(byte[] fileData)
    {
        var dataTable = new DataTable();
        
        using var stream = new MemoryStream(fileData);
        using var package = new ExcelPackage(stream);
        
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
            throw new ValidationException("Excel file contains no worksheets");

        var startRow = worksheet.Dimension?.Start.Row ?? 1;
        var endRow = worksheet.Dimension?.End.Row ?? 1;
        var startCol = worksheet.Dimension?.Start.Column ?? 1;
        var endCol = worksheet.Dimension?.End.Column ?? 1;

        // Add columns
        for (int col = startCol; col <= endCol; col++)
        {
            var headerValue = worksheet.Cells[startRow, col].Value?.ToString() ?? $"Column{col}";
            dataTable.Columns.Add(headerValue);
        }

        // Add data rows
        for (int row = startRow + 1; row <= endRow; row++)
        {
            var dataRow = dataTable.NewRow();
            for (int col = startCol; col <= endCol; col++)
            {
                dataRow[col - startCol] = worksheet.Cells[row, col].Value?.ToString() ?? string.Empty;
            }
            dataTable.Rows.Add(dataRow);
        }

        _logger.LogDebug("Parsed Excel: {Rows} rows, {Columns} columns", 
            dataTable.Rows.Count, dataTable.Columns.Count);

        return dataTable;
    }

    private string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        
        result.Add(current.ToString());
        return result.ToArray();
    }

    private async Task<int> EstimateCsvRowCountAsync(byte[] fileData)
    {
        var content = Encoding.UTF8.GetString(fileData);
        return content.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length - 1; // Subtract header
    }

    private async Task<int> EstimateJsonRowCountAsync(byte[] fileData)
    {
        var json = Encoding.UTF8.GetString(fileData);
        using var document = JsonDocument.Parse(json);
        return document.RootElement.ValueKind == JsonValueKind.Array 
            ? document.RootElement.GetArrayLength() 
            : 1;
    }

    private async Task<int> EstimateXmlRowCountAsync(byte[] fileData)
    {
        var xml = Encoding.UTF8.GetString(fileData);
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xml);
        return xmlDoc.SelectNodes("//item")?.Count ?? 0;
    }

    private async Task<int> EstimateExcelRowCountAsync(byte[] fileData)
    {
        using var stream = new MemoryStream(fileData);
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        return (worksheet?.Dimension?.End.Row ?? 1) - 1; // Subtract header
    }
}