using Prototype.Services.Interfaces;

namespace Prototype.Services.DataParser;

/// <summary>
/// Parses JSON data dumps into model instances.
/// </summary>
public class JsonDataDumpParserService : BaseDataDumpParserService, IDataDumpParserService
{
    public async Task<List<object>> ParseDataDump(ICollection<IFormFile> files, Type modelType)
    {
        return await ParseFilesWithExceptionHandling(files, async file =>
        {
            await using var stream = file.OpenReadStream();
            var type = typeof(List<>).MakeGenericType(modelType);
            var data = await System.Text.Json.JsonSerializer.DeserializeAsync(stream, type);
            return data as IEnumerable<object> ?? [];
        });
    }
}