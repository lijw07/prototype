using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;
using CsvHelper;
using CsvHelper.Configuration;
using OfficeOpenXml;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class FileParsingService(ILogger<FileParsingService> logger) : IFileParsingService
{
    private readonly string[] _supportedExtensions = { ".csv", ".json", ".xml", ".xlsx", ".xls" };

    static FileParsingService()
    {
        // Set EPPlus license context for non-commercial use
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<DataTable?> ParseFileAsync(byte[] fileData, string fileExtension)
    {
        if (!IsValidFileExtension(fileExtension))
        {
            logger.LogWarning("Unsupported file extension: {FileExtension}", fileExtension);
            return null;
        }

        try
        {
            return fileExtension.ToLower() switch
            {
                ".csv" => await ParseCsvAsync(fileData),
                ".json" => await ParseJsonAsync(fileData),
                ".xml" => await ParseXmlAsync(fileData),
                ".xlsx" or ".xls" => await ParseExcelAsync(fileData),
                _ => null
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing file with extension {FileExtension}", fileExtension);
            return null;
        }
    }

    public async Task<DataTable?> ParseCsvAsync(byte[] fileData)
    {
        return await Task.Run(() =>
        {
            try
            {
                var dataTable = new DataTable();
                
                using var memoryStream = new MemoryStream(fileData);
                using var reader = new StreamReader(memoryStream, Encoding.UTF8);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    BadDataFound = null,
                    MissingFieldFound = null,
                    HeaderValidated = null
                });

                using var dr = new CsvDataReader(csv);
                dataTable.Load(dr);

                logger.LogInformation("Successfully parsed CSV file with {RowCount} rows and {ColumnCount} columns", 
                    dataTable.Rows.Count, dataTable.Columns.Count);

                return dataTable;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error parsing CSV file");
                throw;
            }
        });
    }

    public async Task<DataTable?> ParseExcelAsync(byte[] fileData)
    {
        return await Task.Run(() =>
        {
            try
            {
                var dataTable = new DataTable();

                using var memoryStream = new MemoryStream(fileData);
                using var package = new ExcelPackage(memoryStream);
                
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null || worksheet.Dimension == null)
                {
                    logger.LogWarning("Excel file contains no data or worksheets");
                    return dataTable;
                }

                var rowCount = worksheet.Dimension.End.Row;
                var colCount = worksheet.Dimension.End.Column;
                
                logger.LogInformation("Processing Excel file with {RowCount} rows and {ColCount} columns", rowCount, colCount);

                // Add columns with proper data types
                for (int col = 1; col <= colCount; col++)
                {
                    var columnName = worksheet.Cells[1, col].Value?.ToString() ?? $"Column{col}";
                    dataTable.Columns.Add(columnName, typeof(string));
                }

                // Memory-optimized batch processing
                var batchSize = CalculateMemoryOptimizedBatchSize(
                    GetAvailableMemoryMb(), 
                    EstimateRowSizeMb(colCount));
                
                for (int startRow = 2; startRow <= rowCount; startRow += batchSize)
                {
                    var endRow = Math.Min(startRow + batchSize - 1, rowCount);
                    
                    var batchRows = new List<object[]>(endRow - startRow + 1);
                    
                    for (int row = startRow; row <= endRow; row++)
                    {
                        var rowData = new object[colCount];
                        for (int col = 1; col <= colCount; col++)
                        {
                            var cellValue = worksheet.Cells[row, col].Value;
                            rowData[col - 1] = cellValue?.ToString() ?? string.Empty;
                        }
                        batchRows.Add(rowData);
                    }
                    
                    // Add batch to DataTable efficiently
                    foreach (var rowData in batchRows)
                    {
                        dataTable.Rows.Add(rowData);
                    }
                    
                    // Force garbage collection for large files
                    if (startRow % (batchSize * 10) == 0)
                    {
                        GC.Collect(0, GCCollectionMode.Optimized);
                        GC.WaitForPendingFinalizers();
                    }
                    
                    logger.LogDebug("Processed Excel rows {StartRow}-{EndRow} of {TotalRows}", 
                        startRow, endRow, rowCount);
                }

                logger.LogInformation("Successfully parsed Excel file with {RowCount} data rows", dataTable.Rows.Count);
                return dataTable;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error parsing Excel file");
                throw;
            }
        });
    }

    public async Task<DataTable?> ParseJsonAsync(byte[] fileData)
    {
        return await Task.Run(() =>
        {
            try
            {
                var dataTable = new DataTable();
                var jsonString = Encoding.UTF8.GetString(fileData);
                
                using var document = JsonDocument.Parse(jsonString);
                
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                {
                    logger.LogError("JSON file must contain an array of objects at the root level");
                    return null;
                }

                var elements = document.RootElement.EnumerateArray().ToList();
                if (!elements.Any())
                {
                    logger.LogWarning("JSON file contains an empty array");
                    return dataTable;
                }

                // Extract column names from the first object
                var firstElement = elements.First();
                if (firstElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in firstElement.EnumerateObject())
                    {
                        dataTable.Columns.Add(property.Name, typeof(string));
                    }
                }

                // Process each JSON object
                foreach (var element in elements)
                {
                    if (element.ValueKind == JsonValueKind.Object)
                    {
                        var rowData = new object[dataTable.Columns.Count];
                        
                        for (int i = 0; i < dataTable.Columns.Count; i++)
                        {
                            var columnName = dataTable.Columns[i].ColumnName;
                            if (element.TryGetProperty(columnName, out var property))
                            {
                                rowData[i] = property.ValueKind switch
                                {
                                    JsonValueKind.String => property.GetString() ?? string.Empty,
                                    JsonValueKind.Number => property.GetRawText(),
                                    JsonValueKind.True => "true",
                                    JsonValueKind.False => "false",
                                    JsonValueKind.Null => string.Empty,
                                    _ => property.GetRawText()
                                };
                            }
                            else
                            {
                                rowData[i] = string.Empty;
                            }
                        }
                        
                        dataTable.Rows.Add(rowData);
                    }
                }

                logger.LogInformation("Successfully parsed JSON file with {RowCount} rows and {ColumnCount} columns", 
                    dataTable.Rows.Count, dataTable.Columns.Count);

                return dataTable;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error parsing JSON file");
                throw;
            }
        });
    }

    public async Task<DataTable?> ParseXmlAsync(byte[] fileData)
    {
        return await Task.Run(() =>
        {
            try
            {
                var dataTable = new DataTable();
                var xmlString = Encoding.UTF8.GetString(fileData);
                
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlString);
                
                var rootElement = xmlDoc.DocumentElement;
                if (rootElement == null)
                {
                    logger.LogError("XML file has no root element");
                    return null;
                }

                // Find the first repeating element to use as row data
                var sampleNode = rootElement.FirstChild;
                if (sampleNode == null)
                {
                    logger.LogWarning("XML file contains no data elements");
                    return dataTable;
                }

                // Create columns based on the first data element
                if (sampleNode.NodeType == XmlNodeType.Element)
                {
                    foreach (XmlNode childNode in sampleNode.ChildNodes)
                    {
                        if (childNode.NodeType == XmlNodeType.Element)
                        {
                            dataTable.Columns.Add(childNode.Name, typeof(string));
                        }
                    }
                }

                // Process each data element
                foreach (XmlNode node in rootElement.ChildNodes)
                {
                    if (node.NodeType == XmlNodeType.Element)
                    {
                        var rowData = new object[dataTable.Columns.Count];
                        
                        for (int i = 0; i < dataTable.Columns.Count; i++)
                        {
                            var columnName = dataTable.Columns[i].ColumnName;
                            var childNode = node.SelectSingleNode(columnName);
                            rowData[i] = childNode?.InnerText ?? string.Empty;
                        }
                        
                        dataTable.Rows.Add(rowData);
                    }
                }

                logger.LogInformation("Successfully parsed XML file with {RowCount} rows and {ColumnCount} columns", 
                    dataTable.Rows.Count, dataTable.Columns.Count);

                return dataTable;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error parsing XML file");
                throw;
            }
        });
    }

    public bool IsValidFileExtension(string fileExtension)
    {
        return _supportedExtensions.Contains(fileExtension.ToLower());
    }

    public int CalculateMemoryOptimizedBatchSize(int availableMemoryMb, int estimatedRowSizeMb)
    {
        // Calculate safe batch size based on available memory
        // Use 25% of available memory to be conservative
        var safeMemoryMb = availableMemoryMb / 4;
        var batchSize = Math.Max(100, safeMemoryMb / Math.Max(1, estimatedRowSizeMb));
        
        // Cap at reasonable limits
        return Math.Min(batchSize, 10000);
    }

    private int GetAvailableMemoryMb()
    {
        // Estimate available memory (simplified)
        var totalMemory = GC.GetTotalMemory(false);
        var availableMemory = Math.Max(100, (int)((Environment.WorkingSet - totalMemory) / 1024 / 1024));
        return Math.Min(availableMemory, 512); // Cap at 512MB for safety
    }

    private int EstimateRowSizeMb(int columnCount)
    {
        // Rough estimate: assume average 50 characters per column
        var estimatedRowSizeBytes = columnCount * 50 * sizeof(char);
        return Math.Max(1, estimatedRowSizeBytes / 1024 / 1024);
    }
}