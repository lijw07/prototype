using System.Text;
using System.Text.Json;
using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;

namespace Prototype.Database.File;

public class JsonFileConnectionStrategy(
    PasswordEncryptionService encryptionService,
    ILogger<JsonFileConnectionStrategy> logger)
    : IFileConnectionStrategy
{
    private readonly PasswordEncryptionService _encryptionService = encryptionService;

    public DataSourceTypeEnum ConnectionType => DataSourceTypeEnum.JsonFile;

    public Dictionary<AuthenticationTypeEnum, bool> GetSupportedAuthTypes()
    {
        return new Dictionary<AuthenticationTypeEnum, bool>
        {
            { AuthenticationTypeEnum.NoAuth, true },
            { AuthenticationTypeEnum.FileSystem, true }
        };
    }

    public async Task<object> ReadDataAsync(ConnectionSourceRequestDto sourceRequest)
    {
        try
        {
            var filePath = GetFilePath(sourceRequest);
            if (!System.IO.File.Exists(filePath))
            {
                throw new FileNotFoundException($"JSON file not found: {filePath}");
            }

            var encoding = GetEncoding(sourceRequest.Encoding ?? "UTF-8");
            var jsonContent = await System.IO.File.ReadAllTextAsync(filePath, encoding);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            // Try to parse as array first, then as object
            object? parsedData = null;
            var isArray = false;

            try
            {
                parsedData = JsonSerializer.Deserialize<JsonElement[]>(jsonContent, options);
                isArray = true;
            }
            catch
            {
                try
                {
                    parsedData = JsonSerializer.Deserialize<JsonElement>(jsonContent, options);
                }
                catch (JsonException ex)
                {
                    throw new InvalidDataException($"Invalid JSON format: {ex.Message}", ex);
                }
            }

            // Convert to a standardized format
            var result = new List<Dictionary<string, object>>();

            if (isArray && parsedData is JsonElement[] arrayData)
            {
                foreach (var item in arrayData)
                {
                    result.Add(ConvertJsonElementToDictionary(item));
                }
            }
            else if (parsedData is JsonElement singleData)
            {
                if (singleData.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in singleData.EnumerateArray())
                    {
                        result.Add(ConvertJsonElementToDictionary(item));
                    }
                }
                else
                {
                    result.Add(ConvertJsonElementToDictionary(singleData));
                }
            }

            return new
            {
                Data = result,
                RecordCount = result.Count,
                FilePath = filePath,
                Encoding = encoding.EncodingName,
                IsArray = isArray,
                FileSize = new FileInfo(filePath).Length
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read JSON file: {FilePath}", sourceRequest.FilePath);
            throw;
        }
    }

    public async Task<object> ReadDataAsync(ApplicationConnectionModel source)
    {
        var dto = MapToDto(source);
        return await ReadDataAsync(dto);
    }

    public async Task<bool> TestConnectionAsync(ConnectionSourceRequestDto sourceRequest)
    {
        try
        {
            var filePath = GetFilePath(sourceRequest);
            
            // Check if file exists and is accessible
            if (!System.IO.File.Exists(filePath))
            {
                return false;
            }

            var encoding = GetEncoding(sourceRequest.Encoding ?? "UTF-8");
            var jsonContent = await System.IO.File.ReadAllTextAsync(filePath, encoding);

            // Try to parse JSON to validate format
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            JsonSerializer.Deserialize<JsonElement>(jsonContent, options);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "JSON connection test failed for {FilePath}", sourceRequest.FilePath);
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
        return "JSON file connection supporting arrays and objects with various encodings";
    }

    private string GetFilePath(ConnectionSourceRequestDto sourceRequest)
    {
        if (!string.IsNullOrEmpty(sourceRequest.FilePath))
        {
            return sourceRequest.FilePath;
        }

        if (!string.IsNullOrEmpty(sourceRequest.Url))
        {
            // Handle file:// URLs
            if (sourceRequest.Url.StartsWith("file://"))
            {
                return sourceRequest.Url.Substring(7);
            }
            return sourceRequest.Url;
        }

        throw new ArgumentException("Either FilePath or Url must be provided for JSON file connection");
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

    private Dictionary<string, object> ConvertJsonElementToDictionary(JsonElement element)
    {
        var result = new Dictionary<string, object>();

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                result[property.Name] = ConvertJsonValue(property.Value);
            }
        }
        else
        {
            // If it's not an object, create a single-value dictionary
            result["Value"] = ConvertJsonValue(element);
        }

        return result;
    }

    private object ConvertJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt64(out var longVal) ? longVal : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonValue).ToArray(),
            JsonValueKind.Object => ConvertJsonElementToDictionary(element),
            _ => element.ToString()
        };
    }

    private ConnectionSourceRequestDto MapToDto(ApplicationConnectionModel source)
    {
        return new ConnectionSourceRequestDto
        {
            Host = source.Host,
            Port = source.Port,
            Url = source.Url,
            AuthenticationType = source.AuthenticationType,
            FilePath = source.FilePath,
            FileFormat = source.FileFormat,
            Encoding = source.Encoding,
            CustomProperties = source.CustomProperties
        };
    }
}