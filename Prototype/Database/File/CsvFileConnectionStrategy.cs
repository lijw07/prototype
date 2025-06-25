using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;

namespace Prototype.Database.File;

public class CsvFileConnectionStrategy(
    PasswordEncryptionService encryptionService,
    ILogger<CsvFileConnectionStrategy> logger)
    : IFileConnectionStrategy
{
    private readonly PasswordEncryptionService _encryptionService = encryptionService;

    public DataSourceTypeEnum ConnectionType => DataSourceTypeEnum.CsvFile;

    public Dictionary<AuthenticationTypeEnum, bool> GetSupportedAuthTypes()
    {
        return new Dictionary<AuthenticationTypeEnum, bool>
        {
            { AuthenticationTypeEnum.NoAuth, true },
            { AuthenticationTypeEnum.FileSystem, true }
        };
    }

    public async Task<object> ReadDataAsync(ConnectionSourceDto source)
    {
        try
        {
            var filePath = GetFilePath(source);
            if (!System.IO.File.Exists(filePath))
            {
                throw new FileNotFoundException($"CSV file not found: {filePath}");
            }

            var encoding = GetEncoding(source.Encoding ?? "UTF-8");
            var delimiter = source.Delimiter ?? ",";

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter,
                HasHeaderRecord = source.HasHeader,
                MissingFieldFound = null,
                BadDataFound = null
            };

            using var reader = new StringReader(await System.IO.File.ReadAllTextAsync(filePath, encoding));
            using var csv = new CsvReader(reader, config);

            var records = new List<Dictionary<string, object>>();
            
            if (source.HasHeader)
            {
                await csv.ReadAsync();
                csv.ReadHeader();
                var headers = csv.HeaderRecord ?? Array.Empty<string>();

                while (await csv.ReadAsync())
                {
                    var record = new Dictionary<string, object>();
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var value = csv.TryGetField(i, out string? fieldValue) ? fieldValue : null;
                        record[headers[i]] = value ?? string.Empty;
                    }
                    records.Add(record);
                }
            }
            else
            {
                while (await csv.ReadAsync())
                {
                    var record = new Dictionary<string, object>();
                    for (int i = 0; csv.TryGetField(i, out string? fieldValue); i++)
                    {
                        record[$"Column{i + 1}"] = fieldValue ?? string.Empty;
                    }
                    records.Add(record);
                }
            }

            return new
            {
                Data = records,
                RowCount = records.Count,
                FilePath = filePath,
                Encoding = encoding.EncodingName,
                Delimiter = delimiter,
                HasHeader = source.HasHeader
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read CSV file: {FilePath}", source.FilePath);
            throw;
        }
    }

    public async Task<object> ReadDataAsync(ApplicationConnectionModel source)
    {
        var dto = MapToDto(source);
        return await ReadDataAsync(dto);
    }

    public async Task<bool> TestConnectionAsync(ConnectionSourceDto source)
    {
        try
        {
            var filePath = GetFilePath(source);
            
            // Check if file exists and is accessible
            if (!System.IO.File.Exists(filePath))
            {
                return false;
            }

            // Try to read the first few lines to validate format
            var encoding = GetEncoding(source.Encoding ?? "UTF-8");
            var delimiter = source.Delimiter ?? ",";

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter,
                HasHeaderRecord = source.HasHeader,
                MissingFieldFound = null,
                BadDataFound = null
            };

            using var reader = new StringReader(await System.IO.File.ReadAllTextAsync(filePath, encoding));
            using var csv = new CsvReader(reader, config);

            // Try to read at least one record
            if (source.HasHeader)
            {
                await csv.ReadAsync();
                csv.ReadHeader();
            }
            
            await csv.ReadAsync();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CSV connection test failed for {FilePath}", source.FilePath);
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync(ApplicationConnectionModel source)
    {
        var dto = MapToDto(source);
        return await TestConnectionAsync(dto);
    }

    public string GetConnectionDescription()
    {
        return "CSV file connection supporting various delimiters, encodings, and header configurations";
    }

    private string GetFilePath(ConnectionSourceDto source)
    {
        if (!string.IsNullOrEmpty(source.FilePath))
        {
            return source.FilePath;
        }

        if (!string.IsNullOrEmpty(source.Url))
        {
            // Handle file:// URLs
            if (source.Url.StartsWith("file://"))
            {
                return source.Url.Substring(7);
            }
            return source.Url;
        }

        throw new ArgumentException("Either FilePath or Url must be provided for CSV file connection");
    }

    private Encoding GetEncoding(string encodingName)
    {
        return encodingName.ToUpperInvariant() switch
        {
            "UTF-8" or "UTF8" => Encoding.UTF8,
            "UTF-16" or "UTF16" => Encoding.Unicode,
            "ASCII" => Encoding.ASCII,
            "ISO-8859-1" or "LATIN1" => Encoding.GetEncoding("ISO-8859-1"),
            _ => Encoding.UTF8
        };
    }

    private ConnectionSourceDto MapToDto(ApplicationConnectionModel source)
    {
        return new ConnectionSourceDto
        {
            Host = source.Host,
            Port = source.Port,
            Url = source.Url,
            AuthenticationType = source.AuthenticationType,
            FilePath = source.FilePath,
            FileFormat = source.FileFormat,
            Delimiter = source.Delimiter,
            Encoding = source.Encoding,
            HasHeader = source.HasHeader,
            CustomProperties = source.CustomProperties
        };
    }
}