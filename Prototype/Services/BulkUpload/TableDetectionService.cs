using System.Data;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using OfficeOpenXml;
using Prototype.DTOs.BulkUpload;
using Prototype.Data;
using Microsoft.EntityFrameworkCore;

namespace Prototype.Services.BulkUpload
{
    public class TableDetectionService : ITableDetectionService
    {
        private readonly SentinelContext _context;
        private readonly ILogger<TableDetectionService> _logger;
        private readonly Dictionary<string, SupportedTableInfo> _supportedTables;

        public TableDetectionService(SentinelContext context, ILogger<TableDetectionService> logger)
        {
            _context = context;
            _logger = logger;
            _supportedTables = InitializeSupportedTables();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<DetectedTableInfo?> DetectTableTypeAsync(byte[] fileData, string fileExtension)
        {
            try
            {
                var headers = ExtractHeaders(fileData, fileExtension);
                if (headers == null || !headers.Any())
                {
                    _logger.LogWarning("No headers found in file with extension: {Extension}", fileExtension);
                    return null;
                }

                _logger.LogInformation("Extracted headers: {Headers}", string.Join(", ", headers));

                var normalizedHeaders = headers.Select(h => NormalizeColumnName(h)).ToList();
                _logger.LogInformation("Normalized headers: {Headers}", string.Join(", ", normalizedHeaders));
                
                DetectedTableInfo? bestMatch = null;
                double highestScore = 0;

                foreach (var supportedTable in _supportedTables)
                {
                    var score = CalculateMatchScore(normalizedHeaders, supportedTable.Value);
                    _logger.LogInformation("Table {TableName} score: {Score}", supportedTable.Key, score);
                    
                    if (score > highestScore && score >= 0.3) // 30% threshold for debugging
                    {
                        highestScore = score;
                        bestMatch = new DetectedTableInfo
                        {
                            TableType = supportedTable.Key,
                            ConfidenceScore = score,
                            DetectedColumns = headers,
                            SuggestedMappings = CreateColumnMappings(headers, supportedTable.Value)
                        };
                    }
                }

                _logger.LogInformation("Best match: {TableType} with score: {Score}", bestMatch?.TableType ?? "None", highestScore);
                return bestMatch;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting table type");
                return null;
            }
        }

        public async Task<List<SupportedTableInfo>> GetSupportedTablesAsync()
        {
            return await Task.FromResult(_supportedTables.Values.ToList());
        }

        public bool IsTableSupported(string tableName)
        {
            return _supportedTables.ContainsKey(tableName.ToLower());
        }

        private Dictionary<string, SupportedTableInfo> InitializeSupportedTables()
        {
            return new Dictionary<string, SupportedTableInfo>(StringComparer.OrdinalIgnoreCase)
            {
                ["Users"] = new SupportedTableInfo
                {
                    TableName = "Users",
                    DisplayName = "Users",
                    Description = "User accounts for the system",
                    SupportsUpdate = true,
                    PrimaryKeyColumn = "UserId",
                    RequiredColumns = new List<string> { "FirstName", "LastName", "Email" },
                    Columns = new List<TableColumnInfo>
                    {
                        new() { ColumnName = "UserId", DataType = "guid", IsRequired = false, IsUnique = true, Description = "Auto-generated if not provided" },
                        new() { ColumnName = "FirstName", DataType = "string", IsRequired = true, MaxLength = 50 },
                        new() { ColumnName = "LastName", DataType = "string", IsRequired = true, MaxLength = 50 },
                        new() { ColumnName = "Username", DataType = "string", IsRequired = true, IsUnique = true, MaxLength = 100 },
                        new() { ColumnName = "PasswordHash", DataType = "string", IsRequired = false, MaxLength = 255, Description = "System managed - use Password field instead" },
                        new() { ColumnName = "Password", DataType = "string", IsRequired = true, MaxLength = 255, Description = "Plain text password (will be hashed into PasswordHash)" },
                        new() { ColumnName = "Email", DataType = "string", IsRequired = true, IsUnique = true, MaxLength = 255 },
                        new() { ColumnName = "PhoneNumber", DataType = "string", IsRequired = true, MaxLength = 20 },
                        new() { ColumnName = "IsActive", DataType = "boolean", IsRequired = false, DefaultValue = "true" },
                        new() { ColumnName = "Role", DataType = "string", IsRequired = false, DefaultValue = "User", MaxLength = 50 },
                        new() { ColumnName = "LastLogin", DataType = "datetime", IsRequired = false, Description = "System managed" },
                        new() { ColumnName = "CreatedAt", DataType = "datetime", IsRequired = false, Description = "Auto-generated if not provided" },
                        new() { ColumnName = "UpdatedAt", DataType = "datetime", IsRequired = false, Description = "Auto-generated if not provided" }
                    }
                },
                ["Applications"] = new SupportedTableInfo
                {
                    TableName = "Applications",
                    DisplayName = "Applications",
                    Description = "Applications managed by the system",
                    SupportsUpdate = true,
                    PrimaryKeyColumn = "ApplicationId",
                    RequiredColumns = new List<string> { "ApplicationName", "ApplicationDataSourceType" },
                    Columns = new List<TableColumnInfo>
                    {
                        new() { ColumnName = "ApplicationId", DataType = "guid", IsRequired = false, IsUnique = true, Description = "Auto-generated if not provided" },
                        new() { ColumnName = "ApplicationName", DataType = "string", IsRequired = true, IsUnique = true, MaxLength = 100 },
                        new() { ColumnName = "ApplicationDescription", DataType = "string", IsRequired = false, MaxLength = 500 },
                        new() { ColumnName = "ApplicationDataSourceType", DataType = "enum", IsRequired = true, Description = "Data source type enum value", AllowedValues = new List<string> { "MicrosoftSqlServer", "MySql", "PostgreSql", "MongoDb", "Redis", "Oracle", "MariaDb", "Sqlite", "Cassandra", "ElasticSearch", "RestApi", "GraphQL", "SoapApi", "ODataApi", "WebSocket", "CsvFile", "JsonFile", "XmlFile", "ExcelFile", "ParquetFile", "YamlFile", "TextFile", "AzureBlobStorage", "AmazonS3", "GoogleCloudStorage", "RabbitMQ", "ApacheKafka", "AzureServiceBus" } },
                        new() { ColumnName = "CreatedAt", DataType = "datetime", IsRequired = false, Description = "Auto-generated if not provided" },
                        new() { ColumnName = "UpdatedAt", DataType = "datetime", IsRequired = false, Description = "Auto-generated if not provided" }
                    }
                },
                ["UserApplications"] = new SupportedTableInfo
                {
                    TableName = "UserApplications",
                    DisplayName = "User Application Assignments",
                    Description = "Assigns users to applications with specific connections",
                    SupportsUpdate = false,
                    PrimaryKeyColumn = "UserApplicationId",
                    RequiredColumns = new List<string> { "UserId", "ApplicationId", "ApplicationConnectionId" },
                    Columns = new List<TableColumnInfo>
                    {
                        new() { ColumnName = "UserApplicationId", DataType = "guid", IsRequired = false, IsUnique = true, Description = "Auto-generated if not provided" },
                        new() { ColumnName = "UserId", DataType = "guid", IsRequired = true, Description = "User ID (GUID)" },
                        new() { ColumnName = "ApplicationId", DataType = "guid", IsRequired = true, Description = "Application ID (GUID)" },
                        new() { ColumnName = "ApplicationConnectionId", DataType = "guid", IsRequired = true, Description = "Application Connection ID (GUID)" },
                        new() { ColumnName = "CreatedAt", DataType = "datetime", IsRequired = false, Description = "Auto-generated if not provided" }
                    }
                },
                ["TemporaryUsers"] = new SupportedTableInfo
                {
                    TableName = "TemporaryUsers",
                    DisplayName = "Temporary Users",
                    Description = "Temporary users pending verification",
                    SupportsUpdate = true,
                    PrimaryKeyColumn = "TemporaryUserId",
                    RequiredColumns = new List<string> { "FirstName", "LastName", "Email" },
                    Columns = new List<TableColumnInfo>
                    {
                        new() { ColumnName = "TemporaryUserId", DataType = "guid", IsRequired = false, IsUnique = true, Description = "Auto-generated if not provided" },
                        new() { ColumnName = "FirstName", DataType = "string", IsRequired = true, MaxLength = 255 },
                        new() { ColumnName = "LastName", DataType = "string", IsRequired = true, MaxLength = 255 },
                        new() { ColumnName = "Email", DataType = "string", IsRequired = true, IsUnique = true, MaxLength = 255 },
                        new() { ColumnName = "Username", DataType = "string", IsRequired = true, IsUnique = true, MaxLength = 255 },
                        new() { ColumnName = "PasswordHash", DataType = "string", IsRequired = false, MaxLength = 255, Description = "System managed - use Password field instead" },
                        new() { ColumnName = "Password", DataType = "string", IsRequired = true, MaxLength = 255, Description = "Plain text password (will be hashed into PasswordHash)" },
                        new() { ColumnName = "PhoneNumber", DataType = "string", IsRequired = true, MaxLength = 255 },
                        new() { ColumnName = "CreatedAt", DataType = "datetime", IsRequired = false, Description = "Auto-generated if not provided" },
                        new() { ColumnName = "Token", DataType = "string", IsRequired = false, MaxLength = 255, Description = "System managed verification token" }
                    }
                }
            };
        }

        private List<string>? ExtractHeaders(byte[] fileData, string fileExtension)
        {
            return fileExtension.ToLower() switch
            {
                ".csv" => ExtractCsvHeaders(fileData),
                ".json" => ExtractJsonHeaders(fileData),
                ".xml" => ExtractXmlHeaders(fileData),
                ".xlsx" or ".xls" => ExtractExcelHeaders(fileData),
                _ => null
            };
        }

        private List<string> ExtractCsvHeaders(byte[] fileData)
        {
            using var memoryStream = new MemoryStream(fileData);
            using var reader = new StreamReader(memoryStream, Encoding.UTF8);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            });

            csv.Read();
            csv.ReadHeader();
            return csv.HeaderRecord?.ToList() ?? new List<string>();
        }

        private List<string> ExtractExcelHeaders(byte[] fileData)
        {
            using var memoryStream = new MemoryStream(fileData);
            using var package = new ExcelPackage(memoryStream);
            
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            if (worksheet == null || worksheet.Dimension == null)
            {
                return new List<string>();
            }

            var headers = new List<string>();
            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                var header = worksheet.Cells[1, col].Value?.ToString() ?? $"Column{col}";
                headers.Add(header);
            }

            return headers;
        }

        private List<string> ExtractJsonHeaders(byte[] fileData)
        {
            try
            {
                var json = Encoding.UTF8.GetString(fileData);
                var jsonDoc = System.Text.Json.JsonDocument.Parse(json);
                
                if (jsonDoc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array && 
                    jsonDoc.RootElement.GetArrayLength() > 0)
                {
                    var firstElement = jsonDoc.RootElement[0];
                    if (firstElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        return firstElement.EnumerateObject()
                            .Select(prop => prop.Name)
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting JSON headers");
            }
            
            return new List<string>();
        }

        private List<string> ExtractXmlHeaders(byte[] fileData)
        {
            try
            {
                var xml = Encoding.UTF8.GetString(fileData);
                var doc = System.Xml.Linq.XDocument.Parse(xml);
                
                var root = doc.Root;
                if (root != null && root.Elements().Any())
                {
                    var firstElement = root.Elements().First();
                    return firstElement.Elements()
                        .Select(e => e.Name.LocalName)
                        .Distinct()
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting XML headers");
            }
            
            return new List<string>();
        }

        private double CalculateMatchScore(List<string> fileHeaders, SupportedTableInfo tableInfo)
        {
            var tableColumns = tableInfo.Columns.Select(c => NormalizeColumnName(c.ColumnName)).ToList();
            var requiredColumns = tableInfo.RequiredColumns.Select(c => NormalizeColumnName(c)).ToList();

            _logger.LogInformation("Calculating score for table {TableName}", tableInfo.TableName);
            _logger.LogInformation("File headers: {FileHeaders}", string.Join(", ", fileHeaders));
            _logger.LogInformation("Table columns: {TableColumns}", string.Join(", ", tableColumns));
            _logger.LogInformation("Required columns: {RequiredColumns}", string.Join(", ", requiredColumns));

            int matchedColumns = 0;
            int matchedRequired = 0;

            foreach (var header in fileHeaders)
            {
                if (tableColumns.Contains(header))
                {
                    matchedColumns++;
                    _logger.LogInformation("Matched column: {Header}", header);
                    if (requiredColumns.Contains(header))
                    {
                        matchedRequired++;
                        _logger.LogInformation("Matched required column: {Header}", header);
                    }
                }
            }

            _logger.LogInformation("Matched {MatchedColumns}/{TotalColumns} columns, {MatchedRequired}/{TotalRequired} required", 
                matchedColumns, tableColumns.Count, matchedRequired, requiredColumns.Count);

            // Calculate base score based on matched columns
            double score = matchedColumns > 0 ? (double)matchedColumns / tableColumns.Count : 0;
            
            // Apply penalty if missing required columns (but don't make it 0)
            if (matchedRequired < requiredColumns.Count)
            {
                double requiredRatio = (double)matchedRequired / requiredColumns.Count;
                score *= requiredRatio; // Reduce score proportionally
                _logger.LogInformation("Applied required column penalty: {RequiredRatio}", requiredRatio);
            }
            
            // Bonus for having exact number of columns
            if (fileHeaders.Count == tableColumns.Count)
            {
                score += 0.1;
                _logger.LogInformation("Applied exact column count bonus");
            }

            var finalScore = Math.Min(score, 1.0);
            _logger.LogInformation("Final score for {TableName}: {Score}", tableInfo.TableName, finalScore);
            
            return finalScore;
        }

        private Dictionary<string, string> CreateColumnMappings(List<string> fileHeaders, SupportedTableInfo tableInfo)
        {
            var mappings = new Dictionary<string, string>();
            var normalizedTableColumns = tableInfo.Columns.ToDictionary(
                c => NormalizeColumnName(c.ColumnName),
                c => c.ColumnName
            );

            foreach (var header in fileHeaders)
            {
                var normalizedHeader = NormalizeColumnName(header);
                if (normalizedTableColumns.TryGetValue(normalizedHeader, out var tableColumn))
                {
                    mappings[header] = tableColumn;
                }
                else
                {
                    // Try fuzzy matching
                    var bestMatch = FindBestMatch(normalizedHeader, normalizedTableColumns.Keys);
                    if (!string.IsNullOrEmpty(bestMatch))
                    {
                        mappings[header] = normalizedTableColumns[bestMatch];
                    }
                }
            }

            return mappings;
        }

        private string NormalizeColumnName(string columnName)
        {
            return columnName
                .ToLower()
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "");
        }

        private string? FindBestMatch(string input, IEnumerable<string> candidates)
        {
            var bestMatch = "";
            var bestScore = 0.0;

            foreach (var candidate in candidates)
            {
                var score = CalculateSimilarity(input, candidate);
                if (score > bestScore && score > 0.8) // 80% similarity threshold
                {
                    bestScore = score;
                    bestMatch = candidate;
                }
            }

            return string.IsNullOrEmpty(bestMatch) ? null : bestMatch;
        }

        private double CalculateSimilarity(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                return 0;

            if (s1.Equals(s2))
                return 1;

            var distance = LevenshteinDistance(s1, s2);
            var maxLength = Math.Max(s1.Length, s2.Length);
            return 1.0 - (double)distance / maxLength;
        }

        private int LevenshteinDistance(string s1, string s2)
        {
            var m = s1.Length;
            var n = s2.Length;
            var d = new int[m + 1, n + 1];

            for (var i = 0; i <= m; i++)
                d[i, 0] = i;

            for (var j = 0; j <= n; j++)
                d[0, j] = j;

            for (var i = 1; i <= m; i++)
            {
                for (var j = 1; j <= n; j++)
                {
                    var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }

            return d[m, n];
        }
    }
}