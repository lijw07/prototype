using Prototype.Services.Interfaces;

namespace Prototype.Services.DataParser;

/// <summary>
/// Parses CSV data dumps into model instances.
/// </summary>
public class CsvDataDumpParserService : BaseDataDumpParserService, IDataDumpParserService
{
    public async Task<List<object>> ParseDataDump(ICollection<IFormFile> files, Type modelType)
    {
        return await ParseFilesWithExceptionHandling(files, async file =>
        {
            await using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            using var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);
            return csv.GetRecords(modelType).ToList();
        });
    }
}