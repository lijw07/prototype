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
                    return null;
                }

                var normalizedHeaders = headers.Select(h => NormalizeColumnName(h)).ToList();
                DetectedTableInfo? bestMatch = null;
                double highestScore = 0;

                foreach (var supportedTable in _supportedTables)
                {
                    var score = CalculateMatchScore(normalizedHeaders, supportedTable.Value);
                    if (score > highestScore && score >= 0.6) // 60% threshold
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
                    PrimaryKeyColumn = "Username",
                    RequiredColumns = new List<string> { "Username", "Email", "FirstName", "LastName" },
                    Columns = new List<TableColumnInfo>
                    {
                        new() { ColumnName = "Username", DataType = "string", IsRequired = true, IsUnique = true, MaxLength = 50 },
                        new() { ColumnName = "Email", DataType = "string", IsRequired = true, IsUnique = true, MaxLength = 100 },
                        new() { ColumnName = "FirstName", DataType = "string", IsRequired = true, MaxLength = 50 },
                        new() { ColumnName = "LastName", DataType = "string", IsRequired = true, MaxLength = 50 },
                        new() { ColumnName = "PhoneNumber", DataType = "string", IsRequired = false, MaxLength = 20 },
                        new() { ColumnName = "Role", DataType = "string", IsRequired = false, DefaultValue = "User", AllowedValues = new List<string> { "Admin", "User", "ReadOnly" } },
                        new() { ColumnName = "IsActive", DataType = "boolean", IsRequired = false, DefaultValue = "true" },
                        new() { ColumnName = "Department", DataType = "string", IsRequired = false, MaxLength = 100 }
                    }
                },
                ["Applications"] = new SupportedTableInfo
                {
                    TableName = "Applications",
                    DisplayName = "Applications",
                    Description = "Applications managed by the system",
                    SupportsUpdate = true,
                    PrimaryKeyColumn = "ApplicationName",
                    RequiredColumns = new List<string> { "ApplicationName", "ApplicationDescription" },
                    Columns = new List<TableColumnInfo>
                    {
                        new() { ColumnName = "ApplicationName", DataType = "string", IsRequired = true, IsUnique = true, MaxLength = 100 },
                        new() { ColumnName = "ApplicationDescription", DataType = "string", IsRequired = true, MaxLength = 500 },
                        new() { ColumnName = "ApplicationDataSourceType", DataType = "string", IsRequired = false, DefaultValue = "Database", AllowedValues = new List<string> { "Database", "API", "File", "Cloud" } },
                        new() { ColumnName = "IsActive", DataType = "boolean", IsRequired = false, DefaultValue = "true" },
                        new() { ColumnName = "Owner", DataType = "string", IsRequired = false, MaxLength = 100 },
                        new() { ColumnName = "Category", DataType = "string", IsRequired = false, MaxLength = 50 }
                    }
                },
                ["UserApplications"] = new SupportedTableInfo
                {
                    TableName = "UserApplications",
                    DisplayName = "User Application Assignments",
                    Description = "Assigns users to applications with specific permissions",
                    SupportsUpdate = false,
                    PrimaryKeyColumn = "",
                    RequiredColumns = new List<string> { "Username", "ApplicationName" },
                    Columns = new List<TableColumnInfo>
                    {
                        new() { ColumnName = "Username", DataType = "string", IsRequired = true, MaxLength = 50 },
                        new() { ColumnName = "ApplicationName", DataType = "string", IsRequired = true, MaxLength = 100 },
                        new() { ColumnName = "PermissionLevel", DataType = "string", IsRequired = false, DefaultValue = "Read", AllowedValues = new List<string> { "Read", "Write", "Admin" } },
                        new() { ColumnName = "ExpirationDate", DataType = "datetime", IsRequired = false },
                        new() { ColumnName = "Notes", DataType = "string", IsRequired = false, MaxLength = 500 }
                    }
                },
                ["TemporaryUsers"] = new SupportedTableInfo
                {
                    TableName = "TemporaryUsers",
                    DisplayName = "Temporary Users",
                    Description = "Temporary users pending approval",
                    SupportsUpdate = true,
                    PrimaryKeyColumn = "Email",
                    RequiredColumns = new List<string> { "FirstName", "LastName", "Email" },
                    Columns = new List<TableColumnInfo>
                    {
                        new() { ColumnName = "FirstName", DataType = "string", IsRequired = true, MaxLength = 50 },
                        new() { ColumnName = "LastName", DataType = "string", IsRequired = true, MaxLength = 50 },
                        new() { ColumnName = "Email", DataType = "string", IsRequired = true, IsUnique = true, MaxLength = 100 },
                        new() { ColumnName = "RequestedApplications", DataType = "string", IsRequired = false, MaxLength = 500, Description = "Comma-separated application names" },
                        new() { ColumnName = "Justification", DataType = "string", IsRequired = false, MaxLength = 1000 },
                        new() { ColumnName = "RequestedBy", DataType = "string", IsRequired = false, MaxLength = 50 }
                    }
                }
            };
        }

        private List<string>? ExtractHeaders(byte[] fileData, string fileExtension)
        {
            return fileExtension.ToLower() switch
            {
                ".csv" => ExtractCsvHeaders(fileData),
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

        private double CalculateMatchScore(List<string> fileHeaders, SupportedTableInfo tableInfo)
        {
            var tableColumns = tableInfo.Columns.Select(c => NormalizeColumnName(c.ColumnName)).ToList();
            var requiredColumns = tableInfo.RequiredColumns.Select(c => NormalizeColumnName(c)).ToList();

            int matchedColumns = 0;
            int matchedRequired = 0;

            foreach (var header in fileHeaders)
            {
                if (tableColumns.Contains(header))
                {
                    matchedColumns++;
                    if (requiredColumns.Contains(header))
                    {
                        matchedRequired++;
                    }
                }
            }

            // Must have all required columns
            if (matchedRequired < requiredColumns.Count)
            {
                return 0;
            }

            // Calculate score based on matched columns
            double score = (double)matchedColumns / tableColumns.Count;
            
            // Bonus for having exact number of columns
            if (fileHeaders.Count == tableColumns.Count)
            {
                score += 0.1;
            }

            return Math.Min(score, 1.0);
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