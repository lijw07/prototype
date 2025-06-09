using Prototype.DTOs;
using Prototype.Services.Interfaces;

namespace Prototype.Services.DataParser;

public class CsvDataDumpParserService : IDataDumpParserService
{

    public Task<List<TableSchemaDto>> ParseAndInferSchemasAsync(ICollection<IFormFile> file)
    {
        throw new NotImplementedException();
    }
}